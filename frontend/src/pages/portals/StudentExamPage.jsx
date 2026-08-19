import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import {
  startStudentExam,
  submitStudentExam,
  submitExamKeepAlive,
  saveExamDraft,
  getExamProgress,
  registerExamViolation,
  getReadableErrorMessage,
  toAbsoluteFileUrl,
} from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";
import { toArabicDigits, toArabicNumber } from "../../utils/arabicNumbers";
import { useLanguage } from "../../app/i18n/LanguageContext";

function getQuestionSnapshotId(q) {
  return (
    q?.questionSnapshotId ||
    q?.snapshotId ||
    q?.attemptQuestionSnapshotId ||
    q?.id ||
    q?.questionId
  );
}

function getTextDirection(value) {
  return /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF]/.test(String(value || ""))
    ? "rtl"
    : "ltr";
}

function getDraftStorageKey(examId) {
  return `student-exam-draft:${examId}`;
}

function getViolationStorageKey(examId) {
  return `student-exam-violations:${examId}`;
}

function readJsonStorage(key, fallback) {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
}

function writeJsonStorage(key, value) {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // ignore
  }
}

function normalizeExam(data, fallbackExamId) {
  const questions = Array.isArray(data?.questions) ? data.questions : [];

  return {
    examId: data?.examId || fallbackExamId,
    title: data?.title || "الاختبار",
    examCode: data?.examCode || "-",
    attemptId: data?.attemptId || null,
    startAtUtc: data?.startAtUtc || null,
    endAtUtc: data?.endAtUtc || null,
    allowStudentExit: data?.allowStudentExit === true,
    enableAntiCheat: data?.enableAntiCheat === false ? false : true,
    maxViolationCount: Number(data?.maxViolationCount || 3),
    questions: questions.map((q, index) => ({
      id: getQuestionSnapshotId(q) || `q-${index + 1}`,
      questionSnapshotId: getQuestionSnapshotId(q) || `q-${index + 1}`,
      questionText: q.questionText || q.text || "",
      questionImageUrl: q.questionImageUrl || q.imageUrl || "",
      choices: (q.choices || []).map((c, cIndex) => ({
        displayLabel:
          c.displayLabel || c.label || ["A", "B", "C", "D"][cIndex] || "",
        originalKey: c.originalKey || c.value || c.key || c.displayLabel || "",
        text: c.text || c.choiceText || "",
        imageUrl: c.imageUrl || c.choiceImageUrl || "",
      })),
    })),
  };
}

function buildSubmitPayload(examId, answers, questions, isAutoSubmitDueToExit = false) {
  return {
    examId,
    isAutoSubmitDueToExit,
    answers: (questions || [])
      .map((q) => {
        const id = getQuestionSnapshotId(q);
        const selectedAnswer = answers?.[id];

        if (!id || !selectedAnswer) return null;

        return {
          questionSnapshotId: id,
          questionId: id,
          selectedAnswer,
        };
      })
      .filter(Boolean),
  };
}

function getTimeText(endAtUtc) {
  if (!endAtUtc) return { text: "-", ended: false };

  const diff = new Date(endAtUtc).getTime() - Date.now();
  if (diff <= 0) return { text: "انتهى الوقت", ended: true };

  const total = Math.floor(diff / 1000);
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;

  return {
    text: `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}:${String(
      s
    ).padStart(2, "0")}`,
    ended: false,
  };
}

export default function StudentExamPage() {
  const { t, language, dir } = useLanguage();
  const { examId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  const [exam, setExam] = useState(() =>
    normalizeExam(location.state?.startedExam, examId)
  );
  const [answers, setAnswers] = useState({});
  const [loading, setLoading] = useState(!location.state?.startedExam);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [timeLeft, setTimeLeft] = useState("-");
  const [violationCount, setViolationCount] = useState(0);
  const [antiCheatWarning, setAntiCheatWarning] = useState("");
  const [violationPopup, setViolationPopup] = useState(null);

  const answersRef = useRef({});
  const examRef = useRef(exam);
  const autoSubmitted = useRef(false);
  const lastViolationAtRef = useRef(0);
  const lastSavedDraftSignatureRef = useRef("");

  const draftStorageKey = getDraftStorageKey(examId);
  const violationStorageKey = getViolationStorageKey(examId);

  useEffect(() => {
    answersRef.current = answers;
  }, [answers]);

  useEffect(() => {
    examRef.current = exam;
  }, [exam]);

  useEffect(() => {
    async function load() {
      try {
        setLoading(true);
        setError("");

        const data = await startStudentExam(examId);
        setExam(normalizeExam(data, examId));
      } catch (err) {
        setError(getReadableErrorMessage(err, "فشل تحميل الاختبار"));
      } finally {
        setLoading(false);
      }
    }

    if (!location.state?.startedExam && examId) {
      load();
    }
  }, [examId]);

  useEffect(() => {
    if (!examId) return;

    const localDraft = readJsonStorage(draftStorageKey, null);
    if (localDraft?.answers) {
      setAnswers((prev) => ({ ...prev, ...localDraft.answers }));
    }

    getExamProgress(examId)
      .then((progress) => {
        if (!progress?.hasActiveAttempt || !Array.isArray(progress.answers)) {
          return;
        }

        const serverAnswers = {};
        progress.answers.forEach((x) => {
          serverAnswers[x.questionSnapshotId] = x.selectedAnswer;
        });

        setAnswers((prev) => ({ ...serverAnswers, ...prev }));
      })
      .catch(() => {});
  }, [examId]);

  async function flushDraftToServer() {
    const currentExam = examRef.current;
    const currentAnswers = answersRef.current;

    writeJsonStorage(draftStorageKey, {
      answers: currentAnswers,
      savedAtUtc: new Date().toISOString(),
    });

    const draftAnswers = buildSubmitPayload(
      examId,
      currentAnswers,
      currentExam?.questions || []
    ).answers;

    if (!navigator.onLine || draftAnswers.length === 0) return;

    const signature = JSON.stringify(draftAnswers);
    if (signature === lastSavedDraftSignatureRef.current) return;
    lastSavedDraftSignatureRef.current = signature;

    await saveExamDraft(examId, draftAnswers);
  }

  async function flushPendingViolations() {
    if (!navigator.onLine) return;

    const pending = readJsonStorage(violationStorageKey, []);
    const remaining = [];

    for (const item of pending) {
      try {
        await registerExamViolation(examId, item);
      } catch {
        remaining.push(item);
      }
    }

    writeJsonStorage(violationStorageKey, remaining);
  }

  function queueViolation(type, details) {
    if (submitting || autoSubmitted.current) return;

    const currentExam = examRef.current;
    if (currentExam?.enableAntiCheat === false) return;

    const now = Date.now();
    if (now - lastViolationAtRef.current < 2500) return;
    lastViolationAtRef.current = now;

   const latestDraftAnswers = buildSubmitPayload(
      examId,
      answersRef.current,
      currentExam?.questions || []
    ).answers;

    const item = {
      type,
      details,
      occurredAtUtc: new Date().toISOString(),
      answers: latestDraftAnswers,
    };

    

    const saveBeforeViolation =
      latestDraftAnswers.length > 0
        ? saveExamDraft(examId, latestDraftAnswers).catch(() => null)
        : Promise.resolve(null);

    saveBeforeViolation.then(() => registerExamViolation(examId, item))
      .then((res) => {
        const count = Number(res?.violationsCount || 0);

        if (count > 0) {
          setViolationCount(count);
        }

        if (res?.shouldWarn || count >= 2) {
          const message =
            res?.message ||
            "تم تسجيل مخالفات. في حال التكرار سيتم غلق الاختبار.";

          setAntiCheatWarning(message);
          setViolationPopup({
            type: "warning",
            title: "تنبيه مخالفة",
            message,
          });
        }

        if (
          res?.closedForViolation === true ||
          res?.status === "ClosedForViolation" ||
          res?.autoSubmitted === true
        ) {
          autoSubmitted.current = true;

          const message =
            res?.message ||
            "تم غلق الاختبار بسبب تكرار المخالفات، وتم احتساب الإجابات التي تم حلها.";

          localStorage.removeItem(draftStorageKey);
          localStorage.removeItem(violationStorageKey);

          setViolationPopup({
            type: "closed",
            title: "تم غلق الاختبار للمخالفة",
            message,
          });
        }
      })
      .catch(() => {});
  }

  useEffect(() => {
    const timer = setInterval(() => {
      flushDraftToServer().catch(() => {});
      flushPendingViolations().catch(() => {});
    }, 10000);

    return () => clearInterval(timer);
  }, [examId]);

  useEffect(() => {
    function handleOnline() {
      flushDraftToServer().catch(() => {});
      flushPendingViolations().catch(() => {});
    }

    window.addEventListener("online", handleOnline);
    return () => window.removeEventListener("online", handleOnline);
  }, [examId]);

  useEffect(() => {
    function handleVisibilityChange() {
      if (document.hidden) {
        queueViolation("visibilitychange", "Student left exam screen");
        flushDraftToServer().catch(() => {});
      }
    }

    function handleBlur() {
      queueViolation("blur", "Exam window lost focus");
    }

    function handleFullscreenChange() {
      if (!document.fullscreenElement) {
        queueViolation("fullscreenchange", "Student exited fullscreen mode");
      }
    }

    document.addEventListener("visibilitychange", handleVisibilityChange);
    document.addEventListener("fullscreenchange", handleFullscreenChange);
    window.addEventListener("blur", handleBlur);

    return () => {
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      document.removeEventListener("fullscreenchange", handleFullscreenChange);
      window.removeEventListener("blur", handleBlur);
    };
  }, [examId]);

  useEffect(() => {
    if (!exam?.endAtUtc) return;

    const timer = setInterval(() => {
      const state = getTimeText(exam.endAtUtc);
      setTimeLeft(state.text);

      if (state.ended && !autoSubmitted.current) {
        autoSubmitted.current = true;
        submitExam(true);
      }
    }, 1000);

    const initial = getTimeText(exam.endAtUtc);
    setTimeLeft(initial.text);

    return () => clearInterval(timer);
  }, [exam?.endAtUtc]);

  useEffect(() => {
    if (!examId || exam?.allowStudentExit !== false) return;

    const autoSubmit = () => {
      const currentExam = examRef.current;
      const payload = buildSubmitPayload(
        examId,
        answersRef.current,
        currentExam?.questions || [],
        true
      );

      if (payload.answers.length > 0) {
        submitExamKeepAlive(payload).catch(() => {});
      }
    };

    window.addEventListener("pagehide", autoSubmit);
    window.addEventListener("beforeunload", autoSubmit);

    return () => {
      window.removeEventListener("pagehide", autoSubmit);
      window.removeEventListener("beforeunload", autoSubmit);
    };
  }, [exam?.allowStudentExit, examId]);

  const answeredCount = useMemo(
    () => Object.values(answers).filter(Boolean).length,
    [answers]
  );

  function selectAnswer(question, choice) {
    const id = getQuestionSnapshotId(question);
    const selected = choice.originalKey || choice.displayLabel;

    setAnswers((prev) => {
      const next = { ...prev, [id]: selected };

      writeJsonStorage(draftStorageKey, {
        answers: next,
        savedAtUtc: new Date().toISOString(),
      });

      const draftAnswers = buildSubmitPayload(
        examId,
        next,
        examRef.current?.questions || []
      ).answers;

      if (draftAnswers.length > 0) {
        const signature = JSON.stringify(draftAnswers);
        if (signature !== lastSavedDraftSignatureRef.current) {
          lastSavedDraftSignatureRef.current = signature;
          saveExamDraft(examId, draftAnswers)
            .then((res) => console.log("draft saved", res))
            .catch((err) => console.error("draft save failed", err));
        }
      }

      return next;
    });
  }

  async function submitExam(isAutoSubmit = false) {
    if (submitting || autoSubmitted.current) return;

    try {
      setSubmitting(true);
      setError("");

      const payload = buildSubmitPayload(
        exam.examId,
        answersRef.current,
        exam.questions,
        isAutoSubmit
      );

      await saveExamDraft(exam.examId, payload.answers).catch(() => null);
      const result = await submitStudentExam(payload);

      localStorage.removeItem(draftStorageKey);
      localStorage.removeItem(violationStorageKey);

      if (result?.closedForViolation === true || result?.status === "ClosedForViolation") {
        navigate("/student", {
          replace: true,
          state: {
            justSubmitted: true,
            closedForViolation: true,
            message:
              result?.message ||
              "تم غلق الاختبار بسبب تكرار المخالفات، وتم احتساب الإجابات التي تم حلها.",
          },
        });
      } else {
        navigate("/student", {
          replace: true,
          state: { examResult: result, justSubmitted: true },
        });
      }
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تسليم الاختبار"));
      setSubmitting(false);
    }
  }

  function enterFullscreen() {
    const root = document.documentElement;

    if (root.requestFullscreen) {
      root.requestFullscreen().catch(() => {});
    }
  }

  function confirmViolationPopup() {
    const popup = violationPopup;
    setViolationPopup(null);

    if (popup?.type === "closed") {
      navigate("/student", {
        replace: true,
        state: {
          closedForViolation: true,
          message:
            popup.message || "تم غلق الاختبار بسبب تكرار المخالفات.",
        },
      });
    }
  }

  function exitExam() {
    if (exam?.allowStudentExit !== true) return;

    const ok = window.confirm(
      "هل تريد الخروج من الاختبار؟ يمكنك الرجوع لاحقًا."
    );

    if (!ok) return;

    flushDraftToServer().finally(() => navigate("/student"));
  }

  if (loading) {
    return (
      <div className="student-exam-old-shell">
        <div className="student-exam-panel">جاري تحميل الاختبار...</div>
      </div>
    );
  }

  if (!exam?.examId) {
    return (
      <div className="student-exam-old-shell">
        <div className="alert error">{error || "تعذر فتح الاختبار"}</div>
      </div>
    );
  }

  return (
    <div className={`student-exam-old-shell exam-dir-${dir}`} dir={dir}>
      <div className="student-exam-header-old">
        <div>
          <span className="topbar-badge">Exam Session</span>
          <h1>{exam.title}</h1>
          <p>{language === "ar" ? "كود الاختبار" : "Exam code"}: {exam.examCode}</p>
        </div>

        <div className="student-exam-header-actions">
          {exam.enableAntiCheat && (
            <button className="ghost-btn" type="button" onClick={enterFullscreen}>
              {language === "ar" ? "تفعيل وضع الاختبار" : "Enable exam mode"}
            </button>
          )}

          <div className="student-exam-chip">
            {language === "ar" ? toArabicNumber(answeredCount) : answeredCount} /{" "}
            {language === "ar" ? toArabicNumber(exam.questions.length) : exam.questions.length}
          </div>

          <div className="student-exam-chip">
            ⏱ {toArabicDigits(timeLeft)}
          </div>
        </div>
      </div>

      {error && <div className="alert error">{error}</div>}
      {antiCheatWarning && (
        <div className="alert warning">{antiCheatWarning}</div>
      )}

      <div className="student-exam-questions">
        {exam.questions.map((question, qIndex) => {
          const questionId = getQuestionSnapshotId(question);
          const questionDirection = getTextDirection(question.questionText);

          return (
            <section
              className={`student-question-old-card content-dir-${questionDirection}`}
              dir={questionDirection}
              key={questionId}
            >
              <h2 dir={questionDirection}>
                <span className="student-question-number">
                  {language === "ar" ? toArabicNumber(qIndex + 1) : qIndex + 1} -
                </span>
                <span>{question.questionText}</span>
              </h2>

              {question.questionImageUrl && (
                <img
                  className="exam-question-image"
                  src={toAbsoluteFileUrl(question.questionImageUrl)}
                  alt={t("questionImage")}
                />
              )}

              <div className="student-choices-old-grid">
                {question.choices.map((choice, cIndex) => {
                  const selected =
                    answers[questionId] ===
                    (choice.originalKey || choice.displayLabel);
                  const choiceDirection = getTextDirection(choice.text);

                  return (
                    <button
                      type="button"
                      key={`${questionId}-${choice.displayLabel}-${cIndex}`}
                      className={`student-choice-old ${
                        selected ? "selected" : ""
                      } content-dir-${choiceDirection}`}
                      dir={choiceDirection}
                      onClick={() => selectAnswer(question, choice)}
                      disabled={submitting || autoSubmitted.current}
                    >
                      <span className="student-choice-number">
                        {language === "ar" ? toArabicNumber(cIndex + 1) : cIndex + 1}
                      </span>

                      <span className="student-choice-content">
                        {choice.text}
                        {choice.imageUrl && (
                          <img
                            className="exam-choice-image"
                            src={toAbsoluteFileUrl(choice.imageUrl)}
                            alt={t("choiceImage")}
                          />
                        )}
                      </span>
                    </button>
                  );
                })}
              </div>
            </section>
          );
        })}
      </div>

      {violationPopup && (
        <div className="violation-popup-overlay" role="dialog" aria-modal="true">
          <div className="violation-popup-card">
            <h3>{violationPopup.title}</h3>
            <p>{violationPopup.message}</p>
            <button
              className="primary-btn"
              type="button"
              onClick={confirmViolationPopup}
            >
              {t("ok")}
            </button>
          </div>
        </div>
      )}

      <div className="student-exam-footer-old">
        {exam.allowStudentExit === true && (
          <button
            className="ghost-btn"
            type="button"
            onClick={exitExam}
            disabled={submitting || autoSubmitted.current}
          >
            {t("exit")}
          </button>
        )}

        <button
          className="primary-btn"
          type="button"
          onClick={() => submitExam(false)}
          disabled={submitting || autoSubmitted.current}
        >
          {submitting ? t("submitting") : t("submit")}
        </button>
      </div>
    </div>
  );
}

import { useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getStudentDashboard, startStudentExam, clearToken, getReadableErrorMessage } from "../../services/api";
import { toArabicDigits, toArabicNumber, toArabicTimePart } from "../../utils/arabicNumbers";
import { formatSaudiDateTime } from "../../utils/dateTime";

function formatDate(value) {
  if (!value) return "-";
  try {
    return formatSaudiDateTime(value);
  } catch {
    return value;
  }
}

function getTimeLeftParts(endAtUtc) {
  if (!endAtUtc) {
    return { totalMs: 0, text: "-", isEnded: false };
  }

  const diff = new Date(endAtUtc).getTime() - Date.now();

  if (diff <= 0) {
    return { totalMs: 0, text: "انتهى الوقت", isEnded: true };
  }

  const totalSeconds = Math.floor(diff / 1000);
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  const text =
    days > 0
      ? `${days}ي ${hours.toString().padStart(2, "0")}:${minutes
          .toString()
          .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`
      : `${hours.toString().padStart(2, "0")}:${minutes
          .toString()
          .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;

  return { totalMs: diff, text, isEnded: false };
}

function normalizeExamStatus(exam) {
  const source = exam || {};
  const now = new Date();

  const start = source.startAtUtc ? new Date(source.startAtUtc) : null;
  const end = source.endAtUtc ? new Date(source.endAtUtc) : null;

  const isPublished = source.isPublished ?? true;
  const isSubmitted = !!source.isSubmitted;

  const notStarted = start ? now < start : false;
  const ended = end ? now > end : false;
  const withinTime = start && end ? now >= start && now <= end : false;

  const canStart =
    isPublished &&
    !isSubmitted &&
    start &&
    end &&
    now >= start &&
    now <= end;

  let availabilityStatus = source.availabilityStatus || source.status || "";

  if (!availabilityStatus) {
    if (!isPublished) availabilityStatus = "غير منشور";
    else if (isSubmitted) availabilityStatus = "تم التسليم";
    else if (notStarted) availabilityStatus = "لم يبدأ بعد";
    else if (ended) availabilityStatus = "انتهى الوقت";
    else if (canStart) availabilityStatus = "متاح الآن";
    else availabilityStatus = "غير متاح";
  }

  let statusTone = "warning";
  if (isSubmitted) statusTone = "success";
  else if (canStart) statusTone = "success";
  else if (ended) statusTone = "danger";
  else if (notStarted) statusTone = "warning";
  else statusTone = "warning";

  return {
    examId: source.examId || source.id || "",
    title: source.title || "اختبار",
    examCode: source.examCode || source.code || "",
    startAtUtc: source.startAtUtc || null,
    endAtUtc: source.endAtUtc || null,
    isPublished,
    isSubmitted,
    canStart,
    availabilityStatus,
    statusTone,
    timeLeft: getTimeLeftParts(source.endAtUtc).text,
    isEnded: ended,
  };
}

export default function StudentPortalPage() {
  const navigate = useNavigate();
  const location = useLocation();

  const [dashboard, setDashboard] = useState(null);
  const [exams, setExams] = useState([]);
  const [loading, setLoading] = useState(true);
  const [startingExamId, setStartingExamId] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [search, setSearch] = useState("");

  async function load() {
    try {
      setLoading(true);
      setError("");

      const data = await getStudentDashboard();

      const availableExams = Array.isArray(data?.availableExams)
        ? data.availableExams
        : Array.isArray(data?.exams)
        ? data.exams
        : [];

      setDashboard({
        studentName: data?.studentName || data?.fullName || data?.userName || "الطالب",
        studentCode: data?.studentCode || "-",
        grade: data?.grade || "-",
      });

      setExams(availableExams.map(normalizeExamStatus));
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحميل بوابة الطالب"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    if (!location.state?.justSubmitted) return;

    const result = location.state?.examResult;
    if (result) {
      setSuccess(
        `تم تسليم الاختبار بنجاح. الدرجة: ${result.score ?? 0} / ${result.totalQuestions ?? 0} - النسبة: ${result.percentage ?? 0}%`
      );
    } else {
      setSuccess("تم تسليم الاختبار بنجاح");
    }
  }, [location.state]);

  useEffect(() => {
    const timer = setInterval(() => {
      setExams((prev) => prev.map(normalizeExamStatus));
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  const filteredExams = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return exams;

    return exams.filter((exam) =>
      (exam.title || "").toLowerCase().includes(q) ||
      (exam.examCode || "").toLowerCase().includes(q) ||
      (exam.availabilityStatus || "").toLowerCase().includes(q)
    );
  }, [exams, search]);

  async function handleStartExam(exam) {
    if (!exam?.examId || !exam.canStart || exam.isSubmitted || exam.isEnded) return;

    try {
      setStartingExamId(exam.examId);
      setError("");

      const started = await startStudentExam(exam.examId);

      navigate(`/student/exams/${exam.examId}/play`, {
        state: {
          startedExam: started,
        },
      });
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل بدء الاختبار"));
    } finally {
      setStartingExamId("");
    }
  }

  function handleLogout() {
    clearToken();
    navigate("/login", { replace: true });
  }

  return (
    <div className="page-shell">
      <div className="page-intro">
        <div>
          <span className="topbar-badge">Student Portal</span>
          <h2>الاختبارات المتاحة</h2>
          <p>يمكنك بدء الاختبار عندما يكون متاحًا، مع عرض الحالة والوقت المتبقي بشكل مباشر.</p>
        </div>

        <div className="topbar-actions">
          <button className="ghost-btn" type="button" onClick={handleLogout}>
            تسجيل الخروج
          </button>
        </div>
      </div>

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="stats-grid">
        <div className="stat-card">
          <span>اسم الطالب</span>
          <strong style={{ fontSize: "24px" }}>{dashboard?.studentName || "-"}</strong>
        </div>
        <div className="stat-card">
          <span>كود الطالب</span>
          <strong style={{ fontSize: "24px" }}>{dashboard?.studentCode || "-"}</strong>
        </div>
        <div className="stat-card">
          <span>الصف</span>
          <strong style={{ fontSize: "24px" }}>{dashboard?.grade || "-"}</strong>
        </div>
        <div className="stat-card">
          <span>عدد الاختبارات</span>
          <strong>{toArabicNumber(exams.length)}</strong>
        </div>
      </div>

      <div className="section-card">
        <div className="section-head">
          <h3>قائمة الاختبارات</h3>
          <p>يمكنك البحث بالاسم أو الكود أو الحالة.</p>
        </div>

        <label>بحث</label>
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="ابحث عن اختبار"
        />

        {loading ? (
          <div className="empty-box top-space">جاري التحميل...</div>
        ) : filteredExams.length === 0 ? (
          <div className="empty-box top-space">لا توجد اختبارات متاحة</div>
        ) : (
          <div className="cards-grid top-space">
            {filteredExams.map((exam) => {
              const isDisabled =
                !exam.canStart ||
                exam.isSubmitted ||
                exam.isEnded ||
                startingExamId === exam.examId;

              return (
                <div className="mini-card" key={exam.examId}>
                  <div className="mini-card-head">
                    <h4>{exam.title}</h4>
                    <span className="mini-pill">{exam.examCode || "-"}</span>
                  </div>

                  <div>البداية: {formatDate(exam.startAtUtc)}</div>
                  <div>النهاية: {formatDate(exam.endAtUtc)}</div>

                  <div className="student-exam-meta-grid top-gap">
                    <div>
                      <span className={`status-badge status-${exam.statusTone}`}>
                        {exam.availabilityStatus}
                      </span>
                    </div>
                    <div className="countdown-chip">
                      ⏱ {exam.timeLeft}
                    </div>
                  </div>

                  <button
                    className="primary-btn full-btn"
                    type="button"
                    disabled={isDisabled}
                    onClick={() => handleStartExam(exam)}
                  >
                    {startingExamId === exam.examId
                      ? "جاري تجهيز الاختبار..."
                      : exam.isSubmitted
                      ? "تم التسليم"
                      : exam.isEnded
                      ? "انتهى الوقت"
                      : exam.canStart
                      ? "بدء الاختبار"
                      : "غير متاح حالياً"}
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

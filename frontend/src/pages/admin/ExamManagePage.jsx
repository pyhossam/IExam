import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  getExamById,
  getExamAnalytics,
  updateExamSettings,
  addExamQuestion,
  updateExamQuestion,
  deleteExamQuestion,
  uploadExamQuestions,
  uploadQuestionImage,
  toAbsoluteFileUrl,
  getExamAttempts,
  resetAttempt,
  getLeaderboard,
  openPdfWithAuth,
  downloadPdfWithAuth,
  downloadFileWithAuth,
  getReadableErrorMessage,
  getCourseClos,
  generateAiQuestionPreview,
} from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";
import PageIntro from "../../components/ui/PageIntro";

const emptyQuestion = {
  courseLearningOutcomeId: "",
  cognitiveLevel: "Understand",
  questionText: "",
  questionImageUrl: "",
  choiceA: "",
  choiceAImageUrl: "",
  choiceB: "",
  choiceBImageUrl: "",
  choiceC: "",
  choiceCImageUrl: "",
  choiceD: "",
  choiceDImageUrl: "",
  correctAnswer: "A",
  explanation: "",
};

const bloomLevels = [
  ["Remember", "تذكر"], ["Understand", "فهم"], ["Apply", "تطبيق"],
  ["Analyze", "تحليل"], ["Evaluate", "تقويم"], ["Create", "ابتكار"],
];

function ImagePreview({ url, label, onRemove }) {
  if (!url) return null;

  return (
    <div className="question-image-preview">
      <div className="question-image-preview-head">
        <span>{label}</span>
        <button type="button" className="ghost-btn slim" onClick={onRemove}>
          حذف الصورة
        </button>
      </div>
      <img src={toAbsoluteFileUrl(url)} alt={label} />
    </div>
  );
}

function ImageUploadField({ label, value, onUploaded, onRemove, disabled }) {
  const [uploading, setUploading] = useState(false);

  async function handleChange(e) {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      setUploading(true);
      const result = await uploadQuestionImage(file);
      onUploaded(result.url);
    } finally {
      setUploading(false);
      e.target.value = "";
    }
  }

  return (
    <div className="image-upload-field">
      <label>{label}</label>
      <input
        type="file"
        accept="image/png,image/jpeg,image/webp"
        onChange={handleChange}
        disabled={disabled || uploading}
      />
      {uploading && <div className="mini-hint">جاري رفع الصورة...</div>}
      <ImagePreview url={value} label={label} onRemove={onRemove} />
    </div>
  );
}

function QuestionVisual({ item }) {
  return (
    <div className="question-visual">
      {item.questionText && <h4>{item.questionText}</h4>}
      {item.questionImageUrl && (
        <img
          className="exam-question-image"
          src={toAbsoluteFileUrl(item.questionImageUrl)}
          alt="صورة السؤال"
        />
      )}

      <div className="question-choices-preview">
        {[
          ["A", item.choiceA, item.choiceAImageUrl],
          ["B", item.choiceB, item.choiceBImageUrl],
          ["C", item.choiceC, item.choiceCImageUrl],
          ["D", item.choiceD, item.choiceDImageUrl],
        ].map(([label, text, imageUrl]) => (
          <div className="question-choice-preview" key={label}>
            <strong>{label})</strong>
            <div>
              {text && <p>{text}</p>}
              {imageUrl && (
                <img
                  className="exam-choice-image"
                  src={toAbsoluteFileUrl(imageUrl)}
                  alt={`اختيار ${label}`}
                />
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function AccordionCard({ id, activeId, onToggle, title, subtitle, badge, children }) {
  const isOpen = activeId === id;

  return (
    <section className={`exam-accordion-card ${isOpen ? "open" : ""}`}>
      <button type="button" className="exam-accordion-head" onClick={() => onToggle(id)}>
        <div className="exam-accordion-head-text">
          <h3>{title}</h3>
          {subtitle ? <p>{subtitle}</p> : null}
        </div>

        <div className="exam-accordion-head-side">
          {badge ? <span className="mini-pill">{badge}</span> : null}
          <span className="exam-accordion-arrow">{isOpen ? "−" : "+"}</span>
        </div>
      </button>

      {isOpen && <div className="exam-accordion-body">{children}</div>}
    </section>
  );
}

export default function ExamManagePage() {
  const { examId } = useParams();

  const [exam, setExam] = useState(null);
  const [analytics, setAnalytics] = useState(null);
  const [attempts, setAttempts] = useState([]);
  const [leaderboard, setLeaderboard] = useState([]);
  const [settings, setSettings] = useState(null);
  const [question, setQuestion] = useState(emptyQuestion);
  const [courseClos, setCourseClos] = useState([]);
  const [editingQuestionId, setEditingQuestionId] = useState("");
  const [file, setFile] = useState(null);
  const [formsCount, setFormsCount] = useState(3);
  const [activeCard, setActiveCard] = useState("settings");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [pageLoading, setPageLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [downloadLoadingKey, setDownloadLoadingKey] = useState("");
  const [aiCount, setAiCount] = useState(5);
  const [aiPdf, setAiPdf] = useState(null);
  const [aiDrafts, setAiDrafts] = useState([]);
  const [attemptSearch, setAttemptSearch] = useState("");
  const [leaderboardLimit, setLeaderboardLimit] = useState(10);

  const questionCount = useMemo(() => (exam?.questions || []).length, [exam]);
  const filteredAttempts = useMemo(() => {
    const term = attemptSearch.trim().toLocaleLowerCase();
    if (!term) return attempts;
    return attempts.filter((item) =>
      `${item.studentName || ""} ${item.studentCode || ""}`.toLocaleLowerCase().includes(term)
    );
  }, [attempts, attemptSearch]);
  const visibleLeaderboard = useMemo(
    () => leaderboard.slice(0, Math.max(1, Number(leaderboardLimit) || 10)),
    [leaderboard, leaderboardLimit]
  );

  async function load() {
    try {
      setPageLoading(true);
      setError("");

      const e = await getExamById(examId);
      const [a, at, lb, clos] = await Promise.all([
        getExamAnalytics(examId),
        getExamAttempts(examId),
        getLeaderboard(examId),
        e.subjectId ? getCourseClos(e.subjectId) : Promise.resolve([]),
      ]);

      setExam(e);
      setAnalytics(a);
      setAttempts(at || []);
      setLeaderboard(lb || []);
      setCourseClos((clos || []).filter((item) => item.isActive !== false));
      setSettings({
        subjectId: e.subjectId || null,
        assessmentType: e.assessmentType || "General",
        maxAttempts: e.maxAttempts || 1,
        title: e.title || "",
        topic: e.topic || "",
        description: e.description || "",
        startAtUtc: e.startAtUtc ? e.startAtUtc.slice(0, 16) : "",
        endAtUtc: e.endAtUtc ? e.endAtUtc.slice(0, 16) : "",
        bankQuestionCount: e.bankQuestionCount || 0,
        examQuestionCount: e.examQuestionCount || 0,
        blueprintCloDistribution: e.blueprintCloDistribution || {},
        blueprintBloomDistribution: e.blueprintBloomDistribution || {},
        isPublished: !!e.isPublished,
        allowStudentExit: e.allowStudentExit !== false,
        enableAntiCheat: e.enableAntiCheat !== false,
        maxViolationCount: e.maxViolationCount || 3,
      });
    } catch (err) {
      setError(getReadableErrorMessage(err, "تعذر تحميل بيانات الاختبار"));
    } finally {
      setPageLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [examId]);

  function toggleCard(id) {
    setActiveCard((prev) => (prev === id ? "" : id));
  }

  function setQuestionField(field, value) {
    setQuestion((prev) => ({ ...prev, [field]: value }));
  }

  async function saveSettings(e) {
    e.preventDefault();

    try {
      setActionLoading(true);
      setError("");
      setSuccess("");
      const cloTotal = Object.values(settings.blueprintCloDistribution || {}).reduce((sum, value) => sum + Number(value || 0), 0);
      const bloomTotal = Object.values(settings.blueprintBloomDistribution || {}).reduce((sum, value) => sum + Number(value || 0), 0);
      if (cloTotal > settings.examQuestionCount) throw new Error("مجموع أسئلة CLO لا يمكن أن يتجاوز إجمالي أسئلة الورقة");
      if (bloomTotal > settings.examQuestionCount) throw new Error("مجموع أسئلة Bloom لا يمكن أن يتجاوز إجمالي أسئلة الورقة");
      await updateExamSettings(examId, settings);
      setSuccess("تم تحديث إعدادات الاختبار بنجاح");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحديث إعدادات الاختبار"));
    } finally {
      setActionLoading(false);
    }
  }

  function setBlueprintCount(group, key, value) {
    const field = group === "clo" ? "blueprintCloDistribution" : "blueprintBloomDistribution";
    setSettings((current) => ({ ...current, [field]: { ...(current[field] || {}), [key]: Math.max(0, Number(value || 0)) } }));
  }

  function setBlueprintPercentage(group, key, percentage) {
    const total = Number(settings.examQuestionCount || 0);
    const safePercentage = Math.max(0, Math.min(100, Number(percentage || 0)));
    setBlueprintCount(group, key, Math.round(total * safePercentage / 100));
  }

  function printBlueprint() {
    document.body.classList.add("printing-blueprint");
    window.print();
    window.setTimeout(() => document.body.classList.remove("printing-blueprint"), 500);
  }

  async function saveQuestion(e) {
    e.preventDefault();

    try {
      setActionLoading(true);
      setError("");
      setSuccess("");

      const hasQuestionContent =
        question.questionText.trim() || question.questionImageUrl;

      const hasChoices =
        (question.choiceA.trim() || question.choiceAImageUrl) &&
        (question.choiceB.trim() || question.choiceBImageUrl) &&
        (question.choiceC.trim() || question.choiceCImageUrl) &&
        (question.choiceD.trim() || question.choiceDImageUrl);

      if (!hasQuestionContent) {
        throw new Error("أدخل نص السؤال أو ارفع صورة السؤال");
      }

      if (!hasChoices) {
        throw new Error("كل اختيار يجب أن يحتوي على نص أو صورة");
      }

      if (!question.cognitiveLevel) {
        throw new Error("مستوى السؤال وفق تصنيف Bloom مطلوب");
      }

      if (exam.assessmentType === "CloAligned" && !question.courseLearningOutcomeId) {
        throw new Error("يجب ربط السؤال بمخرج تعلم في الاختبار المرتبط بـ CLO");
      }

      const questionPayload = {
        ...question,
        courseLearningOutcomeId: question.courseLearningOutcomeId || null,
      };

      if (editingQuestionId) {
        await updateExamQuestion(editingQuestionId, questionPayload);
        setSuccess("تم تحديث السؤال بنجاح");
      } else {
        await addExamQuestion(examId, questionPayload);
        setSuccess("تمت إضافة السؤال بنجاح");
      }

      setQuestion(emptyQuestion);
      setEditingQuestionId("");
      setActiveCard("questions-list");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حفظ السؤال"));
    } finally {
      setActionLoading(false);
    }
  }

  async function removeQuestion(id) {
    if (!window.confirm("هل أنت متأكد من حذف هذا السؤال؟")) return;

    try {
      setActionLoading(true);
      setError("");
      setSuccess("");
      await deleteExamQuestion(id);
      setSuccess("تم حذف السؤال بنجاح");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حذف السؤال"));
    } finally {
      setActionLoading(false);
    }
  }

  async function uploadQuestions(e) {
    e.preventDefault();

    if (!file) {
      setError("اختر ملفًا أولًا");
      return;
    }

    try {
      setActionLoading(true);
      setError("");
      setSuccess("");
      const res = await uploadExamQuestions(examId, file);
      setSuccess(`تم رفع الأسئلة بنجاح. المضاف: ${res.inserted} - المتخطي: ${res.skipped}`);
      setFile(null);
      setActiveCard("questions-list");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل رفع ملف الأسئلة"));
    } finally {
      setActionLoading(false);
    }
  }

  async function generateWithAi(e) {
    e.preventDefault();
    if (aiPdf && aiPdf.size > 25 * 1024 * 1024) {
      setError("حجم ملف PDF أكبر من الحد المسموح (25 ميجابايت). اضغط الملف أو اختر ملفاً أصغر.");
      return;
    }
    try {
      setActionLoading(true); setError(""); setSuccess("");
      const drafts = await generateAiQuestionPreview(examId, aiCount, aiPdf);
      setAiDrafts(Array.isArray(drafts) ? drafts : []);
      setSuccess(`تم توليد ${Array.isArray(drafts) ? drafts.length : 0} سؤالًا للمعاينة. راجعها ثم اعتمدها.`);
    } catch (err) { setError(getReadableErrorMessage(err, "تعذر توليد الأسئلة بالذكاء الاصطناعي")); }
    finally { setActionLoading(false); }
  }

  function updateAiDraft(index, field, value) {
    setAiDrafts((items) => items.map((item, itemIndex) => itemIndex === index ? { ...item, [field]: value } : item));
  }

  async function approveAiDrafts() {
    try {
      setActionLoading(true); setError(""); setSuccess("");
      for (const draft of aiDrafts) await addExamQuestion(examId, { ...draft, courseLearningOutcomeId: draft.courseLearningOutcomeId || null });
      setSuccess(`تم اعتماد وإضافة ${aiDrafts.length} سؤالًا إلى بنك الاختبار.`);
      setAiDrafts([]); setAiPdf(null); await load();
    } catch (err) { setError(getReadableErrorMessage(err, "تعذر اعتماد الأسئلة المولدة")); }
    finally { setActionLoading(false); }
  }

  async function handleResetAttempt(id) {
    const randomValues = new Uint32Array(1);
    window.crypto.getRandomValues(randomValues);
    const confirmationCode = String(1000 + (randomValues[0] % 9000));
    const enteredCode = window.prompt(
      `تنبيه: سيؤدي هذا الإجراء إلى حذف محاولة الطالب وورقة الاختبار المحفوظة، وسيتمكن الطالب من بدء محاولة جديدة.\n\nللتأكيد، أدخل الرقم التالي كما هو:\n${confirmationCode}`
    );

    if (enteredCode === null) return;
    if (enteredCode.trim() !== confirmationCode) {
      setSuccess("");
      setError("رمز التأكيد غير صحيح. لم يتم حذف محاولة الطالب.");
      return;
    }

    try {
      setActionLoading(true);
      setError("");
      setSuccess("");
      await resetAttempt(id);
      setSuccess("تم حذف المحاولة وورقة الاختبار المحفوظة بنجاح");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حذف المحاولة وورقة الاختبار المحفوظة"));
    } finally {
      setActionLoading(false);
    }
  }

  async function handleOpenPdf(path, key) {
    try {
      setDownloadLoadingKey(key);
      setError("");
      setSuccess("");
      await openPdfWithAuth(path);
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل فتح الملف"));
    } finally {
      setDownloadLoadingKey("");
    }
  }

  async function handleDownloadPdf(path, fileName, key) {
    try {
      setDownloadLoadingKey(key);
      setError("");
      setSuccess("");
      await downloadPdfWithAuth(path, fileName);
      setSuccess("تم تجهيز ملف PDF للتنزيل");
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تنزيل ملف PDF"));
    } finally {
      setDownloadLoadingKey("");
    }
  }

  async function handleDownloadFile(path, fileName, key) {
    try {
      setDownloadLoadingKey(key);
      setError("");
      setSuccess("");
      await downloadFileWithAuth(path, fileName);
      setSuccess("تم تجهيز الملف للتنزيل");
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تنزيل الملف"));
    } finally {
      setDownloadLoadingKey("");
    }
  }

  function fillQuestionForEdit(q) {
    setEditingQuestionId(q.id);
    setQuestion({
      courseLearningOutcomeId: q.courseLearningOutcomeId || "",
      cognitiveLevel: q.cognitiveLevel || "Understand",
      questionText: q.questionText || "",
      questionImageUrl: q.questionImageUrl || "",
      choiceA: q.choiceA || "",
      choiceAImageUrl: q.choiceAImageUrl || "",
      choiceB: q.choiceB || "",
      choiceBImageUrl: q.choiceBImageUrl || "",
      choiceC: q.choiceC || "",
      choiceCImageUrl: q.choiceCImageUrl || "",
      choiceD: q.choiceD || "",
      choiceDImageUrl: q.choiceDImageUrl || "",
      correctAnswer: q.correctAnswer || "A",
      explanation: q.explanation || "",
    });
    setActiveCard("questions-editor");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function cancelEdit() {
    setEditingQuestionId("");
    setQuestion(emptyQuestion);
  }

  if (pageLoading) {
    return (
      <div className="standalone-page">
        <div className="section-card">جاري تحميل بيانات الاختبار...</div>
      </div>
    );
  }

  if (!exam || !settings || !analytics) {
    return (
      <div className="standalone-page">
        <div className="alert error">تعذر تحميل بيانات الاختبار</div>
      </div>
    );
  }

  return (
    <div className="standalone-page">
      <PageIntro
        title={exam.title}
        description={`إدارة الاختبار: ${exam.examCode}`}
        actions={
          <Link to="/admin/exams" className="ghost-btn">
            العودة للاختبارات
          </Link>
        }
      />

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="stats-grid">
        <div className="stat-card">
          <span>عدد الأسئلة</span>
          <strong>{analytics.questionsCount}</strong>
        </div>
        <div className="stat-card">
          <span>المسجلون</span>
          <strong>{analytics.registeredStudentsCount}</strong>
        </div>
        <div className="stat-card">
          <span>أدوا الاختبار</span>
          <strong>{analytics.attemptedStudentsCount}</strong>
        </div>
        <div className="stat-card">
          <span>&lt; 50%</span>
          <strong>{analytics.lessThan50}</strong>
        </div>
        <div className="stat-card">
          <span>50 - 75%</span>
          <strong>{analytics.from50To75}</strong>
        </div>
        <div className="stat-card">
          <span>75 - 85%</span>
          <strong>{analytics.from75To85}</strong>
        </div>
        <div className="stat-card">
          <span>&gt; 85%</span>
          <strong>{analytics.greaterThan85}</strong>
        </div>
      </div>

      <div className="exam-accordion-list">
        <AccordionCard
          id="settings"
          activeId={activeCard}
          onToggle={toggleCard}
          title="إعدادات الاختبار"
          subtitle="تعديل خصائص الاختبار"
          badge="Settings"
        >
          <form onSubmit={saveSettings}>
            <label>العنوان</label>
            <input
              value={settings.title}
              onChange={(e) => setSettings({ ...settings, title: e.target.value })}
            />

            <label>الموضوع</label>
            <input
              value={settings.topic}
              onChange={(e) => setSettings({ ...settings, topic: e.target.value })}
            />

            <label>الوصف</label>
            <textarea
              rows="3"
              value={settings.description}
              onChange={(e) => setSettings({ ...settings, description: e.target.value })}
            />

            <label>البداية UTC</label>
            <input
              type="datetime-local"
              value={settings.startAtUtc}
              onChange={(e) => setSettings({ ...settings, startAtUtc: e.target.value })}
            />

            <label>النهاية UTC</label>
            <input
              type="datetime-local"
              value={settings.endAtUtc}
              onChange={(e) => setSettings({ ...settings, endAtUtc: e.target.value })}
            />

            <label>عدد أسئلة البنك</label>
            <input
              type="number"
              value={settings.bankQuestionCount}
              onChange={(e) =>
                setSettings({ ...settings, bankQuestionCount: Number(e.target.value) })
              }
            />

            <label>عدد أسئلة الورقة</label>
            <input
              type="number"
              value={settings.examQuestionCount}
              onChange={(e) =>
                setSettings({ ...settings, examQuestionCount: Number(e.target.value) })
              }
            />

            <label className="checkbox-line">
              <input
                type="checkbox"
                checked={settings.isPublished}
                onChange={(e) =>
                  setSettings({ ...settings, isPublished: e.target.checked })
                }
              />
              منشور
            </label>

            <label className="checkbox-line">
              <input
                type="checkbox"
                checked={settings.allowStudentExit}
                onChange={(e) =>
                  setSettings({ ...settings, allowStudentExit: e.target.checked })
                }
              />
              مسموح للطالب الخروج من الاختبار بدون إنهاء
            </label>

            <button className="primary-btn full-btn" type="submit" disabled={actionLoading}>
              {actionLoading ? "جاري الحفظ..." : "حفظ الإعدادات"}
            </button>
          </form>
        </AccordionCard>

        <AccordionCard
          id="blueprint"
          activeId={activeCard}
          onToggle={toggleCard}
          title="مخطط ورقة الاختبار"
          subtitle="توزيع إجمالي أسئلة الورقة على مخرجات CLO ومستويات Bloom"
          badge="Blueprint"
        >
          {(() => {
            const total = Number(settings.examQuestionCount || 0);
            const cloDistribution = settings.blueprintCloDistribution || {};
            const bloomDistribution = settings.blueprintBloomDistribution || {};
            const cloTotal = Object.values(cloDistribution).reduce((sum, value) => sum + Number(value || 0), 0);
            const bloomTotal = Object.values(bloomDistribution).reduce((sum, value) => sum + Number(value || 0), 0);
            const cloItems = settings.assessmentType === "CloAligned" ? courseClos : [{ id: "none", code: "بدون CLO", description: "أسئلة عامة غير مرتبطة بمخرج تعلم" }, ...courseClos];
            return <div className="exam-blueprint">
              <div className="blueprint-summary">
                <article><span>إجمالي أسئلة الورقة</span><strong>{total}</strong></article>
                <article className={cloTotal > total ? "invalid" : ""}><span>الموزع على CLO</span><strong>{cloTotal} / {total}</strong><small>المتبقي {Math.max(0, total - cloTotal)}</small></article>
                <article className={bloomTotal > total ? "invalid" : ""}><span>الموزع على Bloom</span><strong>{bloomTotal} / {total}</strong><small>المتبقي {Math.max(0, total - bloomTotal)}</small></article>
              </div>

              <div className="blueprint-columns">
                <section><div className="blueprint-head"><div><h4>تغطية مخرجات CLO</h4><p>حدد نسبة كل مخرج؛ يحولها النظام تلقائيًا إلى عدد أسئلة.</p></div><span>{cloTotal}/{total}</span></div><div className="blueprint-fields">{cloItems.map((item) => { const count = Number(cloDistribution[item.id] || 0); const percentage = total ? Math.round(count * 1000 / total) / 10 : 0; return <label key={item.id}><span><b>{item.code}</b><small>{item.description} · {count} سؤال</small></span><div className="blueprint-percent-input"><input type="number" min="0" max="100" step="1" value={percentage} onChange={(e) => setBlueprintPercentage("clo", item.id, e.target.value)}/><i>%</i></div></label>; })}</div></section>
                <section><div className="blueprint-head"><div><h4>تغطية مستويات Bloom</h4><p>حدد نسبة كل مستوى معرفي من إجمالي الورقة.</p></div><span>{bloomTotal}/{total}</span></div><div className="blueprint-fields">{bloomLevels.map(([key, label]) => { const count = Number(bloomDistribution[key] || 0); const percentage = total ? Math.round(count * 1000 / total) / 10 : 0; return <label key={key}><span><b>{label}</b><small>{key} · {count} سؤال</small></span><div className="blueprint-percent-input"><input type="number" min="0" max="100" step="1" value={percentage} onChange={(e) => setBlueprintPercentage("bloom", key, e.target.value)}/><i>%</i></div></label>; })}</div></section>
              </div>
              {(cloTotal > total || bloomTotal > total) && <div className="blueprint-warning">راجع التوزيع: لا يجوز أن يتجاوز أي مجموع إجمالي أسئلة الورقة.</div>}
              <div className="blueprint-actions"><button type="button" className="ghost-btn" onClick={printBlueprint}>طباعة تقرير ورقة الاختبار</button><button type="button" className="primary-btn" disabled={actionLoading || cloTotal > total || bloomTotal > total} onClick={saveSettings}>حفظ مخطط الورقة</button></div>

              <div className="blueprint-print-sheet">
                <header><span>QuizSystem · Exam Blueprint</span><h1>تقرير مخطط ورقة الاختبار</h1><p>{exam.title} · {exam.examCode}</p></header>
                <div className="print-blueprint-meta"><span>إجمالي الأسئلة <b>{total}</b></span><span>نوع الاختبار <b>{settings.assessmentType === "CloAligned" ? "مرتبط بـ CLO" : "اختبار عام"}</b></span><span>تاريخ التقرير <b>{new Date().toLocaleDateString("ar-SA")}</b></span></div>
                <div className="print-blueprint-tables"><table><caption>توزيع CLO</caption><thead><tr><th>المخرج</th><th>الوصف</th><th>عدد الأسئلة</th></tr></thead><tbody>{cloItems.map((item) => <tr key={item.id}><td>{item.code}</td><td>{item.description}</td><td>{cloDistribution[item.id] || 0}</td></tr>)}</tbody><tfoot><tr><th colSpan="2">المجموع</th><th>{cloTotal}</th></tr></tfoot></table><table><caption>توزيع Bloom</caption><thead><tr><th>المستوى</th><th>Level</th><th>عدد الأسئلة</th></tr></thead><tbody>{bloomLevels.map(([key, label]) => <tr key={key}><td>{label}</td><td>{key}</td><td>{bloomDistribution[key] || 0}</td></tr>)}</tbody><tfoot><tr><th colSpan="2">المجموع</th><th>{bloomTotal}</th></tr></tfoot></table></div>
              </div>
            </div>;
          })()}
        </AccordionCard>

        <AccordionCard
          id="ai-generator"
          activeId={activeCard}
          onToggle={toggleCard}
          title="توليد الأسئلة بالذكاء الاصطناعي"
          subtitle="توليد عدد محدد اعتمادًا على وصف الاختبار، PDF، ومخطط CLO وBloom"
          badge="AI Agent"
        >
          <form className="ai-generator-form" onSubmit={generateWithAi}>
            <div><label>عدد الأسئلة المطلوب</label><input type="number" min="1" max="50" value={aiCount} onChange={(e) => setAiCount(Number(e.target.value))}/></div>
            <div><label>المحتوى التعليمي PDF (اختياري)</label><input type="file" accept="application/pdf,.pdf" onChange={(e) => setAiPdf(e.target.files?.[0] || null)}/></div>
            <div className="ai-source-note"><b>مصادر التوليد</b><span>وصف الاختبار: {exam.description || exam.topic || "غير محدد"}</span><span>المخطط: {settings.assessmentType === "CloAligned" ? "CLO + Bloom" : "عام + Bloom"}</span><span>PDF: {aiPdf?.name || "بدون ملف"}</span><span>{aiPdf ? "يستخرج النظام النص ويلخص المحتوى تعليمياً أولاً، ثم ينشئ الأسئلة من الملخص. لا يُحفظ ملف PDF على الخادم." : "سيتم إنشاء الأسئلة من وصف الاختبار ومخطط الورقة."}</span></div>
            <button className="primary-btn" type="submit" disabled={actionLoading}>{actionLoading ? (aiPdf ? "تلخيص PDF ثم إنشاء الأسئلة…" : "إنشاء الأسئلة…") : "توليد ومعاينة الأسئلة"}</button>
          </form>
          {aiDrafts.length > 0 && <div className="ai-drafts"><div className="ai-drafts-head"><div><h4>معاينة الأسئلة المولدة</h4><p>يمكنك تعديل أي سؤال قبل إضافته إلى البنك.</p></div><button type="button" className="primary-btn" onClick={approveAiDrafts} disabled={actionLoading}>اعتماد جميع الأسئلة ({aiDrafts.length})</button></div>{aiDrafts.map((draft, index) => <article className="ai-draft-card" key={index}>
            <div className="ai-draft-top"><strong>سؤال {index + 1}</strong><select value={draft.cognitiveLevel || "Understand"} onChange={(e) => updateAiDraft(index, "cognitiveLevel", e.target.value)}>{bloomLevels.map(([key, label]) => <option key={key} value={key}>{label} · {key}</option>)}</select><select value={draft.courseLearningOutcomeId || ""} onChange={(e) => updateAiDraft(index, "courseLearningOutcomeId", e.target.value)}><option value="">بدون CLO</option>{courseClos.map((clo) => <option key={clo.id} value={clo.id}>{clo.code}</option>)}</select><button type="button" className="danger-btn slim" onClick={() => setAiDrafts((items) => items.filter((_, i) => i !== index))}>استبعاد</button></div>
            <textarea rows="2" value={draft.questionText || ""} onChange={(e) => updateAiDraft(index, "questionText", e.target.value)}/>
            <div className="ai-choice-grid">{["A","B","C","D"].map((key) => { const field=`choice${key}`; return <label key={key} className={draft.correctAnswer === key ? "correct" : ""}><button type="button" title="تعيين كإجابة صحيحة" onClick={() => updateAiDraft(index, "correctAnswer", key)}>{key}</button><input value={draft[field] || ""} onChange={(e) => updateAiDraft(index, field, e.target.value)}/></label>; })}</div>
          </article>)}</div>}
        </AccordionCard>

        <AccordionCard
          id="questions-editor"
          activeId={activeCard}
          onToggle={toggleCard}
          title={editingQuestionId ? "تعديل سؤال" : "إضافة سؤال"}
          subtitle="السؤال والاختيارات يمكن أن تكون نصًا أو صورة أو الاثنين معًا"
          badge={editingQuestionId ? "Edit" : "Add"}
        >
          <form onSubmit={saveQuestion}>
            <div className="entity-form-grid">
              <div className="entity-form-field">
                <label>مستوى السؤال (Bloom) *</label>
                <select
                  required
                  value={question.cognitiveLevel}
                  onChange={(e) => setQuestionField("cognitiveLevel", e.target.value)}
                >
                  <option value="Remember">تذكر - Remember</option>
                  <option value="Understand">فهم - Understand</option>
                  <option value="Apply">تطبيق - Apply</option>
                  <option value="Analyze">تحليل - Analyze</option>
                  <option value="Evaluate">تقويم - Evaluate</option>
                  <option value="Create">ابتكار - Create</option>
                </select>
              </div>

              <div className="entity-form-field">
                <label>
                  مخرج التعلم CLO {exam.assessmentType === "CloAligned" ? "*" : "(اختياري)"}
                </label>
                <select
                  required={exam.assessmentType === "CloAligned"}
                  value={question.courseLearningOutcomeId}
                  onChange={(e) => setQuestionField("courseLearningOutcomeId", e.target.value)}
                >
                  <option value="">بدون ربط بـ CLO</option>
                  {courseClos.map((clo) => (
                    <option key={clo.id} value={clo.id}>
                      {clo.code} - {clo.description}
                    </option>
                  ))}
                </select>
                {!courseClos.length && (
                  <div className="mini-hint">لا توجد مخرجات تعلم فعالة للمقرر.</div>
                )}
              </div>
            </div>

            <label>نص السؤال</label>
            <textarea
              rows="3"
              value={question.questionText}
              onChange={(e) => setQuestionField("questionText", e.target.value)}
              placeholder="اتركه فارغًا إذا كان السؤال صورة فقط"
            />

            <ImageUploadField
              label="صورة السؤال"
              value={question.questionImageUrl}
              disabled={actionLoading}
              onUploaded={(url) => setQuestionField("questionImageUrl", url)}
              onRemove={() => setQuestionField("questionImageUrl", "")}
            />

            <div className="image-question-grid">
              {[
                ["A", "choiceA", "choiceAImageUrl"],
                ["B", "choiceB", "choiceBImageUrl"],
                ["C", "choiceC", "choiceCImageUrl"],
                ["D", "choiceD", "choiceDImageUrl"],
              ].map(([label, textField, imageField]) => (
                <div className="image-choice-editor-card" key={label}>
                  <h4>الاختيار {label}</h4>

                  <label>نص الاختيار</label>
                  <input
                    value={question[textField]}
                    onChange={(e) => setQuestionField(textField, e.target.value)}
                    placeholder="اتركه فارغًا إذا كان الاختيار صورة فقط"
                  />

                  <ImageUploadField
                    label={`صورة الاختيار ${label}`}
                    value={question[imageField]}
                    disabled={actionLoading}
                    onUploaded={(url) => setQuestionField(imageField, url)}
                    onRemove={() => setQuestionField(imageField, "")}
                  />
                </div>
              ))}
            </div>

            <label>الإجابة الصحيحة</label>
            <select
              value={question.correctAnswer}
              onChange={(e) => setQuestionField("correctAnswer", e.target.value)}
            >
              <option value="A">A</option>
              <option value="B">B</option>
              <option value="C">C</option>
              <option value="D">D</option>
            </select>

            <label>التفسير</label>
            <textarea
              rows="2"
              value={question.explanation}
              onChange={(e) => setQuestionField("explanation", e.target.value)}
            />

            <div className="action-row top-space">
              <button className="primary-btn" type="submit" disabled={actionLoading}>
                {actionLoading
                  ? "جاري التنفيذ..."
                  : editingQuestionId
                  ? "تحديث السؤال"
                  : "إضافة السؤال"}
              </button>

              {editingQuestionId && (
                <button type="button" className="ghost-btn" onClick={cancelEdit}>
                  إلغاء التعديل
                </button>
              )}
            </div>
          </form>

          <div className="template-box top-space">
            <button
              type="button"
              className="ghost-btn"
              onClick={() =>
                handleDownloadFile(
                  `/exams/${examId}/questions/template`,
                  "questions_template.xlsx",
                  "template"
                )
              }
              disabled={downloadLoadingKey === "template"}
            >
              {downloadLoadingKey === "template"
                ? "جاري التنزيل..."
                : "تحميل نموذج الأسئلة"}
            </button>
          </div>

          <form onSubmit={uploadQuestions}>
            <label>رفع أسئلة Excel/CSV</label>
            <input
              type="file"
              accept=".xlsx,.csv"
              onChange={(e) => setFile(e.target.files?.[0] || null)}
            />
            <button className="ghost-btn full-btn" type="submit" disabled={actionLoading}>
              {actionLoading ? "جاري الرفع..." : "رفع الملف"}
            </button>
          </form>
        </AccordionCard>

        <AccordionCard
          id="print"
          activeId={activeCard}
          onToggle={toggleCard}
          title="الطباعة والتنزيل"
          subtitle="جميع التنزيلات تعمل بالتوكن"
          badge="PDF / ZIP"
        >
          <div className="action-row">
            <button
              className="primary-btn"
              disabled={downloadLoadingKey === "questions-no-answers"}
              onClick={() =>
                handleOpenPdf(
                  `/exams/${examId}/pdf/questions?withAnswers=false`,
                  "questions-no-answers"
                )
              }
            >
              {downloadLoadingKey === "questions-no-answers"
                ? "جاري التجهيز..."
                : "فتح الأسئلة بدون إجابات"}
            </button>

            <button
              className="ghost-btn"
              disabled={downloadLoadingKey === "questions-with-answers"}
              onClick={() =>
                handleOpenPdf(
                  `/exams/${examId}/pdf/questions?withAnswers=true`,
                  "questions-with-answers"
                )
              }
            >
              {downloadLoadingKey === "questions-with-answers"
                ? "جاري التجهيز..."
                : "فتح الأسئلة مع الإجابات"}
            </button>

            <button
              className="ghost-btn"
              disabled={downloadLoadingKey === "exam-report"}
              onClick={() => handleOpenPdf(`/reports/exams/${examId}/pdf`, "exam-report")}
            >
              {downloadLoadingKey === "exam-report"
                ? "جاري التجهيز..."
                : "تقرير الاختبار"}
            </button>
          </div>

          <label className="top-space">عدد النماذج</label>
          <input
            type="number"
            min="1"
            max="26"
            value={formsCount}
            onChange={(e) => setFormsCount(Number(e.target.value) || 1)}
          />

          <div className="action-row top-space">
            <button
              className="primary-btn"
              disabled={downloadLoadingKey === "forms-zip"}
              onClick={() =>
                handleDownloadFile(
                  `/exams/${examId}/pdf/random-forms?formsCount=${formsCount}`,
                  `exam_forms_${formsCount}.zip`,
                  "forms-zip"
                )
              }
            >
              {downloadLoadingKey === "forms-zip"
                ? "جاري التنزيل..."
                : "تنزيل النماذج في ZIP"}
            </button>

            <button
              className="ghost-btn"
              disabled={downloadLoadingKey === "answer-keys-zip"}
              onClick={() =>
                handleDownloadFile(
                  `/exams/${examId}/pdf/random-forms-answer-keys?formsCount=${formsCount}`,
                  `exam_answer_keys_${formsCount}.zip`,
                  "answer-keys-zip"
                )
              }
            >
              {downloadLoadingKey === "answer-keys-zip"
                ? "جاري التنزيل..."
                : "تنزيل مفاتيح الإجابة في ZIP"}
            </button>
          </div>
        </AccordionCard>

        <AccordionCard
          id="questions-list"
          activeId={activeCard}
          onToggle={toggleCard}
          title="الأسئلة"
          subtitle="مراجعة وتعديل وحذف"
          badge={`${questionCount} سؤال`}
        >
          {(exam.questions || []).length === 0 ? (
            <div className="empty-box">لا توجد أسئلة حتى الآن</div>
          ) : (
            exam.questions.map((q, idx) => (
              <div className="question-card" key={q.id}>
                <QuestionVisual item={q} />

                <div className="top-space">
                  <strong>الإجابة:</strong> {q.correctAnswer}
                </div>
                <div>
                  <strong>التفسير:</strong> {q.explanation || "-"}
                </div>
                <div className="action-row top-space">
                  <span className="mini-pill">المستوى: {q.cognitiveLevel || "Understand"}</span>
                  <span className="mini-pill">CLO: {q.cloCode || "غير مرتبط"}</span>
                </div>

                <div className="action-row top-space">
                  <button
                    className="primary-btn"
                    type="button"
                    onClick={() => fillQuestionForEdit(q)}
                  >
                    تعديل
                  </button>
                  <button
                    className="ghost-btn"
                    type="button"
                    onClick={() => removeQuestion(q.id)}
                    disabled={actionLoading}
                  >
                    حذف
                  </button>
                </div>
              </div>
            ))
          )}
        </AccordionCard>

        <AccordionCard
          id="attempts"
          activeId={activeCard}
          onToggle={toggleCard}
          title="المحاولات والترتيب"
          subtitle="Attempts + Leaderboard"
          badge={`${attempts.length} محاولة`}
        >
          <div className="attempts-tools">
            <label>
              <span>البحث عن طالب</span>
              <input value={attemptSearch} onChange={(event) => setAttemptSearch(event.target.value)} placeholder="ابحث بالاسم أو كود الطالب" />
            </label>
            <span className="mini-pill">{filteredAttempts.length} نتيجة</span>
          </div>
          <div className="table-wrap attempts-scroll-frame">
            <table className="app-table">
              <thead>
                <tr>
                  <th>الطالب</th>
                  <th>الكود</th>
                  <th>النتيجة</th>
                  <th>النسبة</th>
                  <th>الحالة</th>
                  <th>رقم المحاولة</th>
                  <th>وقت التسليم</th>
                  <th>إجراء</th>
                </tr>
              </thead>
              <tbody>
                {filteredAttempts.length === 0 ? (
                  <tr>
                    <td className="empty-cell" colSpan="8">
                      لا توجد محاولات
                    </td>
                  </tr>
                ) : (
                  filteredAttempts.map((a) => (
                    <tr key={a.attemptId}>
                      <td>{a.studentName}</td>
                      <td>{a.studentCode}</td>
                      <td>
                        {a.score} / {a.totalQuestions}
                      </td>
                      <td>{a.percentage}%</td>
                      <td>{a.status}</td>
                      <td>{attempts.filter((row) => row.studentId === a.studentId).sort((x, y) => new Date(x.startedAtUtc) - new Date(y.startedAtUtc)).findIndex((row) => row.attemptId === a.attemptId) + 1}</td>
                      <td>{a.submittedAtUtc ? formatSaudiDateTime(a.submittedAtUtc) : "لم تُسلّم"}</td>
                      <td>
                        {(a.status === "Submitted" || a.status === "ClosedForViolation") && (
                          <Link className="primary-btn" to={`/admin/exams/${examId}/attempts/${a.attemptId}`}>
                            مراجعة وطباعة
                          </Link>
                        )}
                        <button
                          className="ghost-btn"
                          type="button"
                          onClick={() => handleResetAttempt(a.attemptId)}
                          disabled={actionLoading}
                        >
                          Reset
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="section-head top-space leaderboard-head">
            <div>
              <h3>ترتيب الطلاب</h3>
              <p>اختر عدد المراكز الأولى المطلوب عرضها أو طباعتها.</p>
            </div>
            <div className="leaderboard-actions">
              <label>
                <span>عرض أول</span>
                <select value={leaderboardLimit} onChange={(event) => setLeaderboardLimit(Number(event.target.value))}>
                  {[5, 10, 20, 50, 100].map((value) => <option key={value} value={value}>{value}</option>)}
                </select>
              </label>
              <Link className="primary-btn" to={`/admin/exams/${examId}/leaderboard-report?limit=${leaderboardLimit}`} target="_blank">
                طباعة تقرير الترتيب
              </Link>
            </div>
          </div>

          <div className="table-wrap">
            <table className="app-table">
              <thead>
                <tr>
                  <th>الترتيب</th>
                  <th>الطالب</th>
                  <th>الكود</th>
                  <th>الدرجة</th>
                  <th>النسبة</th>
                </tr>
              </thead>
              <tbody>
                {leaderboard.length === 0 ? (
                  <tr>
                    <td className="empty-cell" colSpan="5">
                      لا يوجد ترتيب
                    </td>
                  </tr>
                ) : (
                  visibleLeaderboard.map((row) => (
                    <tr key={row.studentId}>
                      <td>{row.rank}</td>
                      <td>{row.studentName}</td>
                      <td>{row.studentCode}</td>
                      <td>
                        {row.score} / {row.totalQuestions}
                      </td>
                      <td>{row.percentage}%</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </AccordionCard>
      </div>
    </div>
  );
}

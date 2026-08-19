import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  createAiExam,
  createManualExam,
  getExams,
  getAssignedCourses,
  getReadableErrorMessage,
  openPdfWithAuth,
} from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";
import PageIntro from "../../components/ui/PageIntro";
import SectionCard from "../../components/ui/SectionCard";

const initialForm = {
  title: "",
  topic: "",
  description: "",
  examCode: "",
  startAtUtc: "",
  endAtUtc: "",
  bankQuestionCount: 20,
  examQuestionCount: 10,
  subjectId: "",
  assessmentType: "General",
  maxAttempts: 1,
};

function formatDate(value) {
  if (!value) return "-";
  try {
    return formatSaudiDateTime(value);
  } catch {
    return value;
  }
}

function getExamStatus(exam) {
  const now = new Date();
  const start = exam?.startAtUtc ? new Date(exam.startAtUtc) : null;
  const end = exam?.endAtUtc ? new Date(exam.endAtUtc) : null;
  const isPublished = exam?.isPublished ?? false;

  if (!isPublished) return { label: "غير منشور", tone: "status-warning" };
  if (start && now < start) return { label: "لم يبدأ", tone: "status-warning" };
  if (end && now > end) return { label: "منتهي", tone: "status-danger" };
  return { label: "نشط", tone: "status-success" };
}

export default function ExamsPage() {
  const [exams, setExams] = useState([]);
  const [subjects, setSubjects] = useState([]);
  const [search, setSearch] = useState("");
  const [mode, setMode] = useState("manual");
  const [pageLoading, setPageLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [form, setForm] = useState(initialForm);

  async function load() {
    try {
      setPageLoading(true);
      setError("");
      const [data, subjectData] = await Promise.all([getExams(), getAssignedCourses()]);
      setExams(Array.isArray(data) ? data : []);
      setSubjects(Array.isArray(subjectData) ? subjectData : []);
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحميل الاختبارات"));
    } finally {
      setPageLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const filteredExams = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return exams;

    return exams.filter((exam) => {
      const title = (exam?.title || "").toLowerCase();
      const topic = (exam?.topic || "").toLowerCase();
      const code = (exam?.examCode || "").toLowerCase();
      return title.includes(q) || topic.includes(q) || code.includes(q);
    });
  }, [exams, search]);

  const stats = useMemo(() => {
    const total = exams.length;
    const published = exams.filter((x) => x?.isPublished).length;
    const active = exams.filter((x) => getExamStatus(x).label === "نشط").length;
    const ended = exams.filter((x) => getExamStatus(x).label === "منتهي").length;
    return { total, published, active, ended };
  }, [exams]);

  function resetForm() {
    setForm(initialForm);
    setMode("manual");
  }

  function openModal() {
    setError("");
    setSuccess("");
    resetForm();
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    resetForm();
  }

  async function handleCreate(e) {
    e.preventDefault();

    try {
      setSaving(true);
      setError("");
      setSuccess("");

      const payload = {
        title: form.title,
        topic: form.topic,
        description: form.description,
        examCode: form.examCode,
        startAtUtc: form.startAtUtc,
        endAtUtc: form.endAtUtc,
        examQuestionCount: Number(form.examQuestionCount),
        subjectId: form.subjectId || null,
        assessmentType: form.assessmentType,
        maxAttempts: Number(form.maxAttempts),
      };

      if (mode === "ai") {
        await createAiExam({
          ...payload,
          bankQuestionCount: Number(form.bankQuestionCount),
        });
      } else {
        await createManualExam(payload);
      }

      setSuccess("تم إنشاء الاختبار بنجاح");
      closeModal();
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل إنشاء الاختبار"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="exams-admin-page">
      <PageIntro
        title="إدارة الاختبارات"
        description="إدارة احترافية للاختبارات اليدوية واختبارات الذكاء الاصطناعي مع وصول سريع للأسئلة وملفات PDF."
      />

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <section className="entity-hero">
        <div className="entity-hero-copy">
          <span className="entity-badge">Exams Center</span>
          <h2>لوحة الاختبارات</h2>
          <p>
            أنشئ اختبارًا يدويًا أو بالذكاء الاصطناعي، وادخل مباشرة على إدارة الأسئلة أو ملفات الطباعة.
          </p>
        </div>

        <div className="entity-hero-stats">
          <div className="entity-hero-stat">
            <span>إجمالي الاختبارات</span>
            <strong>{stats.total}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>المنشور</span>
            <strong>{stats.published}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>النشط الآن</span>
            <strong>{stats.active}</strong>
          </div>
        </div>
      </section>

      <SectionCard
        title="قائمة الاختبارات"
        subtitle="بحث سريع، إنشاء جديد، وإجراءات مباشرة لكل اختبار"
      >
        <div className="entity-toolbar">
          <div className="entity-search-box">
            <label>بحث</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="ابحث بعنوان الاختبار أو الموضوع أو الكود"
            />
          </div>

          <div className="entity-toolbar-actions">
            <button className="primary-btn slim" type="button" onClick={openModal}>
              إنشاء اختبار
            </button>
          </div>
        </div>

        {pageLoading ? (
          <div className="empty-box top-space">جاري التحميل...</div>
        ) : filteredExams.length === 0 ? (
          <div className="empty-box top-space">لا يوجد اختبارات</div>
        ) : (
          <div className="entity-cards-grid top-space">
            {filteredExams.map((exam) => {
              const status = getExamStatus(exam);

              return (
                <div className="entity-card" key={exam.id || exam.examCode}>
                  <div className="entity-card-head">
                    <div>
                      <h3>{exam.title || "اختبار"}</h3>
                      <p>{exam.topic || "بدون موضوع"}</p>
                    </div>
                    <span className="mini-pill">{exam.examCode || "بدون كود"}</span>
                  </div>

                  <div className="entity-card-body">
                    <div className="entity-meta-row">
                      <span>الحالة</span>
                      <strong>
                        <span className={`status-badge ${status.tone}`}>{status.label}</span>
                      </strong>
                    </div>

                    <div className="entity-meta-row">
                      <span>البداية</span>
                      <strong>{formatDate(exam.startAtUtc)}</strong>
                    </div>

                    <div className="entity-meta-row">
                      <span>النهاية</span>
                      <strong>{formatDate(exam.endAtUtc)}</strong>
                    </div>

                    <div className="entity-meta-row">
                      <span>عدد أسئلة الاختبار</span>
                      <strong>{exam.examQuestionCount ?? "-"}</strong>
                    </div>
                  </div>

                  <div className="entity-card-actions">
                    <Link className="ghost-btn slim" to={`/admin/exams/${exam.id}`}>
                      إدارة الاختبار
                    </Link>

                    <button
                      className="ghost-btn slim"
                      type="button"
                      onClick={() => openPdfWithAuth(`/exams/${exam.id}/pdf/questions`)}
                    >
                      PDF الأسئلة
                    </button>

                    <button
                      className="ghost-btn slim"
                      type="button"
                      onClick={() =>
                        openPdfWithAuth(`/exams/${exam.id}/pdf/questions?withAnswers=true`)
                      }
                    >
                      PDF الإجابات
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </SectionCard>

      {isModalOpen && (
        <div className="entity-modal-backdrop" onClick={closeModal}>
          <div className="entity-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="entity-modal-head">
              <div>
                <h2>إنشاء اختبار جديد</h2>
                <p>اختر النمط المناسب ثم أدخل بيانات الاختبار</p>
              </div>

              <button className="ghost-btn slim" type="button" onClick={closeModal}>
                إغلاق
              </button>
            </div>

            <div className="entity-segmented">
              <button
                type="button"
                className={mode === "manual" ? "segment-btn active" : "segment-btn"}
                onClick={() => setMode("manual")}
              >
                يدوي
              </button>
              <button
                type="button"
                className={mode === "ai" ? "segment-btn active" : "segment-btn"}
                onClick={() => setMode("ai")}
              >
                AI
              </button>
            </div>

            <form className="entity-form-grid" onSubmit={handleCreate}>
              <div className="entity-form-field">
                <label>المقرر</label>
                <select required value={form.subjectId} onChange={(e) => setForm({ ...form, subjectId: e.target.value })}>
                  <option value="">اختر المقرر</option>
                  {subjects.map((subject) => <option key={subject.id} value={subject.id}>{subject.name} ({subject.code})</option>)}
                </select>
              </div>

              <div className="entity-form-field">
                <label>نوع الاختبار</label>
                <select value={form.assessmentType} onChange={(e) => setForm({ ...form, assessmentType: e.target.value })}>
                  <option value="General">اختبار عام حسب مستويات الأسئلة</option>
                  <option value="CloAligned">اختبار مرتبط بمخرجات التعلم CLO</option>
                </select>
              </div>

              <div className="entity-form-field">
                <label>عدد المحاولات المسموح</label>
                <input type="number" min="1" max="20" required value={form.maxAttempts} onChange={(e) => setForm({ ...form, maxAttempts: e.target.value })} />
              </div>
              <div className="entity-form-field">
                <label>عنوان الاختبار</label>
                <input
                  value={form.title}
                  onChange={(e) => setForm({ ...form, title: e.target.value })}
                  placeholder="مثال: الرياضيات"
                />
              </div>

              <div className="entity-form-field">
                <label>كود الاختبار</label>
                <input
                  value={form.examCode}
                  onChange={(e) => setForm({ ...form, examCode: e.target.value })}
                  placeholder="Exam-001"
                />
              </div>

              <div className="entity-form-field">
                <label>الموضوع</label>
                <input
                  value={form.topic}
                  onChange={(e) => setForm({ ...form, topic: e.target.value })}
                  placeholder="الجبر، التاريخ، العلوم..."
                />
              </div>

              <div className="entity-form-field">
                <label>عدد أسئلة الاختبار</label>
                <input
                  type="number"
                  value={form.examQuestionCount}
                  onChange={(e) =>
                    setForm({ ...form, examQuestionCount: e.target.value })
                  }
                />
              </div>

              {mode === "ai" && (
                <div className="entity-form-field">
                  <label>عدد أسئلة البنك</label>
                  <input
                    type="number"
                    value={form.bankQuestionCount}
                    onChange={(e) =>
                      setForm({ ...form, bankQuestionCount: e.target.value })
                    }
                  />
                </div>
              )}

              <div className="entity-form-field">
                <label>البداية</label>
                <input
                  type="datetime-local"
                  value={form.startAtUtc}
                  onChange={(e) => setForm({ ...form, startAtUtc: e.target.value })}
                />
              </div>

              <div className="entity-form-field">
                <label>النهاية</label>
                <input
                  type="datetime-local"
                  value={form.endAtUtc}
                  onChange={(e) => setForm({ ...form, endAtUtc: e.target.value })}
                />
              </div>

              <div className="entity-form-field entity-form-field-wide">
                <label>الوصف</label>
                <textarea
                  rows="4"
                  value={form.description}
                  onChange={(e) => setForm({ ...form, description: e.target.value })}
                  placeholder="وصف مختصر عن الاختبار"
                />
              </div>

              <div className="entity-form-actions entity-form-field-wide">
                <button className="primary-btn" type="submit" disabled={saving}>
                  {saving ? "جاري الإنشاء..." : "إنشاء الاختبار"}
                </button>
                <button className="ghost-btn" type="button" onClick={closeModal}>
                  إلغاء
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

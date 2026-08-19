import { useEffect, useMemo, useState } from "react";
import { Download, Users, Trash2 } from "lucide-react";
import {
  downloadFileWithAuth,
  getExams,
  getReadableErrorMessage,
  getStudentLookups,
  registerStudentToExam,
  uploadRegistrations,
  apiRequest,
  getExamCourseSections,
  registerSectionToExam,
} from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";
import PageIntro from "../../components/ui/PageIntro";
import SectionCard from "../../components/ui/SectionCard";

async function getRegistrationSummary() {
  return apiRequest("/admin/registrations/summary");
}

async function getExamRegistrations(examId) {
  return apiRequest(`/admin/registrations/exams/${examId}`);
}

async function exportExamRegistrations(examId) {
  return downloadFileWithAuth(
    `/admin/registrations/exams/${examId}/export`,
    `exam-${examId}-registrations.csv`
  );
}

async function deleteRegistration(registrationId) {
  return apiRequest(`/admin/registrations/${registrationId}`, {
    method: "DELETE",
  });
}

async function clearExamRegistrations(examId) {
  return apiRequest(`/admin/registrations/exams/${examId}`, {
    method: "DELETE",
  });
}

function getExamTitle(exam) {
  return exam?.title || "اختبار";
}

function getExamCode(exam) {
  return exam?.examCode || exam?.code || "";
}

function getStudentName(student) {
  return student?.fullName || student?.name || student?.studentName || "طالب";
}

function getStudentCode(student) {
  return student?.studentCode || student?.code || "";
}

function formatDate(value) {
  if (!value) return "-";
  try {
    return formatSaudiDateTime(value);
  } catch {
    return value;
  }
}

export default function RegistrationsPage() {
  const [exams, setExams] = useState([]);
  const [students, setStudents] = useState([]);
  const [summary, setSummary] = useState([]);
  const [selectedExamId, setSelectedExamId] = useState("");
  const [selectedStudentId, setSelectedStudentId] = useState("");
  const [registrationMode, setRegistrationMode] = useState("student");
  const [sections, setSections] = useState([]);
  const [selectedSectionId, setSelectedSectionId] = useState("");
  const [file, setFile] = useState(null);

  const [search, setSearch] = useState("");
  const [pageLoading, setPageLoading] = useState(true);
  const [loading, setLoading] = useState(false);
  const [downloadLoading, setDownloadLoading] = useState(false);

  const [showManualModal, setShowManualModal] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [showManageModal, setShowManageModal] = useState(false);

  const [manageExam, setManageExam] = useState(null);
  const [manageRows, setManageRows] = useState([]);
  const [manageLoading, setManageLoading] = useState(false);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function load() {
    try {
      setPageLoading(true);
      setError("");

      const [examsData, studentsData, summaryData] = await Promise.all([
        getExams(),
        getStudentLookups(),
        getRegistrationSummary(),
      ]);

      setExams(Array.isArray(examsData) ? examsData : []);
      setStudents(Array.isArray(studentsData) ? studentsData : []);
      setSummary(Array.isArray(summaryData) ? summaryData : []);
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحميل صفحة التسجيلات"));
    } finally {
      setPageLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const filteredSummary = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return summary;

    return summary.filter((item) => {
      const title = (item?.examTitle || item?.title || "").toLowerCase();
      const code = (item?.examCode || item?.code || "").toLowerCase();
      return title.includes(q) || code.includes(q);
    });
  }, [summary, search]);

  function resetMessages() {
    setError("");
    setSuccess("");
  }

  async function submitManual(e) {
    e.preventDefault();

    try {
      setLoading(true);
      resetMessages();

      if (!selectedExamId) throw new Error("اختر الاختبار أولًا");
      if (registrationMode === "student") {
        if (!selectedStudentId) throw new Error("اختر الطالب أولًا");
        await registerStudentToExam(selectedExamId, selectedStudentId);
        setSuccess("تم تسجيل الطالب على الاختبار");
      } else {
        if (!selectedSectionId) throw new Error("اختر الشعبة أولًا");
        const result = await registerSectionToExam(selectedExamId, selectedSectionId);
        setSuccess(`تم تسجيل ${result.added + result.reactivated} طالب فعال، وتجاوز ${result.skipped} مسجل سابقًا`);
      }
      setSelectedStudentId("");
      setShowManualModal(false);
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تسجيل الطالب"));
    } finally {
      setLoading(false);
    }
  }

  async function submitUpload(e) {
    e.preventDefault();

    if (!file) {
      setError("اختر ملف التسجيلات أولًا");
      return;
    }

    try {
      setLoading(true);
      resetMessages();

      const res = await uploadRegistrations(file);
      setSuccess(`تم رفع التسجيلات. المضاف ${res?.inserted ?? 0} والمتخطي ${res?.skipped ?? 0}`);
      setFile(null);
      setShowUploadModal(false);
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل رفع ملف التسجيلات"));
    } finally {
      setLoading(false);
    }
  }

  async function downloadTemplate() {
    try {
      setDownloadLoading(true);
      resetMessages();
      await downloadFileWithAuth(
        "/imports/registrations/template",
        "registrations_template.xlsx"
      );
      setSuccess("تم تنزيل نموذج التسجيلات");
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تنزيل القالب"));
    } finally {
      setDownloadLoading(false);
    }
  }

  async function openManageModal(item) {
    try {
      setManageExam(item);
      setShowManageModal(true);
      setManageLoading(true);
      resetMessages();

      const rows = await getExamRegistrations(item.examId || item.id);
      setManageRows(Array.isArray(rows) ? rows : []);
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحميل المسجلين"));
    } finally {
      setManageLoading(false);
    }
  }

  async function handleExport(item) {
    try {
      setDownloadLoading(true);
      resetMessages();
      await exportExamRegistrations(item.examId || item.id);
      setSuccess("تم تصدير قائمة المسجلين");
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل <Download size={20} />"));
    } finally {
      setDownloadLoading(false);
    }
  }

  async function handleDeleteRegistration(registrationId) {
    const ok = window.confirm("هل تريد حذف هذا التسجيل؟");
    if (!ok) return;

    try {
      resetMessages();
      await deleteRegistration(registrationId);
      setSuccess("تم حذف التسجيل");
      if (manageExam) {
        const rows = await getExamRegistrations(manageExam.examId || manageExam.id);
        setManageRows(Array.isArray(rows) ? rows : []);
      }
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حذف التسجيل"));
    }
  }

  async function handleClearAll(item) {
    const ok = window.confirm("هل تريد <Trash2 size={20} /> لهذا الاختبار؟");
    if (!ok) return;

    try {
      resetMessages();
      await clearExamRegistrations(item.examId || item.id);
      setSuccess("تم <Trash2 size={20} />");
      setShowManageModal(false);
      setManageRows([]);
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل <Trash2 size={20} />"));
    }
  }

  return (
    <div className="entity-page">
      <PageIntro
        title="إدارة التسجيلات"
        description="إدارة تسجيل الطلاب على الاختبارات من خلال واجهة احترافية تدعم التسجيل اليدوي والرفع الجماعي وإدارة المسجلين."
      />

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <section className="entity-hero">
        <div className="entity-hero-copy">
          <span className="entity-badge">Registrations Center</span>
          <h2>لوحة تسجيلات الاختبارات</h2>
          <p>
            اعرض عدد المسجلين في كل اختبار، صدّر القائمة، عدّل المسجلين، أو ألغِهم جميعًا من نفس الصفحة.
          </p>
        </div>

        <div className="entity-hero-stats">
          <div className="entity-hero-stat">
            <span>إجمالي الاختبارات</span>
            <strong>{summary.length}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>إجمالي التسجيلات</span>
            <strong>{summary.reduce((sum, x) => sum + (x.registeredCount || 0), 0)}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>الطلاب المتاحون</span>
            <strong>{students.length}</strong>
          </div>
        </div>
      </section>

      <SectionCard
        title="قائمة الاختبارات"
        subtitle="اضغط على عدد المسجلين للتصدير، أو افتح إدارة المسجلين للتعديل والحذف"
      >
        <div className="entity-toolbar">
          <div className="entity-search-box">
            <label>بحث</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="ابحث باسم الاختبار أو الكود"
            />
          </div>

          <div className="entity-toolbar-actions">
            <button className="primary-btn slim" type="button" onClick={() => setShowManualModal(true)}>
              تسجيل طالب
            </button>
            <button className="ghost-btn slim" type="button" onClick={() => setShowUploadModal(true)}>
              رفع التسجيلات
            </button>
            <button className="ghost-btn slim" type="button" onClick={downloadTemplate} disabled={downloadLoading}>
              {downloadLoading ? "جاري التنزيل..." : "تنزيل القالب"}
            </button>
          </div>
        </div>

        {pageLoading ? (
          <div className="empty-box top-space">جاري التحميل...</div>
        ) : filteredSummary.length === 0 ? (
          <div className="empty-box top-space">لا توجد اختبارات</div>
        ) : (
          <div className="entity-cards-grid top-space">
            {filteredSummary.map((item) => (
              <div className="entity-card" key={item.examId || item.id}>
                <div className="entity-card-head">
                  <div>
                    <h3>{item.examTitle || getExamTitle(item)}</h3>
                    <p>{item.examCode || getExamCode(item) || "بدون كود"}</p>
                  </div>
                  <span className="mini-pill">
                    {item.registeredCount ?? 0} مسجل
                  </span>
                </div>

                <div className="entity-card-body">
                  <div className="entity-meta-row">
                    <span>البداية</span>
                    <strong>{formatDate(item.startAtUtc)}</strong>
                  </div>
                  <div className="entity-meta-row">
                    <span>النهاية</span>
                    <strong>{formatDate(item.endAtUtc)}</strong>
                  </div>
                </div>

                <div className="entity-card-actions">
                 <button className="icon-action-btn export" onClick={() => handleExport(item)}>
  <Download size={20} />
  <span className="icon-tooltip">تصدير المسجلين</span>
</button>
                  <button className="icon-action-btn edit" onClick={() => openManageModal(item)}>
  <Users size={20} />
  <span className="icon-tooltip">تعديل المسجلين</span>
</button>
                  <button className="icon-action-btn danger" onClick={() => handleClearAll(item)}>
  <Trash2 size={20} />
  <span className="icon-tooltip">إلغاء جميع المسجلين</span>
</button>
                </div>
              </div>
            ))}
          </div>
        )}
      </SectionCard>

      {showManualModal && (
        <div className="entity-modal-backdrop" onClick={() => setShowManualModal(false)}>
          <div className="entity-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="entity-modal-head">
              <div>
                <h2>تسجيل طالب على اختبار</h2>
                <p>اختر الاختبار والطالب ثم احفظ</p>
              </div>
              <button className="ghost-btn slim" type="button" onClick={() => setShowManualModal(false)}>
                إغلاق
              </button>
            </div>

            <form className="entity-form-grid" onSubmit={submitManual}>
              <div className="entity-form-field entity-form-field-wide">
                <label>طريقة التسجيل</label>
                <select value={registrationMode} onChange={(e) => setRegistrationMode(e.target.value)}>
                  <option value="student">اختيار طالب</option>
                  <option value="section">اختيار شعبة كاملة</option>
                </select>
              </div>
              <div className="entity-form-field entity-form-field-wide">
                <label>الاختبار</label>
                <select value={selectedExamId} onChange={async (e) => { const id=e.target.value; setSelectedExamId(id); setSelectedSectionId(""); setSections(id ? await getExamCourseSections(id) : []); }}>
                  <option value="">اختر اختبارًا</option>
                  {exams.map((exam) => (
                    <option key={exam.id} value={exam.id}>
                      {getExamTitle(exam)} - {getExamCode(exam)}
                    </option>
                  ))}
                </select>
              </div>

              {registrationMode === "student" ? <div className="entity-form-field entity-form-field-wide">
                <label>الطالب</label>
                <select value={selectedStudentId} onChange={(e) => setSelectedStudentId(e.target.value)}>
                  <option value="">اختر طالبًا</option>
                  {students.map((student) => (
                    <option key={student.id} value={student.id}>
                      {getStudentName(student)} - {getStudentCode(student)}
                    </option>
                  ))}
                </select>
              </div> : <div className="entity-form-field entity-form-field-wide">
                <label>شعبة المقرر</label>
                <select value={selectedSectionId} onChange={(e) => setSelectedSectionId(e.target.value)}>
                  <option value="">اختر شعبة</option>
                  {sections.map((section) => <option key={section.id} value={section.id}>{section.name} - {section.activeStudentsCount} طالب فعال</option>)}
                </select>
                <small>تظهر شعب المقرر المرتبط بالاختبار فقط، ويتم تسجيل الطلاب بالحالة فعال.</small>
              </div>}

              <div className="entity-form-actions entity-form-field-wide">
                <button className="primary-btn" type="submit" disabled={loading}>
                  {loading ? "جاري الحفظ..." : "تسجيل الطالب"}
                </button>
                <button className="ghost-btn" type="button" onClick={() => setShowManualModal(false)}>
                  إلغاء
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showUploadModal && (
        <div className="entity-modal-backdrop" onClick={() => setShowUploadModal(false)}>
          <div className="entity-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="entity-modal-head">
              <div>
                <h2>رفع ملف التسجيلات</h2>
                <p>ارفع ملف Excel أو CSV للتسجيل الجماعي</p>
              </div>
              <button className="ghost-btn slim" type="button" onClick={() => setShowUploadModal(false)}>
                إغلاق
              </button>
            </div>

            <form className="entity-form-grid" onSubmit={submitUpload}>
              <div className="entity-form-field entity-form-field-wide">
                <label>ملف التسجيلات</label>
                <input
                  type="file"
                  accept=".xlsx,.xls,.csv"
                  onChange={(e) => setFile(e.target.files?.[0] || null)}
                />
              </div>

              <div className="entity-form-actions entity-form-field-wide">
                <button className="primary-btn" type="submit" disabled={loading}>
                  {loading ? "جاري الرفع..." : "رفع التسجيلات"}
                </button>
                <button className="ghost-btn" type="button" onClick={() => setShowUploadModal(false)}>
                  إلغاء
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showManageModal && (
        <div className="entity-modal-backdrop" onClick={() => setShowManageModal(false)}>
          <div className="entity-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="entity-modal-head">
              <div>
                <h2>إدارة المسجلين</h2>
                <p>{manageExam?.examTitle || manageExam?.title || "الاختبار"}</p>
              </div>
              <button className="ghost-btn slim" type="button" onClick={() => setShowManageModal(false)}>
                إغلاق
              </button>
            </div>

            {manageLoading ? (
              <div className="empty-box">جاري تحميل المسجلين...</div>
            ) : manageRows.length === 0 ? (
              <div className="empty-box">لا يوجد مسجلون لهذا الاختبار</div>
            ) : (
              <div className="table-wrap">
                <table className="app-table">
                  <thead>
                    <tr>
                      <th>الطالب</th>
                      <th>الكود</th>
                      <th>تاريخ التسجيل</th>
                      <th>إجراء</th>
                    </tr>
                  </thead>
                  <tbody>
                    {manageRows.map((row) => (
                      <tr key={row.id}>
                        <td>{row.studentName || row.fullName || "-"}</td>
                        <td>{row.studentCode || row.code || "-"}</td>
                        <td>{formatDate(row.assignedAtUtc || row.createdAtUtc)}</td>
                        <td>
                          <button
                            className="ghost-btn slim"
                            type="button"
                            onClick={() => handleDeleteRegistration(row.id)}
                          >
                            حذف التسجيل
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            <div className="entity-form-actions top-space">
              <button
                className="ghost-btn"
                type="button"
                onClick={() => manageExam && handleClearAll(manageExam)}
              >
                <Trash2 size={20} />
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

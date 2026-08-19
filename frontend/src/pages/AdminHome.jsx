import React, { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  createAiExam,
  createManualExam,
  createStudent,
  getAdminDashboard,
  getExams,
  getStudentLookups,
  registerStudentToExam,
  uploadRegistrations,
  uploadStudents,
  fileUrl,
} from "../services/api";

const emptyStats = {
  exams: 0,
  students: 0,
  parents: 0,
  registrations: 0,
  attempts: 0,
};

const studentInitial = {
  fullName: "",
  studentCode: "",
  grade: "",
  userName: "",
  password: "",
};

const aiExamInitial = {
  title: "",
  topic: "",
  description: "",
  examCode: "",
  startAtUtc: "",
  endAtUtc: "",
  bankQuestionCount: 20,
  examQuestionCount: 10,
};

const manualExamInitial = {
  title: "",
  topic: "",
  description: "",
  examCode: "",
  startAtUtc: "",
  endAtUtc: "",
  examQuestionCount: 10,
};

function normalizeDashboard(data) {
  const source = data || {};

  if (source.stats) {
    return {
      stats: {
        exams: source.stats.exams || 0,
        students: source.stats.students || 0,
        parents: source.stats.parents || 0,
        registrations: source.stats.registrations || 0,
        attempts: source.stats.attempts || 0,
      },
    };
  }

  return {
    stats: {
      exams: source.examsCount || source.exams || 0,
      students: source.studentsCount || source.students || 0,
      parents: source.parentsCount || source.parents || 0,
      registrations: source.registrationsCount || source.registrations || 0,
      attempts: source.attemptsCount || source.attempts || 0,
    },
  };
}

function normalizeExam(exam) {
  return {
    id: exam?.id || "",
    title: exam?.title || "",
    examCode: exam?.examCode || exam?.exam_code || "",
    bankQuestionCount: exam?.bankQuestionCount ?? exam?.bank_question_count ?? 0,
    examQuestionCount: exam?.examQuestionCount ?? exam?.exam_question_count ?? 0,
    registeredStudents: exam?.registeredStudents ?? exam?.registered_students ?? 0,
    attemptCount: exam?.attemptCount ?? exam?.attempt_count ?? 0,
  };
}

function normalizeStudent(student) {
  return {
    id: student?.id || "",
    fullName: student?.fullName || student?.full_name || "",
    studentCode: student?.studentCode || student?.student_code || "",
  };
}

function toDateTimeValue(value) {
  if (!value) return "";
  if (value.length >= 16) return value.slice(0, 16);
  return value;
}

export default function AdminHome() {
  const [dashboard, setDashboard] = useState({ stats: emptyStats });
  const [exams, setExams] = useState([]);
  const [students, setStudents] = useState([]);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const [studentForm, setStudentForm] = useState(studentInitial);
  const [aiExamForm, setAiExamForm] = useState(aiExamInitial);
  const [manualExamForm, setManualExamForm] = useState(manualExamInitial);

  const [studentsFile, setStudentsFile] = useState(null);
  const [registrationsFile, setRegistrationsFile] = useState(null);

  const [registerExamId, setRegisterExamId] = useState("");
  const [registerStudentId, setRegisterStudentId] = useState("");
  const [quizSearch, setQuizSearch] = useState("");
  const [studentSearch, setStudentSearch] = useState("");

  const [openSection, setOpenSection] = useState("students");
  const [examMode, setExamMode] = useState("ai");
  const [loading, setLoading] = useState(false);

  function resetMessages() {
    setError("");
    setSuccess("");
  }

  async function loadAll() {
    try {
      setLoading(true);
      resetMessages();

      const [dashboardData, examsData, studentsData] = await Promise.all([
        getAdminDashboard(),
        getExams(),
        getStudentLookups(),
      ]);

      setDashboard(normalizeDashboard(dashboardData));
      setExams(Array.isArray(examsData) ? examsData.map(normalizeExam) : []);
      setStudents(Array.isArray(studentsData) ? studentsData.map(normalizeStudent) : []);
    } catch (err) {
      setDashboard({ stats: emptyStats });
      setExams([]);
      setStudents([]);
      setError(err?.message || "فشل تحميل البيانات");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadAll();
  }, []);

  const filteredStudents = useMemo(() => {
    const q = studentSearch.trim().toLowerCase();
    if (!q) return students;
    return students.filter((s) =>
      (s.fullName || "").toLowerCase().includes(q) ||
      (s.studentCode || "").toLowerCase().includes(q)
    );
  }, [students, studentSearch]);

  const filteredExams = useMemo(() => {
    const q = quizSearch.trim().toLowerCase();
    if (!q) return exams;
    return exams.filter((quiz) =>
      (quiz.title || "").toLowerCase().includes(q) ||
      (quiz.examCode || "").toLowerCase().includes(q)
    );
  }, [exams, quizSearch]);

  async function handleCreateStudent(e) {
    e.preventDefault();
    resetMessages();

    try {
      await createStudent({
        fullName: studentForm.fullName,
        studentCode: studentForm.studentCode,
        grade: studentForm.grade,
        userName: studentForm.userName || studentForm.studentCode,
        password: studentForm.password || studentForm.studentCode,
      });

      setSuccess("تمت إضافة الطالب بنجاح");
      setStudentForm(studentInitial);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل إضافة الطالب");
    }
  }

  async function handleUploadStudents(e) {
    e.preventDefault();
    resetMessages();

    if (!studentsFile) {
      setError("اختر ملف الطلاب أولًا");
      return;
    }

    try {
      const result = await uploadStudents(studentsFile);
      setSuccess(result?.message || "تم رفع ملف الطلاب بنجاح");
      setStudentsFile(null);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل رفع ملف الطلاب");
    }
  }

  async function handleCreateAiExam(e) {
    e.preventDefault();
    resetMessages();

    try {
      await createAiExam({
        title: aiExamForm.title,
        topic: aiExamForm.topic,
        description: aiExamForm.description,
        examCode: aiExamForm.examCode,
        startAtUtc: aiExamForm.startAtUtc || null,
        endAtUtc: aiExamForm.endAtUtc || null,
        bankQuestionCount: Number(aiExamForm.bankQuestionCount),
        examQuestionCount: Number(aiExamForm.examQuestionCount),
      });

      setSuccess("تم إنشاء اختبار AI بنجاح");
      setAiExamForm(aiExamInitial);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل إنشاء اختبار AI");
    }
  }

  async function handleCreateManualExam(e) {
    e.preventDefault();
    resetMessages();

    try {
      await createManualExam({
        title: manualExamForm.title,
        topic: manualExamForm.topic,
        description: manualExamForm.description,
        examCode: manualExamForm.examCode,
        startAtUtc: manualExamForm.startAtUtc || null,
        endAtUtc: manualExamForm.endAtUtc || null,
        examQuestionCount: Number(manualExamForm.examQuestionCount),
      });

      setSuccess("تم إنشاء اختبار يدوي بنجاح");
      setManualExamForm(manualExamInitial);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل إنشاء الاختبار اليدوي");
    }
  }

  async function handleRegisterStudentOnExam(e) {
    e.preventDefault();
    resetMessages();

    if (!registerExamId || !registerStudentId) {
      setError("اختر الاختبار والطالب أولًا");
      return;
    }

    try {
      await registerStudentToExam(registerExamId, registerStudentId);
      setSuccess("تم تسجيل الطالب على الاختبار");
      setRegisterExamId("");
      setRegisterStudentId("");
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل تسجيل الطالب على الاختبار");
    }
  }

  async function handleUploadRegistrations(e) {
    e.preventDefault();
    resetMessages();

    if (!registrationsFile) {
      setError("اختر ملف التسجيلات أولًا");
      return;
    }

    try {
      const result = await uploadRegistrations(registrationsFile);
      setSuccess(result?.message || "تم رفع التسجيلات بنجاح");
      setRegistrationsFile(null);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل رفع ملف التسجيلات");
    }
  }

  return (
    <div className="page-wrap">
      <div className="hero-header">
        <div>
          <span className="hero-badge">Admin Control Center</span>
          <h1>لوحة الادمن الحديثة</h1>
          <p>إدارة كاملة للطلاب والاختبارات والتسجيلات داخل واجهة منظمة وعملية</p>
        </div>
        <button className="secondary slim" onClick={loadAll} disabled={loading}>
          {loading ? "جاري التحديث..." : "تحديث البيانات"}
        </button>
      </div>

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="stats-grid">
        <div className="stat-card modern-stat">
          <span>الاختبارات</span>
          <strong>{dashboard?.stats?.exams ?? 0}</strong>
        </div>
        <div className="stat-card modern-stat">
          <span>الطلاب</span>
          <strong>{dashboard?.stats?.students ?? 0}</strong>
        </div>
        <div className="stat-card modern-stat">
          <span>أولياء الأمور</span>
          <strong>{dashboard?.stats?.parents ?? 0}</strong>
        </div>
        <div className="stat-card modern-stat">
          <span>التسجيلات</span>
          <strong>{dashboard?.stats?.registrations ?? 0}</strong>
        </div>
        <div className="stat-card modern-stat">
          <span>المحاولات</span>
          <strong>{dashboard?.stats?.attempts ?? 0}</strong>
        </div>
      </div>

      <div className="admin-section-grid">
        <section className="feature-card">
          <div className="feature-card-head">
            <div>
              <span className="feature-tag">Students</span>
              <h2>إدارة الطلاب</h2>
              <p>إضافة طالب جديد أو رفع بيانات الطلاب من Excel</p>
            </div>
            <button
              className="secondary slim"
              onClick={() => setOpenSection(openSection === "students" ? "" : "students")}
            >
              {openSection === "students" ? "إخفاء" : "فتح"}
            </button>
          </div>

          {openSection === "students" && (
            <div className="feature-body">
              <div className="inner-two-col">
                <section className="inner-panel">
                  <h3>إضافة طالب</h3>
                  <form onSubmit={handleCreateStudent}>
                    <label>اسم الطالب</label>
                    <input
                      value={studentForm.fullName}
                      onChange={(e) => setStudentForm({ ...studentForm, fullName: e.target.value })}
                    />

                    <label>كود الطالب</label>
                    <input
                      value={studentForm.studentCode}
                      onChange={(e) => setStudentForm({ ...studentForm, studentCode: e.target.value })}
                    />

                    <label>الصف</label>
                    <input
                      value={studentForm.grade}
                      onChange={(e) => setStudentForm({ ...studentForm, grade: e.target.value })}
                    />

                    <label>اسم المستخدم</label>
                    <input
                      value={studentForm.userName}
                      onChange={(e) => setStudentForm({ ...studentForm, userName: e.target.value })}
                    />

                    <label>كلمة المرور</label>
                    <input
                      value={studentForm.password}
                      onChange={(e) => setStudentForm({ ...studentForm, password: e.target.value })}
                    />

                    <button type="submit">إضافة الطالب</button>
                  </form>
                </section>

                <section className="inner-panel">
                  <h3>رفع الطلاب من Excel</h3>
                  <div className="action-row">
                    <a
                      className="btn-link secondary-link"
                      href={fileUrl("/imports/students/template")}
                      target="_blank"
                      rel="noreferrer"
                    >
                      تحميل نموذج الطلاب
                    </a>
                  </div>
                  <form onSubmit={handleUploadStudents}>
                    <label>ملف Excel للطلاب</label>
                    <input type="file" accept=".xlsx,.csv" onChange={(e) => setStudentsFile(e.target.files?.[0] || null)} />
                    <button type="submit">رفع بيانات الطلاب</button>
                  </form>
                </section>
              </div>
            </div>
          )}
        </section>

        <section className="feature-card">
          <div className="feature-card-head">
            <div>
              <span className="feature-tag">Exams</span>
              <h2>إدارة الاختبارات</h2>
              <p>إنشاء اختبار AI أو اختبار يدوي</p>
            </div>
            <button
              className="secondary slim"
              onClick={() => setOpenSection(openSection === "exam" ? "" : "exam")}
            >
              {openSection === "exam" ? "إخفاء" : "فتح"}
            </button>
          </div>

          {openSection === "exam" && (
            <div className="feature-body">
              <div className="tab-switch">
                <button
                  className={examMode === "ai" ? "tab-btn active-tab" : "tab-btn"}
                  type="button"
                  onClick={() => setExamMode("ai")}
                >
                  AI Exam
                </button>
                <button
                  className={examMode === "manual" ? "tab-btn active-tab" : "tab-btn"}
                  type="button"
                  onClick={() => setExamMode("manual")}
                >
                  Manual Exam
                </button>
              </div>

              {examMode === "ai" ? (
                <section className="inner-panel single-panel">
                  <h3>إنشاء اختبار AI</h3>
                  <form onSubmit={handleCreateAiExam}>
                    <label>اسم الاختبار</label>
                    <input
                      value={aiExamForm.title}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, title: e.target.value })}
                    />

                    <label>الموضوع</label>
                    <input
                      value={aiExamForm.topic}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, topic: e.target.value })}
                    />

                    <label>الوصف</label>
                    <textarea
                      rows="3"
                      value={aiExamForm.description}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, description: e.target.value })}
                    />

                    <label>كود الاختبار</label>
                    <input
                      value={aiExamForm.examCode}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, examCode: e.target.value })}
                    />

                    <label>بداية الاختبار</label>
                    <input
                      type="datetime-local"
                      value={toDateTimeValue(aiExamForm.startAtUtc)}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, startAtUtc: e.target.value })}
                    />

                    <label>نهاية الاختبار</label>
                    <input
                      type="datetime-local"
                      value={toDateTimeValue(aiExamForm.endAtUtc)}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, endAtUtc: e.target.value })}
                    />

                    <label>عدد أسئلة البنك</label>
                    <input
                      type="number"
                      min="5"
                      value={aiExamForm.bankQuestionCount}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, bankQuestionCount: e.target.value })}
                    />

                    <label>عدد أسئلة الورقة</label>
                    <input
                      type="number"
                      min="1"
                      value={aiExamForm.examQuestionCount}
                      onChange={(e) => setAiExamForm({ ...aiExamForm, examQuestionCount: e.target.value })}
                    />

                    <button type="submit">إنشاء اختبار AI</button>
                  </form>
                </section>
              ) : (
                <section className="inner-panel single-panel">
                  <h3>إنشاء اختبار يدوي</h3>
                  <form onSubmit={handleCreateManualExam}>
                    <label>اسم الاختبار</label>
                    <input
                      value={manualExamForm.title}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, title: e.target.value })}
                    />

                    <label>الموضوع</label>
                    <input
                      value={manualExamForm.topic}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, topic: e.target.value })}
                    />

                    <label>الوصف</label>
                    <textarea
                      rows="3"
                      value={manualExamForm.description}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, description: e.target.value })}
                    />

                    <label>كود الاختبار</label>
                    <input
                      value={manualExamForm.examCode}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, examCode: e.target.value })}
                    />

                    <label>بداية الاختبار</label>
                    <input
                      type="datetime-local"
                      value={toDateTimeValue(manualExamForm.startAtUtc)}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, startAtUtc: e.target.value })}
                    />

                    <label>نهاية الاختبار</label>
                    <input
                      type="datetime-local"
                      value={toDateTimeValue(manualExamForm.endAtUtc)}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, endAtUtc: e.target.value })}
                    />

                    <label>عدد أسئلة الورقة</label>
                    <input
                      type="number"
                      min="1"
                      value={manualExamForm.examQuestionCount}
                      onChange={(e) => setManualExamForm({ ...manualExamForm, examQuestionCount: e.target.value })}
                    />

                    <button type="submit">إنشاء اختبار يدوي</button>
                  </form>
                </section>
              )}
            </div>
          )}
        </section>

        <section className="feature-card">
          <div className="feature-card-head">
            <div>
              <span className="feature-tag">Students Exam</span>
              <h2>ربط الطلاب بالاختبارات</h2>
              <p>تسجيل طالب على اختبار ورفع تسجيلات Excel</p>
            </div>
            <button
              className="secondary slim"
              onClick={() => setOpenSection(openSection === "studentsExam" ? "" : "studentsExam")}
            >
              {openSection === "studentsExam" ? "إخفاء" : "فتح"}
            </button>
          </div>

          {openSection === "studentsExam" && (
            <div className="feature-body">
              <div className="inner-two-col">
                <section className="inner-panel">
                  <h3>تسجيل طالب على اختبار</h3>
                  <form onSubmit={handleRegisterStudentOnExam}>
                    <label>ابحث عن الاختبار</label>
                    <input
                      value={quizSearch}
                      onChange={(e) => setQuizSearch(e.target.value)}
                      placeholder="ابحث باسم الاختبار أو الكود"
                    />

                    <label>اختر الاختبار</label>
                    <select value={registerExamId} onChange={(e) => setRegisterExamId(e.target.value)}>
                      <option value="">اختر اختبارًا</option>
                      {filteredExams.map((quiz) => (
                        <option key={quiz.id} value={quiz.id}>
                          {quiz.title} - {quiz.examCode}
                        </option>
                      ))}
                    </select>

                    <label>ابحث عن الطالب</label>
                    <input
                      value={studentSearch}
                      onChange={(e) => setStudentSearch(e.target.value)}
                      placeholder="ابحث باسم الطالب أو الكود"
                    />

                    <label>اختر الطالب</label>
                    <select value={registerStudentId} onChange={(e) => setRegisterStudentId(e.target.value)}>
                      <option value="">اختر طالبًا</option>
                      {filteredStudents.map((student) => (
                        <option key={student.id} value={student.id}>
                          {student.fullName} - {student.studentCode}
                        </option>
                      ))}
                    </select>

                    <button type="submit">تسجيل الطالب</button>
                  </form>
                </section>

                <section className="inner-panel">
                  <h3>رفع طلاب إلى اختبار من Excel</h3>
                  <div className="action-row">
                    <a
                      className="btn-link secondary-link"
                      href={fileUrl("/imports/registrations/template")}
                      target="_blank"
                      rel="noreferrer"
                    >
                      تحميل نموذج الرفع
                    </a>
                  </div>
                  <form onSubmit={handleUploadRegistrations}>
                    <label>ملف التسجيلات</label>
                    <input type="file" accept=".xlsx,.csv" onChange={(e) => setRegistrationsFile(e.target.files?.[0] || null)} />
                    <button type="submit">رفع تسجيلات الاختبار</button>
                  </form>
                </section>
              </div>

              <section className="inner-panel top-space">
                <div className="panel-title-row">
                  <h3>الاختبارات الحالية</h3>
                  <span className="muted-note">اختر الاختبار لإدارته تفصيليًا</span>
                </div>

                {exams.length === 0 && <div className="empty-box">لا يوجد اختبارات.</div>}

                <div className="quiz-grid">
                  {exams.map((quiz) => (
                    <div className="dashboard-mini-card" key={quiz.id}>
                      <div className="list-head">
                        <strong>{quiz.title}</strong>
                        <span className="pill">{quiz.examCode}</span>
                      </div>
                      <div>بنك الأسئلة: {quiz.bankQuestionCount}</div>
                      <div>أسئلة الورقة: {quiz.examQuestionCount}</div>
                      <div>الطلاب المسجلين: {quiz.registeredStudents}</div>
                      <div>المحاولات: {quiz.attemptCount}</div>
                      <div className="action-row top-gap">
                        <Link className="btn-link" to={`/admin/exams/${quiz.id}`}>فتح الداشبورد</Link>
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

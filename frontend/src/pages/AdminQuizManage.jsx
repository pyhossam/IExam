import React, { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  addExamQuestion,
  deleteExamQuestion,
  downloadFileWithAuth,
  downloadPdfWithAuth,
  getAttemptDetails,
  getExamAnalytics,
  getExamAttempts,
  getExamById,
  getStudentLookups,
  openFileWithAuth,
  openPdfWithAuth,
  resetAttempt,
  updateExamQuestion,
  updateExamSettings,
  uploadExamQuestions,
} from "../services/api";

const emptyQuestionForm = {
  question_text: "",
  choice_a: "",
  choice_b: "",
  choice_c: "",
  choice_d: "",
  correct_answer: "A",
  explanation: "",
};

export default function AdminExamManage() {
  const { quizId } = useParams();

  const [quiz, setExam] = useState(null);
  const [quizStats, setExamStats] = useState(null);
  const [students, setStudents] = useState([]);
  const [attempts, setAttempts] = useState([]);
  const [questionForm, setQuestionForm] = useState(emptyQuestionForm);
  const [editingQuestionId, setEditingQuestionId] = useState(null);
  const [resetStudentId, setResetStudentId] = useState("");
  const [newExamCount, setNewExamCount] = useState(10);
  const [formsCount, setFormsCount] = useState(3);
  const [questionsFile, setQuestionsFile] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function loadPage() {
    try {
      setError("");
      setSuccess("");
      const [q, s, a, stats] = await Promise.all([
        getExamById(quizId),
        getStudentLookups(),
        getExamAttempts(quizId),
        getExamAnalytics(quizId),
      ]);
      setExam(q);
      setStudents(s);
      setAttempts(a);
      setExamStats(stats);
      setNewExamCount(q.exam_question_count);
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  useEffect(() => {
    loadPage();
  }, [quizId]);

  function onQuestionFormChange(field, value) {
    setQuestionForm((prev) => ({ ...prev, [field]: value }));
  }

  async function saveQuestion(e) {
    e.preventDefault();
    try {
      setError("");
      setSuccess("");

      if (editingQuestionId) {
        await updateExamQuestion(editingQuestionId, {
        questionText: questionForm.question_text,
        choiceA: questionForm.choice_a,
        choiceB: questionForm.choice_b,
        choiceC: questionForm.choice_c,
        choiceD: questionForm.choice_d,
        correctAnswer: questionForm.correct_answer,
        explanation: questionForm.explanation,
      });
        setSuccess("تم تعديل السؤال بنجاح");
      } else {
        await addExamQuestion(quizId, {
        questionText: questionForm.question_text,
        choiceA: questionForm.choice_a,
        choiceB: questionForm.choice_b,
        choiceC: questionForm.choice_c,
        choiceD: questionForm.choice_d,
        correctAnswer: questionForm.correct_answer,
        explanation: questionForm.explanation,
      });
        setSuccess("تمت إضافة السؤال بنجاح");
      }

      setQuestionForm(emptyQuestionForm);
      setEditingQuestionId(null);
      await loadPage();
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  function startEditQuestion(question) {
    setEditingQuestionId(question.id);
    setQuestionForm({
      question_text: question.question_text || "",
      choice_a: question.choice_a || "",
      choice_b: question.choice_b || "",
      choice_c: question.choice_c || "",
      choice_d: question.choice_d || "",
      correct_answer: question.correct_answer || "A",
      explanation: question.explanation || "",
    });
  }

  function clearQuestionEditor() {
    setEditingQuestionId(null);
    setQuestionForm(emptyQuestionForm);
  }

  async function deleteQuestion(questionId) {
    if (!window.confirm("هل تريد حذف السؤال؟")) return;
    try {
      setError("");
      setSuccess("");
      await deleteExamQuestion(questionId);
      setSuccess("تم حذف السؤال");
      await loadPage();
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  async function updateExamCount(e) {
    e.preventDefault();
    try {
      setError("");
      setSuccess("");
      await updateExamSettings(quizId, {
        examQuestionCount: Number(newExamCount),
      });
      setSuccess("تم تحديث عدد أسئلة ورقة الاختبار");
      await loadPage();
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  async function resetStudentExam(e) {
    e.preventDefault();
    try {
      setError("");
      setSuccess("");
      const attempt = attempts.find((x) => String(x.studentProfileId || x.studentId || x.student_id) === String(resetStudentId));
      if (!attempt?.id) throw new Error("لم يتم العثور على محاولة لهذا الطالب");
      await resetAttempt(attempt.id);
      setSuccess("تمت إعادة تعيين محاولة الطالب");
      setResetStudentId("");
      await loadPage();
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  async function uploadQuestionsFile(e) {
    e.preventDefault();
    setError("");
    setSuccess("");

    if (!questionsFile) {
      setError("اختر ملف الأسئلة أولًا");
      return;
    }

    try {
      const data = await uploadExamQuestions(quizId, questionsFile);
      setSuccess(data?.message || "تم رفع الأسئلة بنجاح");
      setQuestionsFile(null);
      await loadPage();
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  if (!quiz || !quizStats) {
    return <div className="standalone-page"><div className="section-card">جاري التحميل...</div></div>;
  }

  const bands = quizStats.success_bands || {};

  return (
    <div className="standalone-page">
      <div className="standalone-header">
        <div>
          <span className="topbar-badge">Exam Management</span>
          <h1>{quiz.title}</h1>
          <p>إدارة كاملة لأسئلة الاختبار والطباعة والطلاب والمحاولات.</p>
        </div>
        <div className="action-row">
          <Link to="/admin/exams" className="ghost-btn">العودة للاختبارات</Link>
          <Link to="/admin" className="ghost-btn">الرئيسية</Link>
        </div>
      </div>

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="stats-grid">
        <div className="stat-card"><span>عدد الأسئلة</span><strong>{quizStats.question_count}</strong></div>
        <div className="stat-card"><span>الطلاب المضافون</span><strong>{quizStats.registered_students}</strong></div>
        <div className="stat-card"><span>من أدوا الاختبار</span><strong>{quizStats.students_attempted}</strong></div>
        <div className="stat-card"><span>كود الاختبار</span><strong>{quizStats.exam_code}</strong></div>
        <div className="stat-card"><span>أسئلة الورقة</span><strong>{(quiz.examQuestionCount ?? quiz.exam_question_count)}</strong></div>
      </div>

      <div className="stats-grid">
        <div className="stat-card"><span>أقل من 50%</span><strong>{bands.lt_50 || 0}</strong></div>
        <div className="stat-card"><span>50% - 75%</span><strong>{bands.from_50_to_75 || 0}</strong></div>
        <div className="stat-card"><span>75% - 85%</span><strong>{bands.from_75_to_85 || 0}</strong></div>
        <div className="stat-card"><span>أكثر من 85%</span><strong>{bands.gt_85 || 0}</strong></div>
      </div>

      <div className="page-grid two">
        <section className="section-card">
          <div className="section-head">
            <div>
              <h3>إعدادات ورقة الاختبار</h3>
              <p>تحديد عدد الأسئلة المطلوبة في ورقة الطالب.</p>
            </div>
          </div>

          <form onSubmit={updateExamCount}>
            <label>عدد أسئلة الورقة</label>
            <input
              type="number"
              min="1"
              max={(quiz.questions?.length || 0) || 1}
              value={newExamCount}
              onChange={(e) => setNewExamCount(e.target.value)}
            />
            <button type="submit" className="primary-btn full-btn">حفظ الإعداد</button>
          </form>

          <div className="section-head top-space">
            <div>
              <h3>رفع أسئلة من ملف</h3>
              <p>استخدم نموذج الأسئلة التوضيحي ثم ارفع ملف Excel أو CSV.</p>
            </div>
          </div>

          <div className="template-box">
            <a
              className="ghost-btn"
              href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quizId}/questions/template`}
              target="_blank"
              rel="noreferrer"
            >
              تحميل نموذج الأسئلة
            </a>
          </div>

          <form onSubmit={uploadQuestionsFile}>
            <label>ملف الأسئلة</label>
            <input type="file" accept=".xlsx,.csv" onChange={(e) => setQuestionsFile(e.target.files?.[0] || null)} />
            <button type="submit" className="primary-btn full-btn">رفع الأسئلة</button>
          </form>

          <div className="section-head top-space">
            <div>
              <h3>طباعة ورقة الاختبار</h3>
              <p>طباعة كافة الأسئلة مع أو بدون الإجابات.</p>
            </div>
          </div>

          <div className="action-row">
            <a className="primary-btn" href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quiz.id}/pdf/questions`} target="_blank" rel="noreferrer">
              طباعة الأسئلة بدون إجابات
            </a>
            <a className="ghost-btn" href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quiz.id}/pdf/questions?withAnswers=true`} target="_blank" rel="noreferrer">
              طباعة الأسئلة مع الإجابات
            </a>
            <a className="ghost-btn" href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quiz.id}/pdf/questions?withAnswers=true`} target="_blank" rel="noreferrer">
              طباعة نموذج الإجابة الكامل
            </a>
          </div>

          <div className="section-head top-space">
            <div>
              <h3>طباعة النماذج العشوائية</h3>
              <p>إنشاء نماذج A / B / C / D بأسئلة وإجابات عشوائية.</p>
            </div>
          </div>

          <label>عدد النماذج المطلوبة</label>
          <input
            type="number"
            min="1"
            max="26"
            value={formsCount}
            onChange={(e) => setFormsCount(e.target.value)}
          />

          <div className="action-row top-space">
            <a
              className="primary-btn"
              href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quiz.id}/pdf/random-forms?formsCount=${formsCount}`}
              target="_blank"
              rel="noreferrer"
            >
              طباعة نماذج الطلاب
            </a>

            <a
              className="ghost-btn"
              href={`${import.meta.env.VITE_API_BASE || "/api"}/exams/${quiz.id}/pdf/random-forms-answer-keys?formsCount=${formsCount}`}
              target="_blank"
              rel="noreferrer"
            >
              طباعة نماذج الإجابة
            </a>
          </div>
        </section>

        <section className="section-card">
          <div className="section-head">
            <div>
              <h3>{editingQuestionId ? "تعديل السؤال" : "إضافة سؤال يدويًا"}</h3>
              <p>مراجعة السؤال وتحديد الإجابة الصحيحة ثم الحفظ.</p>
            </div>
          </div>

          <form onSubmit={saveQuestion}>
            <label>نص السؤال</label>
            <textarea rows="3" value={questionForm.question_text} onChange={(e) => onQuestionFormChange("question_text", e.target.value)} />

            <label>الخيار A</label>
            <input value={questionForm.choice_a} onChange={(e) => onQuestionFormChange("choice_a", e.target.value)} />

            <label>الخيار B</label>
            <input value={questionForm.choice_b} onChange={(e) => onQuestionFormChange("choice_b", e.target.value)} />

            <label>الخيار C</label>
            <input value={questionForm.choice_c} onChange={(e) => onQuestionFormChange("choice_c", e.target.value)} />

            <label>الخيار D</label>
            <input value={questionForm.choice_d} onChange={(e) => onQuestionFormChange("choice_d", e.target.value)} />

            <label>الإجابة الصحيحة</label>
            <select value={questionForm.correct_answer} onChange={(e) => onQuestionFormChange("correct_answer", e.target.value)}>
              <option value="A">A</option>
              <option value="B">B</option>
              <option value="C">C</option>
              <option value="D">D</option>
            </select>

            <label>التفسير</label>
            <textarea rows="2" value={questionForm.explanation} onChange={(e) => onQuestionFormChange("explanation", e.target.value)} />

            <div className="action-row top-space">
              <button type="submit" className="primary-btn">
                {editingQuestionId ? "حفظ التعديل" : "إضافة السؤال"}
              </button>
              <button type="button" className="ghost-btn" onClick={clearQuestionEditor}>
                إلغاء
              </button>
            </div>
          </form>

          <div className="section-head top-space">
            <div>
              <h3>إعادة تعيين محاولة طالب</h3>
              <p>حذف محاولة الطالب لإتاحة إعادة الاختبار.</p>
            </div>
          </div>

          <form onSubmit={resetStudentExam}>
            <label>رقم الطالب</label>
            <input value={resetStudentId} onChange={(e) => setResetStudentId(e.target.value)} />
            <button type="submit" className="primary-btn full-btn">إعادة التعيين</button>
          </form>
        </section>
      </div>

      <div className="page-grid two">
        <section className="section-card">
          <div className="section-head">
            <div>
              <h3>الطلاب المسجلون</h3>
              <p>الطلاب المربوطون بهذا الاختبار.</p>
            </div>
          </div>

          <div className="table-wrap">
            <table className="app-table">
              <thead>
                <tr>
                  <th>الاسم</th>
                  <th>رقم الطالب</th>
                  <th>كود الطالب</th>
                </tr>
              </thead>
              <tbody>
                {students.length === 0 ? (
                  <tr><td colSpan="3" className="empty-cell">لا يوجد طلاب مسجلون</td></tr>
                ) : (
                  students.map((student) => (
                    <tr key={student.registration_id}>
                      <td>{student.student_name}</td>
                      <td>{student.student_id}</td>
                      <td>{student.student_code}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          <div className="section-head top-space">
            <div>
              <h3>محاولات الطلاب</h3>
              <p>نتائج المحاولات المنجزة لهذا الاختبار.</p>
            </div>
          </div>

          <div className="table-wrap">
            <table className="app-table">
              <thead>
                <tr>
                  <th>اسم الطالب</th>
                  <th>الدرجة</th>
                  <th>النسبة</th>
                </tr>
              </thead>
              <tbody>
                {attempts.length === 0 ? (
                  <tr><td colSpan="3" className="empty-cell">لا توجد محاولات بعد</td></tr>
                ) : (
                  attempts.map((attempt) => (
                    <tr key={attempt.attempt_id}>
                      <td>{attempt.student_name}</td>
                      <td>{attempt.score} / {attempt.total_questions}</td>
                      <td>{attempt.percentage}%</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>

        <section className="section-card">
          <div className="section-head">
            <div>
              <h3>إدارة أسئلة الاختبار</h3>
              <p>راجع الأسئلة وحدد إن كانت الإجابة صحيحة أو قم بالتعديل أو الحذف.</p>
            </div>
          </div>

          {(quiz.questions?.length || 0) === 0 ? (
            <div className="empty-box">لا توجد أسئلة في هذا الاختبار</div>
          ) : (
            quiz.questions.map((question, index) => (
              <div className="question-card" key={question.id}>
                <h4>{index + 1}. {question.question_text}</h4>
                <div>A) {question.choice_a}</div>
                <div>B) {question.choice_b}</div>
                <div>C) {question.choice_c}</div>
                <div>D) {question.choice_d}</div>
                <div><strong>الإجابة الصحيحة:</strong> {question.correct_answer}</div>
                <div><strong>التفسير:</strong> {question.explanation || "-"}</div>

                <div className="action-row top-space">
                  <button className="primary-btn" onClick={() => startEditQuestion(question)}>تعديل</button>
                  <button className="ghost-btn" onClick={() => deleteQuestion(question.id)}>حذف</button>
                </div>
              </div>
            ))
          )}
        </section>
      </div>
    </div>
  );
}

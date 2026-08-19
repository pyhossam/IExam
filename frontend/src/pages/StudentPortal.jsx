import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  getStudentDashboard,
  startStudentExam,
  submitStudentExam,
} from "../services/api";

export default function StudentPortal() {
  const [dashboard, setDashboard] = useState(null);
  const [activeExam, setActiveExam] = useState(null);
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function loadDashboard() {
    try {
      setError("");
      const data = await getStudentDashboard();
      setDashboard(data);
    } catch (err) {
      setError(getReadableErrorMessage(err));
    }
  }

  useEffect(() => {
    loadDashboard();
  }, []);

  async function handleStartExam(examId) {
    try {
      setError("");
      setSuccess("");
      const data = await startStudentExam(examId);
      setActiveExam(data);
      setAnswers({});
      setResult(null);
      setSuccess("تم فتح الاختبار بنجاح");
    } catch (err) {
      setError(getReadableErrorMessage(err));
    }
  }

  function choose(questionId, selectedAnswer) {
    setAnswers((prev) => ({ ...prev, [questionId]: selectedAnswer }));
  }

  async function handleSubmitExam() {
    if (!activeExam) return;

    try {
      setError("");
      setSuccess("");

      const payload = {
        examId: activeExam.examId,
        answers: Object.entries(answers).map(([questionId, selectedAnswer]) => ({
          questionId,
          selectedAnswer,
        })),
      };

      const res = await submitStudentExam(payload);
      setResult(res);
      setSuccess("تم تسليم الاختبار بنجاح");
      await loadDashboard();
    } catch (err) {
      setError(getReadableErrorMessage(err));
    }
  }

  return (
    <div className="standalone-page">
      <div className="standalone-header">
        <div>
          <span className="topbar-badge">Student Portal</span>
          <h1>بوابة الطالب</h1>
          <p>عرض الاختبارات المتاحة وبدء الاختبار وتسليم الإجابات.</p>
        </div>
        <Link to="/admin" className="ghost-btn">العودة إلى الرئيسية</Link>
      </div>

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      {!dashboard ? (
        <div className="section-card">جاري التحميل...</div>
      ) : (
        <>
          <section className="section-card">
            <div className="section-head">
              <div>
                <h3>{dashboard.studentName}</h3>
                <p>الكود: {dashboard.studentCode} | الصف: {dashboard.grade}</p>
              </div>
            </div>

            <div className="cards-grid">
              {(dashboard.availableExams || []).map((exam) => (
                <div className="mini-card" key={exam.examId}>
                  <div className="mini-card-head">
                    <h4>{exam.title}</h4>
                    <span className="mini-pill">{exam.examCode}</span>
                  </div>
                  <div>الحالة: {exam.availabilityStatus}</div>
                  <div>البداية: {new Date(exam.startAtUtc).toLocaleString()}</div>
                  <div>النهاية: {new Date(exam.endAtUtc).toLocaleString()}</div>
                  <div className="top-space">
                    <button
                      className="primary-btn"
                      disabled={!exam.canStart}
                      onClick={() => handleStartExam(exam.examId)}
                    >
                      بدء الاختبار
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </section>

          {activeExam && (
            <section className="section-card top-space">
              <div className="section-head">
                <div>
                  <h3>{activeExam.title}</h3>
                  <p>كود الاختبار: {activeExam.examCode}</p>
                </div>
              </div>

              {(activeExam.questions || []).map((q, idx) => (
                <div className="question-card" key={q.id}>
                  <h4>{idx + 1}. {q.questionText}</h4>
                  {["A", "B", "C", "D"].map((letter) => (
                    <button
                      key={letter}
                      type="button"
                      className={answers[q.id] === letter ? "choice-btn selected" : "choice-btn"}
                      onClick={() => choose(q.id, letter)}
                    >
                      {letter}) {q[`choice${letter}`]}
                    </button>
                  ))}
                </div>
              ))}

              <button className="primary-btn full-btn top-space" onClick={handleSubmitExam}>
                تسليم الاختبار
              </button>
            </section>
          )}

          {result && (
            <section className="section-card top-space">
              <div className="section-head">
                <div>
                  <h3>النتيجة</h3>
                </div>
              </div>
              <div className="result-box">
                <div>الدرجة: {result.score} / {result.totalQuestions}</div>
                <div>النسبة: {result.percentage}%</div>
              </div>
            </section>
          )}
        </>
      )}
    </div>
  );
}

import { useState } from "react";
import { startStudentExam, submitStudentExam, getReadableErrorMessage } from "../services/api";
import SectionCard from "../components/ui/SectionCard";

export default function StudentPage() {
  const [examCode, setExamCode] = useState("");
  const [studentCode, setStudentCode] = useState("");
  const [exam, setExam] = useState(null);
  const [answers, setAnswers] = useState({});
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function accessExam(e) {
    e.preventDefault();
    try {
      setError("");
      setSuccess("");
      const data = await startStudentExam({
        exam_code: examCode,
        student_code: studentCode,
      });
      setExam(data);
      setResult(null);
      setAnswers({});
      setSuccess("Exam loaded");
    } catch (err) {
      setError(getReadableErrorMessage(err))
    }
  }

  async function submitExam() {
    if (!exam) return;
    try {
      setError("");
      setSuccess("");
      const payload = {
        exam_code: exam.exam_code,
        student_code: studentCode,
        answers: Object.entries(answers).map(([question_id, selected_answer]) => ({
          question_id: Number(question_id),
          selected_answer,
        })),
      };
      const data = await submitStudentExam(payload);
      setResult(data);
      setSuccess("Exam submitted");
    } catch (err) {
      setError(getReadableErrorMessage(err));
    }
  }

  async function loadResult() {
    try {
      setError("");
      setSuccess("");
      const data = await apiJson("/student/result", "POST", {
        exam_code: examCode,
        student_code: studentCode,
      });
      setResult(data);
      setSuccess("Result loaded");
    } catch (err) {
      setError(getReadableErrorMessage(err));
    }
  }

  function choose(questionId, letter) {
    setAnswers((prev) => ({ ...prev, [questionId]: letter }));
  }

  return (
    <div className="portal-page">
      <div className="portal-shell">
        <div className="page-title">
          <h1>Student Page</h1>
          <p>الدخول للاختبار، الحل، وإظهار النتيجة</p>
        </div>

        {error && <div className="alert error">{error}</div>}
        {success && <div className="alert success">{success}</div>}

        <div className="two-col">
          <SectionCard title="Login To Exam" subtitle="اكتب كود الاختبار وكود الطالب">
            <form className="form-grid" onSubmit={accessExam}>
              <label>Exam Code</label>
              <input value={examCode} onChange={(e) => setExamCode(e.target.value)} />

              <label>Student Code</label>
              <input value={studentCode} onChange={(e) => setStudentCode(e.target.value)} />

              <div className="action-row top-space">
                <button className="btn btn-primary" type="submit">Open Exam</button>
                <button className="btn btn-secondary" type="button" onClick={loadResult}>View Result</button>
              </div>
            </form>
          </SectionCard>

          <SectionCard title="Exam / Result" subtitle="منطقة الطالب">
            {!exam && !result && <div className="empty-box">قم بفتح الاختبار أو تحميل النتيجة</div>}

            {exam && (
              <div className="page-stack">
                <div className="info-card">
                  <div className="info-card-head">
                    <strong>{exam.title}</strong>
                    <span className="badge">{exam.exam_code}</span>
                  </div>
                  <p>Student: {exam.student_name}</p>
                </div>

                {(exam.questions || []).map((q, index) => (
                  <div className="question-bank-card" key={q.id}>
                    <h4>{index + 1}. {q.question_text}</h4>

                    {["A", "B", "C", "D"].map((letter) => (
                      <button
                        key={letter}
                        type="button"
                        className={answers[q.id] === letter ? "choice-btn selected" : "choice-btn"}
                        onClick={() => choose(q.id, letter)}
                      >
                        {letter}) {q[`choice_${letter.toLowerCase()}`]}
                      </button>
                    ))}
                  </div>
                ))}

                <button className="btn btn-primary" type="button" onClick={submitExam}>Submit Exam</button>
              </div>
            )}

            {result && (
              <div className="page-stack top-space">
                <div className="hero-panel light">
                  <div>
                    <span className="hero-chip dark">Result</span>
                    <h2>{result.student_name}</h2>
                    <p>Score: {result.score} / {result.total_questions} — {result.percentage}%</p>
                  </div>
                </div>

                {(result.results || []).map((item, index) => (
                  <div className="question-bank-card" key={index}>
                    <p><strong>Question ID:</strong> {item.question_id}</p>
                    <p><strong>Selected:</strong> {item.selected_answer || "-"}</p>
                    <p><strong>Correct:</strong> {item.correct_answer}</p>
                    <p><strong>Status:</strong> {item.is_correct ? "Correct" : "Wrong"}</p>
                    <p><strong>Explanation:</strong> {item.explanation || "-"}</p>
                  </div>
                ))}
              </div>
            )}
          </SectionCard>
        </div>
      </div>
    </div>
  );
}

import { useState } from "react";
import { getParentChildrenResults } from "../services/api";
import SectionCard from "../components/ui/SectionCard";

export default function ParentPage() {
  const [parentCode, setParentCode] = useState("");
  const [studentCode, setStudentCode] = useState("");
  const [examCode, setExamCode] = useState("");
  const [report, setReport] = useState(null);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function loadReport(e) {
    e.preventDefault();
    try {
      setError("");
      setSuccess("");
      const data = await getParentChildrenResults({
        parent_code: parentCode,
        student_code: studentCode,
        exam_code: examCode,
      });
      setReport(data);
      setSuccess("Report loaded");
    } catch (err) {
      setError(getReadableErrorMessage(err))   
    }
  }

  return (
    <div className="portal-page">
      <div className="portal-shell">
        <div className="page-title">
          <h1>Parent Page</h1>
          <p>متابعة نتائج الأبناء من خلال كود ولي الأمر</p>
        </div>

        {error && <div className="alert error">{error}</div>}
        {success && <div className="alert success">{success}</div>}

        <div className="two-col">
          <SectionCard title="Parent Lookup" subtitle="أدخل البيانات المطلوبة">
            <form className="form-grid" onSubmit={loadReport}>
              <label>Parent Code</label>
              <input value={parentCode} onChange={(e) => setParentCode(e.target.value)} />

              <label>Student Code</label>
              <input value={studentCode} onChange={(e) => setStudentCode(e.target.value)} />

              <label>Exam Code</label>
              <input value={examCode} onChange={(e) => setExamCode(e.target.value)} />

              <button className="btn btn-primary top-space" type="submit">Load Report</button>
            </form>
          </SectionCard>

          <SectionCard title="Student Report" subtitle="نتيجة الطالب التفصيلية">
            {!report ? (
              <div className="empty-box">لا يوجد تقرير حتى الآن</div>
            ) : (
              <div className="page-stack">
                <div className="hero-panel light">
                  <div>
                    <span className="hero-chip dark">Parent Report</span>
                    <h2>{report.student_name}</h2>
                    <p>Score: {report.score} / {report.total_questions} — {report.percentage}%</p>
                    <p>Exam Code: {report.exam_code}</p>
                  </div>
                </div>

                {(report.results || []).map((item, index) => (
                  <div className="question-bank-card" key={index}>
                    <p><strong>Question ID:</strong> {item.question_id}</p>
                    <p><strong>Student Answer:</strong> {item.selected_answer || "-"}</p>
                    <p><strong>Correct Answer:</strong> {item.correct_answer}</p>
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

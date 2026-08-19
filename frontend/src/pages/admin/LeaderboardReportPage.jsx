import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { getExamById, getLeaderboard, getReadableErrorMessage } from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";

export default function LeaderboardReportPage() {
  const { examId } = useParams();
  const [params] = useSearchParams();
  const limit = Math.max(1, Number(params.get("limit")) || 10);
  const [exam, setExam] = useState(null);
  const [rows, setRows] = useState([]);
  const [error, setError] = useState("");
  const printedBy = localStorage.getItem("userName") || "مستخدم النظام";
  const generatedAt = useMemo(() => new Date(), []);

  useEffect(() => {
    Promise.all([getExamById(examId), getLeaderboard(examId)])
      .then(([examData, leaderboard]) => {
        setExam(examData);
        setRows((leaderboard || []).slice(0, limit));
      })
      .catch((err) => setError(getReadableErrorMessage(err, "تعذر إعداد تقرير الترتيب")));
  }, [examId, limit]);

  function printReport() {
    document.title = `leaderboard-${exam?.examCode || examId}`;
    window.print();
  }

  if (error) return <div className="standalone-page"><div className="alert error">{error}</div></div>;
  if (!exam) return <div className="standalone-page"><div className="section-card">جاري إعداد التقرير...</div></div>;

  return (
    <div className="leaderboard-report-page" dir="rtl">
      <div className="report-toolbar">
        <Link className="ghost-btn" to={`/admin/exams/${examId}`}>العودة إلى الاختبار</Link>
        <button className="primary-btn" type="button" onClick={printReport}>طباعة التقرير</button>
      </div>
      <main className="leaderboard-report">
        <header>
          <div>
            <span>{exam.institutionName || "المؤسسة التعليمية"}</span>
            <h1>تقرير ترتيب الطلاب</h1>
            <p>أول {rows.length} طالب حسب نتائج الاختبار</p>
          </div>
          <div className="report-badge">{exam.examCode}</div>
        </header>

        <section className="report-exam-meta">
          <div><span>اسم الاختبار</span><strong>{exam.title}</strong></div>
          <div><span>المقرر</span><strong>{exam.subjectName || "غير محدد"}</strong><small>{exam.subjectCode || ""}</small></div>
          <div><span>موضوع الاختبار</span><strong>{exam.topic || "—"}</strong></div>
          <div><span>فترة الاختبار</span><strong>{formatSaudiDateTime(exam.startAtUtc)}</strong><small>إلى {formatSaudiDateTime(exam.endAtUtc)}</small></div>
        </section>

        <table className="report-table">
          <thead><tr><th>الترتيب</th><th>اسم الطالب</th><th>كود الطالب</th><th>الدرجة</th><th>النسبة</th></tr></thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.studentId}>
                <td><b className="rank-number">{row.rank}</b></td>
                <td>{row.studentName}</td><td>{row.studentCode}</td>
                <td>{row.score} / {row.totalQuestions}</td><td>{row.percentage}%</td>
              </tr>
            ))}
            {rows.length === 0 && <tr><td colSpan="5">لا توجد نتائج مكتملة لهذا الاختبار.</td></tr>}
          </tbody>
        </table>

        <footer>أُعد التقرير بواسطة: {printedBy} — {formatSaudiDateTime(generatedAt)}</footer>
      </main>
    </div>
  );
}

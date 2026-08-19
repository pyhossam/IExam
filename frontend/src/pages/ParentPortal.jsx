import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getParentDashboard } from "../services/api";

export default function ParentPortal() {
  const [dashboard, setDashboard] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    getParentDashboard()
      .then(setDashboard)
      .catch((err) => setError(getReadableErrorMessage(err)));
  }, []);

  return (
    <div className="standalone-page">
      <div className="standalone-header">
        <div>
          <span className="topbar-badge">Parent Portal</span>
          <h1>بوابة ولي الأمر</h1>
          <p>عرض الأبناء ونتائجهم من خلال الـ API الجديد.</p>
        </div>
        <Link to="/admin" className="ghost-btn">العودة إلى الرئيسية</Link>
      </div>

      {error && <div className="alert error">{error}</div>}

      {!dashboard ? (
        <div className="section-card">جاري التحميل...</div>
      ) : (
        <>
          <section className="section-card">
            <div className="section-head">
              <div>
                <h3>{dashboard.parentName}</h3>
                <p>كود ولي الأمر: {dashboard.parentCode}</p>
              </div>
            </div>

            <div className="cards-grid">
              {(dashboard.children || []).map((child) => (
                <div className="mini-card" key={child.studentId}>
                  <div className="mini-card-head">
                    <h4>{child.studentName}</h4>
                    <span className="mini-pill">{child.studentCode}</span>
                  </div>
                  <div>الصف: {child.grade}</div>
                </div>
              ))}
            </div>
          </section>

          <section className="section-card top-space">
            <div className="section-head">
              <div>
                <h3>نتائج الأبناء</h3>
              </div>
            </div>

            <div className="table-wrap">
              <table className="app-table">
                <thead>
                  <tr>
                    <th>الطالب</th>
                    <th>كود الطالب</th>
                    <th>الاختبار</th>
                    <th>كود الاختبار</th>
                    <th>الدرجة</th>
                    <th>النسبة</th>
                    <th>وقت التسليم</th>
                  </tr>
                </thead>
                <tbody>
                  {(dashboard.childrenResults || []).length === 0 ? (
                    <tr>
                      <td colSpan="7" className="empty-cell">لا توجد نتائج</td>
                    </tr>
                  ) : (
                    dashboard.childrenResults.map((row, idx) => (
                      <tr key={idx}>
                        <td>{row.studentName}</td>
                        <td>{row.studentCode}</td>
                        <td>{row.examTitle}</td>
                        <td>{row.examCode}</td>
                        <td>{row.score} / {row.totalQuestions}</td>
                        <td>{row.percentage}%</td>
                        <td>{row.submittedAtUtc ? new Date(row.submittedAtUtc).toLocaleString() : "-"}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
}

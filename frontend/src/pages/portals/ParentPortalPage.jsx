import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { getParentChildrenResults, getParentDashboard } from "../../services/api";
import { toArabicDigits, toArabicNumber, toArabicPercent } from "../../utils/arabicNumbers";

function normalizeDashboard(data) {
  const source = data || {};
  return {
    parentName: source.parentName || source.fullName || "ولي الأمر",
    parentCode: source.parentCode || source.code || "-",
    children: Array.isArray(source.children) ? source.children : [],
  };
}

function normalizeResults(rows) {
  return (Array.isArray(rows) ? rows : []).map((row, index) => {
    const submitted =
      !!row.submittedAtUtc ||
      !!row.submittedAt ||
      row.status === "Submitted" ||
      row.completed === true;

    const score =
      row.score ??
      row.obtainedScore ??
      null;

    const totalQuestions =
      row.totalQuestions ??
      row.examQuestionCount ??
      row.total ??
      null;

    const percentage =
      row.percentage ??
      (score != null && totalQuestions ? Math.round((score / totalQuestions) * 100) : null)

    return {
      id: row.id || `${row.studentId || row.studentCode || "student"}-${row.examId || row.examCode || index}`,
      studentId: row.studentId || "",
      studentName: row.studentName || row.fullName || "طالب",
      studentCode: row.studentCode || "-",
      grade: row.grade || "-",
      examTitle: row.examTitle || row.title || "اختبار",
      examCode: row.examCode || "-",
      isSubmitted: submitted,
      score: score,
      totalQuestions: totalQuestions,
      percentage: row.percentage ?? null,
      submittedAtUtc: row.submittedAtUtc || row.submittedAt || null,
      statusText: submitted ? "أدى الاختبار" : "لم يؤد الاختبار بعد",
    };
  });
}

export default function ParentPortalPage() {
  const [dashboard, setDashboard] = useState(null);
  const [results, setResults] = useState([]);
  const [error, setError] = useState("");
  const [selectedStudentId, setSelectedStudentId] = useState("");

  async function load() {
    try {
      setError("");
      const [dashboardData, resultsData] = await Promise.all([
        getParentDashboard(),
        getParentChildrenResults(),
      ]);

      const normalizedDashboard = normalizeDashboard(dashboardData);
      setDashboard(normalizedDashboard);
      setResults(normalizeResults(resultsData));

      if (!selectedStudentId && normalizedDashboard.children?.length > 0) {
        setSelectedStudentId(
          normalizedDashboard.children[0].studentId ||
          normalizedDashboard.children[0].id ||
          ""
        );
      }
    } catch (err) {
      setError(err.message || "فشل تحميل بوابة ولي الأمر");
    }
  }

  useEffect(() => {
    load();
  }, []);

  const children = dashboard?.children || [];

  const selectedStudentRows = useMemo(() => {
    if (!selectedStudentId) return results;
    return results.filter(
      (row) =>
        String(row.studentId || "") === String(selectedStudentId || "")
    );
  }, [results, selectedStudentId]);

  return (
    <div className="st&&alone-page">
      <div className="st&&alone-header">
        <div>
          <span className="topbar-badge">Parent Portal</span>
          <h1>بوابة ولي الأمر</h1>
          <p>متابعة جميع الأبناء وحالة الاختبارات المسجلة ودرجاتهم بعد الأداء.</p>
        </div>
        <Link to="/login" className="ghost-btn">تسجيل الخروج</Link>
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
              {children.length === 0 ? (
                <div className="empty-box">لا يوجد أبناء مرتبطون بهذا الحساب</div>
              ) : (
                children.map((child) => {
                  const childId = child.studentId || child.id || "";
                  const isActive = String(selectedStudentId) === String(childId);
                  return (
                    <button
                      key={childId || child.studentCode}
                      type="button"
                      className={isActive ? "mini-card parent-child-card active" : "mini-card parent-child-card"}
                      onClick={() => setSelectedStudentId(childId)}
                    >
                      <div className="mini-card-head">
                        <h4>{child.studentName || child.fullName}</h4>
                        <span className="mini-pill">{child.studentCode || "-"}</span>
                      </div>
                      <div>الصف: {child.grade || "-"}</div>
                    </button>
                  );
                })
              )}
            </div>
          </section>

          <section className="section-card top-space">
            <div className="section-head">
              <div>
                <h3>الاختبارات المسجلة للطالب</h3>
                <p>يعرض هل أُدي الاختبار أم لا، والدرجة إذا تم الأداء</p>
              </div>
            </div>

            {selectedStudentRows.length === 0 ? (
              <div className="empty-box">لا توجد اختبارات أو نتائج لهذا الطالب</div>
            ) : (
              <div className="table-wrap">
                <table className="app-table">
                  <thead>
                    <tr>
                      <th>الطالب</th>
                      <th>كود الطالب</th>
                      <th>الاختبار</th>
                      <th>كود الاختبار</th>
                      <th>الحالة</th>
                      <th>الدرجة</th>
                      <th>النسبة</th>
                      <th>وقت التسليم</th>
                    </tr>
                  </thead>
                  <tbody>
                    {selectedStudentRows.map((row) => (
                      <tr key={row.id}>
                        <td>{row.studentName}</td>
                        <td>{row.studentCode}</td>
                        <td>{row.examTitle}</td>
                        <td>{row.examCode}</td>
                        <td>
                          <span className={row.isSubmitted ? "status-badge active" : "status-badge inactive"}>
                            {row.statusText}
                          </span>
                        </td>
                        <td>
                          {row.isSubmitted
                            ? `${row.score ?? 0}${row.totalQuestions ? ` / ${row.totalQuestions}` : ""}`
                            : "-"}
                        </td>
                        <td>{row.isSubmitted ? `${row.percentage ?? "-"}%` : "-"}</td>
                        <td>
                          {row.submittedAtUtc
                            ? new Date(row.submittedAtUtc).toLocaleString()
                            : "-"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </div>
  );
}

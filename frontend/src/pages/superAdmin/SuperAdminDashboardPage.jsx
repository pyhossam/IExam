import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { superAdminApi } from "../../services/api";
import "./superAdmin.css";

const numberFormat = new Intl.NumberFormat("ar-SA");

function formatNumber(value) {
  return numberFormat.format(Number(value || 0));
}

function getActivityLevel(item) {
  const students = Number(item.students || 0);
  const teachers = Number(item.teachers || 0);
  const exams = Number(item.exams || 0);
  const sections = Number(item.classSections || 0);
  const score = students + teachers * 3 + sections * 2 + exams * 4;

  if (score >= 80) return { label: "نشاط مرتفع", className: "high" };
  if (score >= 25) return { label: "نشاط متوسط", className: "medium" };
  return { label: "نشاط منخفض", className: "low" };
}

function StatCard({ icon, label, value, hint }) {
  return (
    <div className="sa-stat-card">
      <div className="sa-stat-icon">{icon}</div>
      <div>
        <span>{label}</span>
        <strong>{formatNumber(value)}</strong>
        {hint && <small>{hint}</small>}
      </div>
    </div>
  );
}

function Skeleton() {
  return (
    <div className="sa-page" dir="rtl">
      <div className="sa-hero sa-skeleton-box" />
      <div className="sa-stats-grid">
        {Array.from({ length: 5 }).map((_, index) => (
          <div className="sa-stat-card sa-skeleton-box" key={index} />
        ))}
      </div>
      <div className="sa-card sa-skeleton-box" />
    </div>
  );
}

export default function SuperAdminDashboardPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;

    superAdminApi
      .getDashboard()
      .then((result) => mounted && setData(result))
      .catch((err) => mounted && setError(err.message || "فشل تحميل لوحة المشرف العام"));

    return () => {
      mounted = false;
    };
  }, []);

  const institutions = useMemo(() => data?.institutions || [], [data]);
  const topInstitutions = useMemo(
    () =>
      [...institutions]
        .sort((a, b) => Number(b.exams || 0) + Number(b.students || 0) - (Number(a.exams || 0) + Number(a.students || 0)))
        .slice(0, 5),
    [institutions]
  );

  if (error) {
    return (
      <div className="sa-page" dir="rtl">
        <div className="sa-alert error">{error}</div>
      </div>
    );
  }

  if (!data) return <Skeleton />;

  const activePercent = data.institutionsCount
    ? Math.round((Number(data.activeInstitutionsCount || 0) / Number(data.institutionsCount || 1)) * 100)
    : 0;

  return (
    <div className="sa-page" dir="rtl">
      <section className="sa-hero">
        <div className="sa-hero-content">
          <span className="sa-eyebrow">منصة IExam متعددة المؤسسات</span>
          <h1>أهلًا بك في لوحة المشرف العام</h1>
          <p>
            تابع المؤسسات التعليمية، النشاط العام، وعدد الطلاب والمعلمين والاختبارات من مكان واحد بواجهة واضحة وسريعة.
          </p>
          <div className="sa-hero-actions">
            <Link className="sa-btn primary" to="/super-admin/institutions">
              إدارة المؤسسات
            </Link>
            <Link className="sa-btn soft" to="/login">
              تسجيل الخروج
            </Link>
          </div>
        </div>

        <div className="sa-hero-panel">
          <span>نسبة المؤسسات النشطة</span>
          <strong>{activePercent}%</strong>
          <div className="sa-progress"><i style={{ width: `${activePercent}%` }} /></div>
          <small>{formatNumber(data.activeInstitutionsCount)} نشطة من أصل {formatNumber(data.institutionsCount)}</small>
        </div>
      </section>

      <div className="sa-stats-grid">
        <StatCard icon="🏫" label="المؤسسات" value={data.institutionsCount} hint="إجمالي الجهات المسجلة" />
        <StatCard icon="✅" label="النشطة" value={data.activeInstitutionsCount} hint="جاهزة للاستخدام" />
        <StatCard icon="🎓" label="الطلاب" value={data.totalStudents} hint="داخل جميع المؤسسات" />
        <StatCard icon="👨‍🏫" label="المعلمون" value={data.totalTeachers} hint="حسابات تعليمية" />
        <StatCard icon="📝" label="الاختبارات" value={data.totalExams} hint="منشأة على المنصة" />
      </div>

      <div className="sa-grid-main">
        <section className="sa-card">
          <div className="sa-card-head">
            <div>
              <h2>نشاط المؤسسات</h2>
              <p>ملخص سريع لأهم مؤشرات كل مؤسسة.</p>
            </div>
            <Link className="sa-btn ghost" to="/super-admin/institutions">عرض الكل</Link>
          </div>

          {institutions.length === 0 ? (
            <div className="sa-empty">لا توجد مؤسسات مسجلة بعد.</div>
          ) : (
            <div className="sa-table-wrap">
              <table className="sa-table">
                <thead>
                  <tr>
                    <th>المؤسسة</th>
                    <th>النوع</th>
                    <th>الطلاب</th>
                    <th>المعلمون</th>
                    <th>الشعب</th>
                    <th>الاختبارات</th>
                    <th>النشاط</th>
                    <th>الحالة</th>
                  </tr>
                </thead>
                <tbody>
                  {institutions.map((item) => {
                    const activity = getActivityLevel(item);
                    return (
                      <tr key={item.institutionId || item.id}>
                        <td>
                          <div className="sa-name-cell">
                            <span>{(item.name || "م").slice(0, 1)}</span>
                            <div>
                              <strong>{item.name || "مؤسسة تعليمية"}</strong>
                              <small>{item.email || "لا يوجد بريد"}</small>
                            </div>
                          </div>
                        </td>
                        <td>{item.type || "-"}</td>
                        <td>{formatNumber(item.students)}</td>
                        <td>{formatNumber(item.teachers)}</td>
                        <td>{formatNumber(item.classSections)}</td>
                        <td>{formatNumber(item.exams)}</td>
                        <td><span className={`sa-pill ${activity.className}`}>{activity.label}</span></td>
                        <td><span className={item.isActive ? "sa-status active" : "sa-status inactive"}>{item.isActive ? "نشطة" : "موقوفة"}</span></td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <aside className="sa-card sa-side-card">
          <div className="sa-card-head compact">
            <div>
              <h2>الأكثر نشاطًا</h2>
              <p>حسب عدد الطلاب والاختبارات.</p>
            </div>
          </div>

          <div className="sa-rank-list">
            {topInstitutions.length === 0 ? (
              <div className="sa-empty small">لا توجد بيانات كافية.</div>
            ) : (
              topInstitutions.map((item, index) => (
                <div className="sa-rank-item" key={item.institutionId || item.id}>
                  <span className="sa-rank-number">{index + 1}</span>
                  <div>
                    <strong>{item.name}</strong>
                    <small>{formatNumber(item.students)} طالب · {formatNumber(item.exams)} اختبار</small>
                  </div>
                </div>
              ))
            )}
          </div>
        </aside>
      </div>
    </div>
  );
}

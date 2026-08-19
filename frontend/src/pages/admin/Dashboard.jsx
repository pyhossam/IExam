import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiRequest } from "../../services/api";
import PageIntro from "../../components/ui/PageIntro";
import StatCard from "../../components/ui/StatCard";
import SectionCard from "../../components/ui/SectionCard";

const emptyStats = { exams: 0, students: 0, parents: 0, registrations: 0, attempts: 0 };

export default function Dashboard() {
  const [stats, setStats] = useState(emptyStats);
  const [error, setError] = useState("");

  useEffect(() => {
    getAdminDashboard()
      .then((res) => setStats(res.stats || emptyStats))
      .catch((err) => setError(getReadableErrorMessage(err)));
  }, []);

  return (
    <div>
      <PageIntro
        title="لوحة التحكم الرئيسية"
        description="نظرة سريعة على إحصائيات النظام مع اختصارات للوصول السريع إلى أهم الوظائف."
      />

      {error && <div className="alert error">{error}</div>}

      <div className="stats-grid">
        <StatCard title="الاختبارات" value={stats.exams} />
        <StatCard title="الطلاب" value={stats.students} />
        <StatCard title="أولياء الأمور" value={stats.parents} />
        <StatCard title="التسجيلات" value={stats.registrations} />
        <StatCard title="المحاولات" value={stats.attempts} />
      </div>

      <div className="feature-grid">
        <SectionCard
          title="إدارة الطلاب"
          subtitle="إضافة طالب جديد أو رفع بيانات الطلاب بالجملة."
        >
          <div className="dashboard-actions">
            <Link to="/admin/students" className="primary-btn">فتح صفحة الطلاب</Link>
          </div>
        </SectionCard>

        <SectionCard
          title="إدارة الاختبارات"
          subtitle="إنشاء اختبار AI أو يدوي ثم متابعة الأسئلة والإعدادات."
        >
          <div className="dashboard-actions">
            <Link to="/admin/exams" className="primary-btn">فتح صفحة الاختبارات</Link>
          </div>
        </SectionCard>

        <SectionCard
          title="ربط الطلاب بالاختبارات"
          subtitle="تسجيل الطلاب على اختبار ورفع ملفات التسجيل."
        >
          <div className="dashboard-actions">
            <Link to="/admin/registrations" className="primary-btn">فتح صفحة الربط</Link>
          </div>
        </SectionCard>
      </div>
    </div>
  );
}

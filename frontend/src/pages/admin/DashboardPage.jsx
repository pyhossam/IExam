import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getDashboardOverview, getReadableErrorMessage } from "../../services/api";
import PageIntro from "../../components/ui/PageIntro";
import SectionCard from "../../components/ui/SectionCard";

export default function DashboardPage() {
  const [data, setData] = useState(null);
  const [error, setError] = useState("");
  useEffect(() => { getDashboardOverview().then(setData).catch(err => setError(getReadableErrorMessage(err))); }, []);
  const courseSupervisor = data?.role === "CourseSupervisor";
  return <div>
    <PageIntro title={courseSupervisor ? "لوحة مشرف المقرر" : "لوحة التحكم"} description={courseSupervisor ? "بيانات المقررات المكلّف بالإشراف عليها فقط." : "إحصائيات المؤسسة والخدمات الإدارية."} actions={<Link to="/admin/exams" className="ghost-btn">الاختبارات</Link>} />
    {error && <div className="alert error">{error}</div>}
    {!data ? <div className="section-card">جاري التحميل...</div> : <>
      <div className="stats-grid">
        {!courseSupervisor && <div className="stat-card"><span>المستخدمون</span><strong>{data.usersCount}</strong></div>}
        <div className="stat-card"><span>{courseSupervisor ? "الطلاب المسجلون" : "الطلاب"}</span><strong>{data.studentsCount}</strong></div>
        {!courseSupervisor && <div className="stat-card"><span>أولياء الأمور</span><strong>{data.parentsCount}</strong></div>}
        <div className="stat-card"><span>الاختبارات</span><strong>{data.examsCount}</strong></div>
        <div className="stat-card"><span>المحاولات</span><strong>{data.attemptsCount}</strong></div>
        <div className="stat-card"><span>التسجيلات</span><strong>{data.registrationsCount}</strong></div>
      </div>
      {courseSupervisor && <SectionCard title="المقررات المسندة إليّ" subtitle="تظهر المقررات التي ربطها مشرف المدرسة بحسابك فقط">
        {!data.assignedCourses?.length ? <div className="empty-box">لم يتم ربط حسابك بأي مقرر. يرجى التواصل مع مشرف المدرسة.</div> : <div className="entity-cards-grid">{data.assignedCourses.map(course => <div className="entity-card" key={course.id}><div className="entity-card-head"><div><h3>{course.name}</h3><p>{course.code}</p></div></div><div className="entity-card-body"><div className="entity-meta-row"><span>مخرجات CLO</span><strong>{course.closCount}</strong></div><div className="entity-meta-row"><span>الاختبارات</span><strong>{course.examsCount}</strong></div></div></div>)}</div>}
      </SectionCard>}
      <div className="feature-grid top-space">
        {!courseSupervisor && <SectionCard title="إدارة الطلاب" subtitle="إضافة ورفع بيانات الطلاب"><Link to="/admin/students" className="primary-btn">فتح الصفحة</Link></SectionCard>}
        {courseSupervisor && <SectionCard title="مخرجات المقرر CLO" subtitle="إعداد المخرجات وربطها بمستويات Bloom"><Link to="/admin/course-outcomes" className="primary-btn">إدارة CLO</Link></SectionCard>}
        <SectionCard title="إدارة الاختبارات" subtitle={courseSupervisor ? "اختبارات المقررات المسندة إليك فقط" : "إنشاء وإدارة وطباعة الاختبارات"}><Link to="/admin/exams" className="primary-btn">فتح الاختبارات</Link></SectionCard>
        <SectionCard title="تسجيل الطلاب" subtitle={courseSupervisor ? "تسجيل الطلاب في اختبارات مقرراتك" : "تسجيل الطلاب على الاختبارات"}><Link to="/admin/registrations" className="primary-btn">فتح التسجيلات</Link></SectionCard>
      </div>
    </>}
  </div>;
}

import { Link, useLocation } from "react-router-dom";

const pageTitles = {
  "/admin": "لوحة التحكم",
  "/admin/students": "إدارة الطلاب",
  "/admin/exams": "إدارة الاختبارات",
  "/admin/registrations": "ربط الطلاب بالاختبارات",
  "/student": "بوابة الطالب",
  "/parent": "بوابة ولي الأمر",
};

export default function Header() {
  const location = useLocation();
  const title = pageTitles[location.pathname] || "AI Exam System";
  const isHome = location.pathname === "/admin";

  return (
    <header className="topbar">
      <div>
        <div className="topbar-badge">AI Exam Enterprise</div>
        <h1 className="topbar-title">{title}</h1>
      </div>

      <div className="topbar-actions">
        {!isHome && (
          <Link to="/admin" className="ghost-btn">
            العودة إلى الرئيسية
          </Link>
        )}
      </div>
    </header>
  );
}

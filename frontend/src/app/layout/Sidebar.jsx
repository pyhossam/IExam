import { NavLink } from "react-router-dom";
import { getRole, getRoleGroups } from "../../services/api";
import { useLanguage } from "../i18n/LanguageContext";
const L=(to,ar,en)=>({to,ar,en});
const linksByGroup={
 AdminOnly:[L("/admin","الرئيسية","Home"),L("/admin/students","إدارة الطلاب","Students"),L("/admin/student-account-requests","طلبات حسابات الطلاب","Student Requests"),L("/admin/parents","إدارة أولياء الأمور","Parents"),L("/admin/users","إدارة المستخدمين","Users"),L("/admin/exams","إدارة الاختبارات","Exams"),L("/admin/registrations","إدارة التسجيلات","Registrations"),L("/admin/school","الإدارة التعليمية","School Management"),L("/admin/course-outcomes","مخرجات المقررات CLO","Course Outcomes (CLO)"),L("/admin/education/reports","التقارير التعليمية","Education Reports")],
 ExamSupervisor:[L("/admin","الرئيسية","Home"),L("/admin/exams","إدارة الاختبارات","Exams"),L("/admin/registrations","إدارة التسجيلات","Registrations")],
 CourseSupervisor:[L("/admin","الرئيسية","Home"),L("/admin/course-outcomes","مخرجات المقررات CLO","Course Outcomes (CLO)"),L("/admin/exams","إدارة الاختبارات","Exams"),L("/admin/registrations","تسجيل الطلاب","Student Registration")],
 Student:[L("/student","اختباراتي","My Exams")],Parent:[L("/parent","بوابة ولي الأمر","Parent Portal")],SuperAdmin:[L("/super-admin","لوحة المشرف العام","Super Admin"),L("/super-admin/institutions","إدارة المؤسسات","Institutions")]
};
export default function Sidebar({isOpen,onClose}){const role=getRole(),groups=getRoleGroups(role),{language}=useLanguage(),map=new Map();[...groups,role].forEach(g=>(linksByGroup[g]||[]).forEach(x=>map.set(x.to,x)));return <aside className={`sidebar ${isOpen?"show":""}`}><div className="brand-box"><div className="brand-icon">Q</div><div><h2>QuizSystem</h2><p>{role||"Portal"}</p></div></div><nav className="sidebar-nav">{[...map.values()].map(x=><NavLink key={x.to} to={x.to} end={["/admin","/student","/parent","/super-admin"].includes(x.to)} className={({isActive})=>`sidebar-link ${isActive?"active":""}`} onClick={onClose}>{language==="ar"?x.ar:x.en}</NavLink>)}</nav></aside>}

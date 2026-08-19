import { useEffect, useState } from "react";
import { ArrowRight, CheckCircle2, Eye, EyeOff, MailCheck, ShieldCheck, UserPlus } from "lucide-react";
import { Link } from "react-router-dom";
import { getPublicInstitutions, getReadableErrorMessage, submitStudentAccountRequest } from "../services/api";

export default function StudentSignupPage() {
  const [institutions, setInstitutions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [form, setForm] = useState({ fullName: "", email: "", gender: "", educationStage: "", institutionId: "", password: "" });

  useEffect(() => {
    getPublicInstitutions().then((data) => setInstitutions(Array.isArray(data) ? data : [])).catch((e) => setError(getReadableErrorMessage(e)));
  }, []);

  const change = (e) => setForm({ ...form, [e.target.name]: e.target.value });
  async function submit(e) {
    e.preventDefault(); setLoading(true); setError("");
    try { const result = await submitStudentAccountRequest(form); setMessage(result.message); }
    catch (e) { setError(getReadableErrorMessage(e)); }
    finally { setLoading(false); }
  }

  return <main className="student-signup-page" dir="rtl">
    <div className="signup-orb signup-orb-one" /><div className="signup-orb signup-orb-two" />
    <section className="student-signup-shell">
      <aside className="signup-info-panel">
        <div className="signup-brand"><span>Q</span><div><strong>QuizSystem</strong><small>منصة الاختبارات الذكية</small></div></div>
        <div className="signup-info-copy"><span className="signup-kicker">حساب الطالب</span><h1>ابدأ رحلتك التعليمية<br/><em>بخطوات بسيطة.</em></h1><p>أنشئ حسابك، فعّل بريدك، ثم تابع حالة طلبك لدى المؤسسة التعليمية.</p></div>
        <ol className="signup-steps">
          <li><UserPlus/><div><b>أدخل بياناتك</b><small>بيانات واضحة وآمنة لإنشاء الطلب.</small></div></li>
          <li><MailCheck/><div><b>فعّل بريدك</b><small>سنرسل رابط تحقق إلى بريدك الإلكتروني.</small></div></li>
          <li><ShieldCheck/><div><b>موافقة المؤسسة</b><small>يصلك إشعار فور قبول الطلب أو رفضه.</small></div></li>
        </ol>
      </aside>

      <section className="signup-form-panel">
        <Link className="signup-back" to="/login"><ArrowRight size={18}/> العودة إلى تسجيل الدخول</Link>
        <header><span>طلب انضمام جديد</span><h2>إنشاء حساب طالب</h2><p>جميع الحقول مطلوبة. سيكون البريد الإلكتروني هو اسم المستخدم.</p></header>
        {error && <div className="alert error">{error}</div>}
        {message ? <div className="signup-success"><CheckCircle2/><h2>تم إرسال رابط التفعيل</h2><p>{message}</p><Link className="primary-btn" to="/login">العودة لتسجيل الدخول</Link></div> :
        <form className="signup-form" onSubmit={submit}>
          <label className="signup-wide"><span>الاسم الكامل</span><input name="fullName" autoFocus required value={form.fullName} onChange={change} placeholder="اكتب الاسم كما يظهر في السجلات" /></label>
          <label><span>البريد الإلكتروني</span><input name="email" type="email" autoComplete="email" required value={form.email} onChange={change} placeholder="name@example.com" dir="ltr" /></label>
          <label><span>كلمة المرور</span><div className="signup-password"><input name="password" type={showPassword ? "text" : "password"} minLength="8" required value={form.password} onChange={change} placeholder="8 أحرف على الأقل"/><button type="button" onClick={() => setShowPassword(!showPassword)} aria-label="إظهار كلمة المرور">{showPassword?<EyeOff/>:<Eye/>}</button></div><small>يجب أن تحتوي على حروف وأرقام.</small></label>
          <label><span>النوع</span><select name="gender" required value={form.gender} onChange={change}><option value="">اختر النوع</option><option value="Male">ذكر</option><option value="Female">أنثى</option></select></label>
          <label><span>المرحلة الدراسية</span><select name="educationStage" required value={form.educationStage} onChange={change}><option value="">اختر المرحلة</option><option value="Primary">ابتدائي</option><option value="Intermediate">متوسط</option><option value="Secondary">ثانوي</option><option value="University">جامعي</option></select></label>
          <label className="signup-wide"><span>المؤسسة التعليمية</span><select name="institutionId" required value={form.institutionId} onChange={change}><option value="">اختر المؤسسة التعليمية</option>{institutions.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label>
          <div className="signup-note signup-wide"><MailCheck size={21}/><p><b>تفعيل البريد مطلوب</b><br/>لن يصل الطلب إلى مشرف المؤسسة قبل الضغط على رابط التفعيل.</p></div>
          <button className="signup-submit signup-wide" disabled={loading}>{loading ? "جارٍ إرسال رابط التفعيل..." : "إنشاء الحساب وإرسال رابط التفعيل"}</button>
        </form>}
      </section>
    </section>
  </main>;
}

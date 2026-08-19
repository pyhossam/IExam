import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { getReadableErrorMessage, login, normalizeRole } from "../services/api";
import { useLanguage } from "../app/i18n/LanguageContext";

export default function LoginPage() {
  const navigate = useNavigate();
  const { language, toggleLanguage } = useLanguage();
  const ar = language === "ar";
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(e) {
    e.preventDefault();
    try {
      setLoading(true);
      setError("");
      const data = await login(userName, password);
      if (data.requiresAccountSetup) {
        navigate("/account/setup", { replace: true, state: { currentPassword: password } });
        return;
      }
      const role = normalizeRole(data.role);
      if (role === "SuperAdmin") navigate("/super-admin");
      else if (role === "Student") navigate("/student");
      else if (role === "Parent") navigate("/parent");
      else navigate("/admin");
    } catch (err) {
      setError(getReadableErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="neo-login-page" dir={ar ? "rtl" : "ltr"}>
      <div className="neo-login-noise" />
      <div className="neo-orb orb-one" /><div className="neo-orb orb-two" />
      <button className="neo-language" type="button" onClick={toggleLanguage}>{ar ? "English" : "العربية"}</button>

      <section className="neo-login-stage" aria-hidden="true">
        <div className="neo-brand"><span className="neo-brand-mark">Q</span><div><strong>QuizSystem</strong><small>{ar ? "منصة القياس الذكي" : "Intelligent Assessment Platform"}</small></div></div>
        <div className="neo-copy"><span>{ar ? "تعليم يتحول إلى أثر" : "Turn learning into impact"}</span><h1>{ar ? <>اختبارات أذكى.<br/><em>قرارات أوضح.</em></> : <>Smarter exams.<br/><em>Clearer decisions.</em></>}</h1><p>{ar ? "منصة متكاملة لبناء الاختبارات، قياس مخرجات التعلم، وتحويل النتائج إلى رؤى قابلة للتنفيذ." : "Build assessments, measure learning outcomes, and transform results into actionable insight."}</p></div>
        <div className="neo-visual">
          <div className="neo-core"><span>CLO</span><b>87%</b><small>{ar ? "نسبة التحقق" : "Attainment"}</small></div>
          <div className="neo-ring ring-a"/><div className="neo-ring ring-b"/>
          <div className="neo-float-card card-a"><i>✓</i><span>{ar ? "تحليل فوري" : "Live analytics"}</span></div>
          <div className="neo-float-card card-b"><b>24</b><span>{ar ? "اختبار نشط" : "Active exams"}</span></div>
          <div className="neo-float-card card-c"><i>↗</i><span>{ar ? "أداء متقدم" : "Higher impact"}</span></div>
          {[...Array(12)].map((_, index) => <i className={`neo-particle p-${index + 1}`} key={index}/>) }
        </div>
        <div className="neo-trust"><span>AI</span><span>CLO</span><span>Bloom</span><span>Analytics</span></div>
      </section>

      <section className="neo-login-panel">
        <div className="neo-mobile-brand"><span className="neo-brand-mark">Q</span><strong>QuizSystem</strong></div>
        <div className="neo-form-heading"><span>{ar ? "مرحبًا بعودتك" : "Welcome back"}</span><h2>{ar ? "تسجيل الدخول" : "Sign in"}</h2><p>{ar ? "أدخل بيانات حسابك للوصول إلى مساحة العمل." : "Enter your account details to access your workspace."}</p></div>
        {error && <div className="alert error">{error}</div>}
        <form className="neo-login-form" onSubmit={handleSubmit}>
          <label>{ar ? "اسم المستخدم" : "Username"}<div className="neo-input-wrap"><span>◎</span><input autoFocus autoComplete="username" required value={userName} onChange={(e) => setUserName(e.target.value)} placeholder={ar ? "أدخل اسم المستخدم" : "Enter your username"}/></div></label>
          <label>{ar ? "كلمة المرور" : "Password"}<div className="neo-input-wrap"><span>◇</span><input type={showPassword ? "text" : "password"} autoComplete="current-password" required value={password} onChange={(e) => setPassword(e.target.value)} placeholder={ar ? "أدخل كلمة المرور" : "Enter your password"}/><button type="button" onClick={() => setShowPassword((value) => !value)}>{showPassword ? "◉" : "◌"}</button></div></label>
          <div className="neo-form-meta"><label><input type="checkbox"/> {ar ? "تذكرني" : "Remember me"}</label><Link to="/forgot-password">{ar ? "نسيت كلمة المرور؟" : "Forgot password?"}</Link></div>
          <button className="neo-submit" type="submit" disabled={loading}><span>{loading ? (ar ? "جاري الدخول…" : "Signing in…") : (ar ? "دخول إلى المنصة" : "Enter platform")}</span><i>←</i></button>
        </form>
        <div className="neo-form-meta"><Link to="/student-signup">إنشاء حساب طالب جديد</Link></div>
        <footer>{ar ? "دخول آمن ومشفّر" : "Secure encrypted access"}<span>•</span> QuizSystem © 2026</footer>
      </section>
    </main>
  );
}

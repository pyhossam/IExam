import { useState } from "react";
import { Link } from "react-router-dom";
import { getReadableErrorMessage, requestPasswordReset } from "../services/api";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  async function submit(e) {
    e.preventDefault();
    try {
      setLoading(true); setError("");
      await requestPasswordReset(email);
      setMessage("إذا كان البريد مسجلًا فسيصلك رابط إعادة التعيين خلال دقائق.");
    } catch (err) { setError(getReadableErrorMessage(err)); }
    finally { setLoading(false); }
  }
  return <div className="login-page"><div className="login-card">
    <div className="topbar-badge">استعادة الحساب</div><h1>نسيت كلمة المرور؟</h1>
    <p>أدخل البريد المرتبط بحسابك لإرسال رابط صالح لمدة 30 دقيقة.</p>
    {error && <div className="alert error">{error}</div>}{message && <div className="alert success">{message}</div>}
    <form onSubmit={submit}><label>البريد الإلكتروني</label><input type="email" required value={email} onChange={e => setEmail(e.target.value)} />
    <button className="primary-btn full-btn" disabled={loading}>{loading ? "جارٍ الإرسال..." : "إرسال رابط الاستعادة"}</button></form>
    <Link className="ghost-btn full-btn" to="/login">العودة لتسجيل الدخول</Link>
  </div></div>;
}

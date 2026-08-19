import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getReadableErrorMessage, resetPassword } from "../services/api";

export default function ResetPasswordPage() {
  const [params] = useSearchParams();
  const token = params.get("token") || "";
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  async function submit(e) {
    e.preventDefault();
    if (password !== confirm) return setError("كلمتا المرور غير متطابقتين");
    try { setError(""); await resetPassword(token, password); setMessage("تم تغيير كلمة المرور بنجاح."); }
    catch (err) { setError(getReadableErrorMessage(err)); }
  }
  return <div className="login-page"><div className="login-card">
    <div className="topbar-badge">كلمة مرور جديدة</div><h1>إعادة تعيين كلمة المرور</h1>
    {!token && <div className="alert error">الرابط غير مكتمل.</div>}{error && <div className="alert error">{error}</div>}{message && <div className="alert success">{message}</div>}
    {!message && <form onSubmit={submit}><label>كلمة المرور الجديدة</label><input type="password" minLength="8" required value={password} onChange={e => setPassword(e.target.value)} />
    <label>تأكيد كلمة المرور</label><input type="password" minLength="8" required value={confirm} onChange={e => setConfirm(e.target.value)} />
    <button className="primary-btn full-btn" disabled={!token}>حفظ كلمة المرور</button></form>}
    <Link className="ghost-btn full-btn" to="/login">تسجيل الدخول</Link>
  </div></div>;
}

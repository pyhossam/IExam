import { useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { clearToken, completeFirstLogin, getReadableErrorMessage } from "../services/api";

export default function AccountSetupPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("");
  const [currentPassword, setCurrentPassword] = useState(location.state?.currentPassword || "");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  async function submit(e) {
    e.preventDefault();
    if (newPassword !== confirmPassword) return setError("كلمتا المرور غير متطابقتين");
    try {
      setLoading(true);
      setError("");
      await completeFirstLogin({ email, currentPassword, newPassword });
      clearToken();
      navigate("/login", { replace: true, state: { message: "تم تحديث الحساب. سجّل الدخول بكلمة المرور الجديدة." } });
    } catch (err) {
      setError(getReadableErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return <div className="login-page"><div className="login-card">
    <div className="topbar-badge">إعداد الحساب</div>
    <h1>أكمل بياناتك أولًا</h1>
    <p>يجب إضافة بريد صالح وتغيير كلمة المرور المؤقتة قبل استخدام النظام.</p>
    {error && <div className="alert error">{error}</div>}
    <form onSubmit={submit}>
      <label>البريد الإلكتروني</label><input type="email" required value={email} onChange={e => setEmail(e.target.value)} />
      <label>كلمة المرور الحالية</label><input type="password" required value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} />
      <label>كلمة المرور الجديدة</label><input type="password" minLength="8" required value={newPassword} onChange={e => setNewPassword(e.target.value)} />
      <label>تأكيد كلمة المرور</label><input type="password" minLength="8" required value={confirmPassword} onChange={e => setConfirmPassword(e.target.value)} />
      <button className="primary-btn full-btn" disabled={loading}>{loading ? "جارٍ الحفظ..." : "حفظ ومتابعة"}</button>
    </form>
  </div></div>;
}

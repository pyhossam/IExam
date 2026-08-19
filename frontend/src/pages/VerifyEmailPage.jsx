import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getReadableErrorMessage, verifyEmail } from "../services/api";

export default function VerifyEmailPage() {
  const [params] = useSearchParams();
  const [status, setStatus] = useState("loading");
  const [error, setError] = useState("");
  useEffect(() => {
    const token = params.get("token") || "";
    if (!token) { setError("رابط التحقق غير مكتمل"); setStatus("error"); return; }
    verifyEmail(token).then(() => setStatus("success")).catch(err => { setError(getReadableErrorMessage(err)); setStatus("error"); });
  }, [params]);
  return <div className="login-page"><div className="login-card">
    <div className="topbar-badge">تأكيد البريد</div><h1>التحقق من البريد الإلكتروني</h1>
    {status === "loading" && <div className="alert">جارٍ التحقق...</div>}
    {status === "success" && <div className="alert success">تم تأكيد بريدك الإلكتروني بنجاح.</div>}
    {status === "error" && <div className="alert error">{error}</div>}
    <Link className="primary-btn full-btn" to="/login">الانتقال إلى تسجيل الدخول</Link>
  </div></div>;
}

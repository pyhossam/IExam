import { useEffect, useState } from "react";

export default function AccessDeniedListener() {
  const [message, setMessage] = useState("");

  useEffect(() => {
    function onUnauthorized(event) {
      setMessage(event?.detail?.message || "غير مصرح لك");
    }

    window.addEventListener("app:unauthorized", onUnauthorized);
    return () => window.removeEventListener("app:unauthorized", onUnauthorized);
  }, []);

  if (!message) return null;

  return (
    <div className="auth-popup-backdrop" onClick={() => setMessage("")}>
      <div className="auth-popup-card" onClick={(e) => e.stopPropagation()}>
        <h3>تنبيه</h3>
        <p>{message}</p>
        <button className="primary-btn" type="button" onClick={() => setMessage("")}>
          حسنًا
        </button>
      </div>
    </div>
  );
}

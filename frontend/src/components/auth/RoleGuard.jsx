import { useEffect } from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { getRole, isAuthenticated, isRoleAllowed, requiresAccountSetup } from "../../services/api";

export default function RoleGuard({ allowedRoles = [] }) {
  const location = useLocation();
  const role = getRole();
  const isAllowed = isRoleAllowed(role, allowedRoles);

  useEffect(() => {
    if (isAuthenticated() && !isAllowed) {
      window.dispatchEvent(
        new CustomEvent("app:unauthorized", {
          detail: { message: "غير مصرح لك بالدخول إلى هذه الصفحة" },
        })
      );
    }
  }, [isAllowed]);

  if (!isAuthenticated()) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (requiresAccountSetup()) {
    return <Navigate to="/account/setup" replace />;
  }

  if (!isAllowed) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}

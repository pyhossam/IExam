import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { isAuthenticated, getRole, isRoleAllowed } from "../../services/api";

import MainLayout from "../layout/MainLayout";
import LoginPage from "../../pages/LoginPage";
import DashboardPage from "../../pages/admin/DashboardPage";
import StudentsPage from "../../pages/admin/StudentsPage";
import ExamsPage from "../../pages/admin/ExamsPage";
import ExamManagePage from "../../pages/admin/ExamManagePage";
import RegistrationsPage from "../../pages/admin/RegistrationsPage";
import UsersPage from "../../pages/admin/UsersPage";
import StudentPortalPage from "../../pages/portals/StudentPortalPage";
import StudentExamPage from "../../pages/portals/StudentExamPage";
import ParentPortalPage from "../../pages/portals/ParentPortalPage";
import SchoolManagementPage from "../../pages/admin/school/SchoolManagementPage";
import StudentSignupPage from "../../pages/StudentSignupPage";
import VerifyStudentRegistrationPage from "../../pages/VerifyStudentRegistrationPage";
import StudentAccountRequestsPage from "../../pages/admin/StudentAccountRequestsPage";
import CourseOutcomesPage from "../../pages/admin/CourseOutcomesPage";
import EducationReportsPage from "../../pages/admin/EducationReportsPage";

function ProtectedRoute({ children, roles = [] }) {
  if (!isAuthenticated()) return <Navigate to="/login" replace />;
  const role = getRole();
  if (!isRoleAllowed(role, roles)) return <Navigate to="/login" replace />;
  return children;
}

export default function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/student-signup" element={<StudentSignupPage />} />
        <Route path="/verify-student-registration" element={<VerifyStudentRegistrationPage />} />

        <Route
          path="/admin"
          element={
            <ProtectedRoute roles={["AdminOrSupervisor"]}>
              <MainLayout />
            </ProtectedRoute>
          }
        >
          <Route index element={<DashboardPage />} />
          <Route path="students" element={<StudentsPage />} />
          <Route path="exams" element={<ExamsPage />} />
          <Route path="exams/:examId" element={<ExamManagePage />} />
          <Route path="registrations" element={<RegistrationsPage />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="school" element={<SchoolManagementPage />} />
          <Route path="student-account-requests" element={<StudentAccountRequestsPage />} />
          <Route path="course-outcomes" element={<CourseOutcomesPage />} />
          <Route path="education/reports" element={<EducationReportsPage />} />
        </Route>

        <Route
          path="/student"
          element={
            <ProtectedRoute roles={["Student"]}>
              <StudentPortalPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/parent"
          element={
            <ProtectedRoute roles={["Parent"]}>
              <ParentPortalPage />
            </ProtectedRoute>
          }
        />

        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

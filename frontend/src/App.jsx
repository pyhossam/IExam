import SuperAdminInstitutionsPage from "./pages/superAdmin/SuperAdminInstitutionsPage";
import SuperAdminDashboardPage from "./pages/superAdmin/SuperAdminDashboardPage";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import PrivacyPolicyPage from "./pages/PrivacyPolicyPage";
import MainLayout from "./app/layout/MainLayout";
import RoleGuard from "./components/auth/RoleGuard";
import AccessDeniedListener from "./components/auth/AccessDeniedListener";

import LoginPage from "./pages/LoginPage";
import AccountSetupPage from "./pages/AccountSetupPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import ResetPasswordPage from "./pages/ResetPasswordPage";
import VerifyEmailPage from "./pages/VerifyEmailPage";
import StudentSignupPage from "./pages/StudentSignupPage";
import VerifyStudentRegistrationPage from "./pages/VerifyStudentRegistrationPage";

import DashboardPage from "./pages/admin/DashboardPage";
import ExamsPage from "./pages/admin/ExamsPage";
import StudentsPage from "./pages/admin/StudentsPage";
import ParentsPage from "./pages/admin/ParentsPage";
import RegistrationsPage from "./pages/admin/RegistrationsPage";
import UsersPage from "./pages/admin/UsersPage";
import ExamManagePage from "./pages/admin/ExamManagePage";
import EducationReportsPage from "./pages/admin/EducationReportsPage";
import EducationAdminPage from "./pages/admin/EducationAdminPage";
import SchoolManagementPage from "./pages/admin/school/SchoolManagementPage";
import CourseOutcomesPage from "./pages/admin/CourseOutcomesPage";
import AttemptReviewPage from "./pages/admin/AttemptReviewPage";
import LeaderboardReportPage from "./pages/admin/LeaderboardReportPage";
import StudentAccountRequestsPage from "./pages/admin/StudentAccountRequestsPage";

import StudentPortalPage from "./pages/portals/StudentPortalPage";
import ParentPortalPage from "./pages/portals/ParentPortalPage";
import StudentExamPage from "./pages/portals/StudentExamPage";

export default function App() {
  return (
    <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <AccessDeniedListener />

      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/verify-email" element={<VerifyEmailPage />} />
        <Route path="/account/setup" element={<AccountSetupPage />} />
        <Route path="/student-signup" element={<StudentSignupPage />} />
        <Route path="/verify-student-registration" element={<VerifyStudentRegistrationPage />} />

        <Route element={<RoleGuard allowedRoles={["AdminOrSupervisor"]} />}>
          <Route path="/admin" element={<MainLayout />}>
            <Route index element={<Navigate to="/admin/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="exams" element={<ExamsPage />} />
            <Route path="exams/:examId" element={<ExamManagePage />} />
            <Route path="exams/:examId/attempts/:attemptId" element={<AttemptReviewPage />} />
            <Route path="exams/:examId/leaderboard-report" element={<LeaderboardReportPage />} />
            <Route path="students" element={<StudentsPage />} />
            <Route path="parents" element={<ParentsPage />} />
            <Route path="registrations" element={<RegistrationsPage />} />
            <Route path="users" element={<UsersPage />} />
            <Route path="education/reports" element={<EducationReportsPage />} />
            <Route path="education" element={<EducationAdminPage />} />
            <Route path="school" element={<SchoolManagementPage />} />
            <Route path="course-outcomes" element={<CourseOutcomesPage />} />
            <Route path="student-account-requests" element={<StudentAccountRequestsPage />} />
          </Route>
        </Route>

        <Route element={<RoleGuard allowedRoles={["Student"]} />}>
          <Route path="/student" element={<MainLayout />}>
            <Route index element={<StudentPortalPage />} />
            <Route path="exams/:examId/play" element={<StudentExamPage />} />
          </Route>
        </Route>

        <Route element={<RoleGuard allowedRoles={["Parent"]} />}>
          <Route path="/parent" element={<MainLayout />}>
            <Route index element={<ParentPortalPage />} />
          </Route>
        </Route>

        <Route element={<RoleGuard allowedRoles={["SuperAdmin", "superadmin"]} />}>
          <Route path="/super-admin" element={<SuperAdminDashboardPage />} />
          <Route path="/super-admin/institutions" element={<SuperAdminInstitutionsPage />} />
        </Route>

        <Route path="*" element={<Navigate to="/login" replace />} />
        <Route path="/privacy" element={<PrivacyPolicyPage />} />
</Routes>
    </BrowserRouter>
  );
}

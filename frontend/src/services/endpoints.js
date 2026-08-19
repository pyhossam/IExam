export const API_ENDPOINTS = {
  auth: {
    login: "/auth/login",
  },
  dashboard: {
    overview: "/dashboard/overview",
    examAnalytics: (examId) => `/dashboard/exams/${examId}`,
  },
  exams: {
    list: "/exams",
    ai: "/exams/ai",
    manual: "/exams/manual",
    byId: (examId) => `/exams/${examId}`,
    settings: (examId) => `/exams/${examId}/settings`,
    addQuestion: (examId) => `/exams/${examId}/questions`,
    updateQuestion: (questionId) => `/exams/questions/${questionId}`,
    deleteQuestion: (questionId) => `/exams/questions/${questionId}`,
    questionTemplate: (examId) => `/exams/${examId}/questions/template`,
    uploadQuestions: (examId) => `/exams/${examId}/questions/upload`,
    pdfQuestions: (examId, withAnswers = false) =>
      `/exams/${examId}/pdf/questions?withAnswers=${withAnswers}`,
    pdfForms: (examId, formsCount = 3) =>
      `/exams/${examId}/pdf/random-forms?formsCount=${formsCount}`,
    pdfAnswerKeys: (examId, formsCount = 3) =>
      `/exams/${examId}/pdf/random-forms-answer-keys?formsCount=${formsCount}`,
    attempts: (examId) => `/exams/${examId}/attempts`,
    attemptDetails: (attemptId) => `/exams/attempts/${attemptId}`,
  },
  imports: {
    studentsTemplate: "/imports/students/template",
    students: "/imports/students",
    registrationsTemplate: "/imports/registrations/template",
    registrations: "/imports/registrations",
  },
  admin: {
    dashboard: "/admin/dashboard",
    students: "/admin/students",
    parents: "/admin/parents",
    users: "/admin/users",
    registerStudent: (examId) => `/admin/exams/${examId}/registrations`,
  },
  portal: {
    studentDashboard: "/portal/student/dashboard",
    parentDashboard: "/portal/parent/dashboard",
    leaderboard: (examId) => `/portal/exams/${examId}/leaderboard`,
  },
  student: {
    available: "/student/exams/available",
    start: (examId) => `/student/exams/${examId}/start`,
    submit: "/student/exams/submit",
  },
  reports: {
    studentPdf: (studentId) => `/reports/students/${studentId}/pdf`,
    examPdf: (examId) => `/reports/exams/${examId}/pdf`,
  },
};

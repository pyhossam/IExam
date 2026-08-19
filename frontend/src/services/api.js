const API_BASE = import.meta.env.VITE_API_BASE || "/api";

export async function submitStudentAccountRequest(payload) {
  return apiJson("/public/student-account-requests", "POST", payload);
}
export async function verifyStudentAccountRequest(token) {
  return apiJson("/public/student-account-requests/verify-email", "POST", { token });
}
export async function getPublicInstitutions() { return apiRequest("/public/institutions"); }
export async function getStudentAccountRequests(status = "Pending") { return apiRequest(`/admin/student-account-requests?status=${encodeURIComponent(status)}`); }
export async function decideStudentAccountRequest(id, approve, reason = "") { return apiJson(`/admin/student-account-requests/${id}/decision`, "POST", { approve, reason }); }
export async function getExamCourseSections(examId) { return apiRequest(`/admin/registrations/exams/${examId}/sections`); }
export async function registerSectionToExam(examId, sectionId) { return apiJson(`/admin/registrations/exams/${examId}/sections/${sectionId}`, "POST", {}); }

export function getToken() {
  return localStorage.getItem("token") || "";
}

export function setToken(token) {
  localStorage.setItem("token", token);
}

export function clearToken() {
  localStorage.removeItem("token");
  localStorage.removeItem("role");
  localStorage.removeItem("userName");
  localStorage.removeItem("requiresAccountSetup");
}

function decodeJwtPayload(token) {
  try {
    const payload = token.split(".")[1];
    if (!payload) return null;

    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");

    return JSON.parse(window.atob(padded));
  } catch {
    return null;
  }
}

export function isTokenExpired(token = getToken()) {
  if (!token) return true;

  const payload = decodeJwtPayload(token);
  if (!payload?.exp) return false;

  const expiresAtMs = Number(payload.exp) * 1000;
  return expiresAtMs <= Date.now();
}

function redirectToLogin(message = "انتهت الجلسة. الرجاء تسجيل الدخول مرة أخرى.") {
  clearToken();

  window.dispatchEvent(
    new CustomEvent("app:unauthorized", {
      detail: { message },
    })
  );

  if (window.location.pathname !== "/login") {
    window.location.replace("/login");
  }
}

function ensureTokenIsActive(token = getToken()) {
  if (token && isTokenExpired(token)) {
    const message = "انتهت الجلسة. الرجاء تسجيل الدخول مرة أخرى.";
    redirectToLogin(message);
    throw buildApiError(message, 401);
  }
}

export function setSession(data) {
  if (data?.accessToken) localStorage.setItem("token", data.accessToken);
  if (data?.role) localStorage.setItem("role", normalizeRole(data.role));
  if (data?.userName) localStorage.setItem("userName", data.userName);
  localStorage.setItem("requiresAccountSetup", data?.requiresAccountSetup ? "true" : "false");
}

export function requiresAccountSetup() {
  return localStorage.getItem("requiresAccountSetup") === "true";
}

export function normalizeRole(role) {
  const value = String(role || "").replace(/[\s_-]/g, "").toLowerCase();

  if (value === "adminonly") return "AdminOnly";
  if (value === "adminorsupervisor") return "AdminOrSupervisor";
  if (value === "superorinstitutionadmin") return "SuperOrInstitutionAdmin";
  if (value === "superadmin") return "SuperAdmin";
  if (value === "institutionadmin") return "InstitutionAdmin";
  if (value === "schooladmin") return "SchoolAdmin";
  if (value === "teacher") return "Teacher";
  if (value === "examsupervisor") return "ExamSupervisor";
  if (value === "coursesupervisor") return "CourseSupervisor";
  if (value === "institutionadmin" || value === "schooladmin") return "InstitutionAdmin";
  if (value === "teacher") return "Teacher";
  if (value === "admin") return "Admin";
  if (value === "student") return "Student";
  if (value === "parent") return "Parent";

  return role || "";
}

const permissionGroups = {
  AdminOnly: ["Admin", "InstitutionAdmin", "SchoolAdmin"],
  AdminOrSupervisor: ["Admin", "InstitutionAdmin", "SchoolAdmin", "ExamSupervisor", "CourseSupervisor"],
  SuperOrInstitutionAdmin: ["SuperAdmin", "Admin", "InstitutionAdmin", "SchoolAdmin"],
};

export function getRoleGroups(role) {
  const normalizedRole = normalizeRole(role);

  return Object.entries(permissionGroups)
    .filter(([group, roles]) => group === normalizedRole || roles.map(normalizeRole).includes(normalizedRole))
    .map(([group]) => group);
}

export function isRoleAllowed(role, allowedRoles = []) {
  if (!allowedRoles.length) return true;

  const normalizedRole = normalizeRole(role);
  const roleGroups = getRoleGroups(normalizedRole);

  return allowedRoles.some((allowedRole) => {
    const normalizedAllowedRole = normalizeRole(allowedRole);

    if (normalizedAllowedRole === normalizedRole) return true;
    if (roleGroups.includes(normalizedAllowedRole)) return true;

    const groupRoles = permissionGroups[normalizedAllowedRole] || [];
    return groupRoles.map(normalizeRole).includes(normalizedRole);
  });
}

export function getRole() {
  return normalizeRole(localStorage.getItem("role") || "");
}

export function getUserName() {
  return localStorage.getItem("userName") || "";
}

export function isAuthenticated() {
  const token = getToken();

  if (!token) return false;
  if (isTokenExpired(token)) {
    clearToken();
    return false;
  }

  return true;
}

export function publicUrl(path) {
  return path;
}

export function fileUrl(path) {
  return `${API_BASE}${path}`;
}

function buildApiError(message, status = 0, payload = null) {
  const err = new Error(message || "Request failed");
  err.status = status;
  err.payload = payload;
  return err;
}

export function getReadableErrorMessage(error, fallback = "حدث خطأ غير متوقع") {
  const hasValidationErrors = Object.keys(error?.payload?.errors || {}).length > 0;
  const validationMessage = Object.values(error?.payload?.errors || {}).flat().find(Boolean);
  const raw =
    validationMessage ||
    error?.payload?.detail ||
    error?.payload?.title ||
    error?.message ||
    "";

  const text = String(raw || "").trim();
  const lower = text.toLowerCase();

  if (!text) return fallback;

  if (hasValidationErrors)
    return "بعض البيانات المدخلة غير صحيحة أو غير مكتملة. راجع الحقول المطلوبة ثم حاول مرة أخرى.";

  if (lower.includes("failed to fetch") || lower.includes("networkerror"))
    return "تعذر الاتصال بالخادم. تحقق من اتصال الشبكة ثم حاول مرة أخرى.";
  if (lower.includes("one or more validation errors occurred"))
    return "بعض البيانات المدخلة غير صحيحة. راجع الحقول المطلوبة ثم حاول مرة أخرى.";

  if (lower.includes("current password is incorrect") || lower.includes("كلمة المرور الحالية غير صحيحة"))
    return "كلمة المرور الحالية غير صحيحة. تحقق منها ثم حاول مرة أخرى.";
  if (lower.includes("password must") || lower.includes("8 أحرف على الأقل"))
    return "يجب أن تتكون كلمة المرور من 8 أحرف على الأقل، وأن تحتوي على حروف وأرقام.";
  if (lower.includes("reset link is invalid or expired") || lower.includes("رابط إعادة تعيين"))
    return "رابط إعادة تعيين كلمة المرور غير صالح أو انتهت صلاحيته. اطلب رابطًا جديدًا.";
  if (lower.includes("verification link is invalid or expired") || lower.includes("رابط تأكيد البريد غير صالح"))
    return "رابط تأكيد البريد غير صالح أو انتهت صلاحيته.";
  if (lower.includes("email service is not configured") || lower.includes("تعذر إرسال البريد"))
    return "تعذر إرسال رسالة البريد حاليًا. حاول مرة أخرى لاحقًا أو تواصل مع الإدارة.";
  if (lower.includes("username already exists") || lower.includes("اسم المستخدم مستخدم"))
    return "اسم المستخدم مستخدم بالفعل. اختر اسمًا مختلفًا.";
  if (lower.includes("exam code already exists") || lower.includes("كود الاختبار مستخدم"))
    return "كود الاختبار مستخدم بالفعل. اختر كودًا مختلفًا.";

  if (lower.includes("exam has already ended"))
    return "انتهى وقت الاختبار.";
  if (lower.includes("exam has not started yet"))
    return "الاختبار لم يبدأ بعد.";
  if (lower.includes("student is not registered for this exam"))
    return "الطالب غير مسجل في هذا الاختبار.";
  if (lower.includes("student has already submitted this exam"))
    return "تم تسليم الاختبار مسبقًا.";
  if (lower.includes("attempt not found"))
    return "تعذر العثور على محاولة الاختبار. أعد بدء الاختبار.";
  if (lower.includes("could not save exam submission"))
    return "تعذر حفظ إجابات الاختبار. حاول مرة أخرى.";
  if (lower.includes("exam submission conflicted"))
    return "حدث تعارض أثناء تسليم الاختبار. حدّث الصفحة ثم أعد المحاولة.";
  if (lower.includes("exam is not published yet"))
    return "الاختبار غير منشور حتى الآن.";
  if (lower.includes("exam not found"))
    return "الاختبار غير موجود.";
  if (lower.includes("email is already used") || lower.includes("البريد الإلكتروني مستخدم"))
    return "البريد الإلكتروني مستخدم في حساب آخر. استخدم بريدًا مختلفًا لكل حساب.";
  if (lower.includes("student not found"))
    return "بيانات الطالب غير موجودة.";
  if (lower.includes("request is required"))
    return "بيانات الطلب غير مكتملة.";
  if (lower.includes("examid is required"))
    return "لم يتم تحديد الاختبار.";
  if (lower.includes("unauthorized"))
    return "غير مصرح لك بتنفيذ هذا الإجراء.";
  if (lower.includes("forbidden"))
    return "ليس لديك صلاحية للوصول إلى هذه الخدمة.";

  if (error?.status === 429)
    return "تم إرسال محاولات كثيرة خلال وقت قصير. انتظر قليلًا ثم حاول مرة أخرى.";
  if (error?.status === 404)
    return "تعذر العثور على البيانات المطلوبة، ربما تم حذفها أو لم تعد متاحة.";
  if (error?.status === 503)
    return "الخدمة غير متاحة مؤقتًا. حاول مرة أخرى بعد قليل.";
  if (error?.status >= 500 || /system\.|microsoft\.|sqlite|stack trace|constraint/i.test(text)) {
    const traceId = error?.payload?.traceId;
    return traceId
      ? `حدث خطأ غير متوقع. حاول مرة أخرى، وإذا استمرت المشكلة أرسل رقم التتبع: ${traceId}`
      : "حدث خطأ غير متوقع. حاول مرة أخرى، وإذا استمرت المشكلة تواصل مع الدعم.";
  }

  return text;
}

async function parseResponse(res) {
  const type = res.headers.get("content-type") || "";

  if (res.status === 413) {
    throw buildApiError(
      "حجم الملف أكبر من الحد المسموح. يجب ألا يتجاوز ملف PDF حجم 25 ميجابايت.",
      413
    );
  }

  if (res.status === 401 || res.status === 403) {
    let payload = null;
    let message = "غير مصرح لك باستخدام هذه الخدمة";

    try {
      if (type.includes("application/json")) {
        payload = await res.json();
        message = payload?.detail || payload?.title || payload?.message || message;
      } else {
        const text = await res.text();
        if (text) message = text;
      }
    } catch {
      // ignore
    }

    if (res.status === 401) {
      redirectToLogin(message);
    } else {
      window.dispatchEvent(
        new CustomEvent("app:unauthorized", {
          detail: { message },
        })
      );
    }

    throw buildApiError(message, res.status, payload);
  }

  if (type.includes("application/pdf")) return res;

  if (type.includes("application/json")) {
    const data = await res.json();

    if (!res.ok) {
      const message =
        data?.detail ||
        data?.title ||
        data?.message ||
        "Request failed";

      throw buildApiError(message, res.status, data);
    }

    return data;
  }

  const text = await res.text();

  if (!res.ok) {
    let payload = null;
    let message = text || "Request failed";

    try {
      payload = JSON.parse(text);
      message =
        payload?.detail ||
        payload?.title ||
        payload?.message ||
        message;
    } catch {
      // plain text response
    }

    throw buildApiError(message, res.status, payload);
  }

  return text;
}

export async function apiRequest(path, options = {}) {
  const token = getToken();
  ensureTokenIsActive(token);

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      ...(options.headers || {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  return parseResponse(response);
}

export async function apiJson(path, method = "GET", body = null) {
  return apiRequest(path, {
    method,
    headers: {
      "Content-Type": "application/json",
    },
    body: body ? JSON.stringify(body) : null,
  });
}

export async function apiUpload(path, file) {
  const formData = new FormData();
  formData.append("file", file);

  return apiRequest(path, {
    method: "POST",
    body: formData,
  });
}

export async function login(userName, password) {
  const data = await apiJson("/auth/login", "POST", { userName, password });
  setSession(data);
  return data;
}

export async function completeFirstLogin(payload) {
  const result = await apiJson("/auth/complete-first-login", "POST", payload);
  localStorage.setItem("requiresAccountSetup", "false");
  return result;
}

export async function requestPasswordReset(email) {
  return apiJson("/auth/forgot-password", "POST", { email });
}

export async function resetPassword(token, newPassword) {
  return apiJson("/auth/reset-password", "POST", { token, newPassword });
}

export async function verifyEmail(token) {
  return apiJson("/auth/verify-email", "POST", { token });
}

export async function getAdminDashboard() {
  return apiRequest("/admin/dashboard");
}

export async function getDashboardOverview() {
  return apiRequest("/dashboard/overview");
}

export async function createStudent(payload) {
  return apiJson("/admin/students", "POST", payload);
}

export async function createParent(payload) {
  return apiJson("/admin/parents", "POST", payload);
}

export async function createUser(payload) {
  return apiJson("/admin/users", "POST", payload);
}

export async function getExams() {
  return apiRequest("/exams");
}

export async function getExamById(examId) {
  return apiRequest(`/exams/${examId}`);
}

export async function createAiExam(payload) {
  return apiJson("/exams/ai", "POST", payload);
}

export async function createManualExam(payload) {
  return apiJson("/exams/manual", "POST", payload);
}

export async function updateExamSettings(examId, payload) {
  return apiJson(`/exams/${examId}/settings`, "PUT", payload);
}

export async function getExamAnalytics(examId) {
  return apiRequest(`/dashboard/exams/${examId}`);
}

export async function addExamQuestion(examId, payload) {
  return apiJson(`/exams/${examId}/questions`, "POST", payload);
}

export async function updateExamQuestion(questionId, payload) {
  return apiJson(`/exams/questions/${questionId}`, "PUT", payload);
}

export async function deleteExamQuestion(questionId) {
  return apiRequest(`/exams/questions/${questionId}`, { method: "DELETE" });
}

export async function uploadExamQuestions(examId, file) {
  return apiUpload(`/exams/${examId}/questions/upload`, file);
}

export async function generateAiQuestionPreview(examId, count, file = null) {
  const form = new FormData();
  form.append("count", String(count));
  if (file) form.append("file", file);
  return apiRequest(`/exams/${examId}/questions/ai-preview`, { method: "POST", body: form });
}

export async function uploadStudents(file) {
  return apiUpload("/imports/students", file);
}

export async function uploadRegistrations(file) {
  return apiUpload("/imports/registrations", file);
}

export async function registerStudentToExam(examId, studentId) {
  return apiJson(`/admin/registrations/exams/${examId}`, "POST", { studentId });
}
//new api fore xam registraion
export async function getRegistrationSummary() {
  return apiRequest("/admin/registrations/summary");
}

export async function getExamRegistrations(examId) {
  return apiRequest(`/admin/registrations/exams/${examId}`);
}

export async function deleteRegistration(registrationId) {
  return apiRequest(`/admin/registrations/${registrationId}`, {
    method: "DELETE",
  });
}

export async function clearExamRegistrations(examId) {
  return apiRequest(`/admin/registrations/exams/${examId}`, {
    method: "DELETE",
  });
}
export async function getExamAttempts(examId) {
  return apiRequest(`/exams/${examId}/attempts`);
}

export async function getAttemptDetails(attemptId) {
  return apiRequest(`/exams/attempts/${attemptId}`);
}

export async function resetAttempt(attemptId) {
  return apiRequest(`/admin/exam-attempts/${attemptId}/reset-with-snapshots`, {
    method: "DELETE",
  });
}

export async function getStudentDashboard() {
  return apiRequest("/portal/student/dashboard");
}

export async function getParentDashboard() {
  return apiRequest("/portal/parent/dashboard");
}

export async function getParentChildrenResults(payload) {
  return apiJson("/parent/children/results", "POST", payload);
}

export async function getLeaderboard(examId) {
  return apiRequest(`/portal/exams/${examId}/leaderboard`);
}

export async function submitStudentExam(payload) {
  return apiJson("/student/exams/submit", "POST", normalizeSubmitAnswersForSnapshot(payload));
}

// export async function startStudentExam(payload) {
//   return apiJson("/student/exams/start", "POST", payload);
// }

export async function fetchPdfBlob(path) {
  const token = getToken();
  ensureTokenIsActive(token);

  const response = await fetch(`${API_BASE}${path}`, {
    method: "GET",
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  if (!response.ok) {
    let message = "فشل تحميل ملف PDF";
    try {
      const data = await response.json();
      message = data?.message || data?.detail || data?.title || message;
    } catch {
      try {
        message = await response.text();
      } catch {
        // ignore
      }
    }
    if (response.status === 401) {
      redirectToLogin(message);
      throw buildApiError(message, response.status);
    }

    throw buildApiError(message, response.status);
  }

  return await response.blob();
}

export async function openPdfWithAuth(path, fileName = "document.pdf") {
  const blob = await fetchPdfBlob(path);
  const url = window.URL.createObjectURL(blob);
  window.open(url, "_blank", "noopener,noreferrer");
  setTimeout(() => window.URL.revokeObjectURL(url), 60000);
}

export async function downloadPdfWithAuth(path, fileName = "document.pdf") {
  const blob = await fetchPdfBlob(path);
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => window.URL.revokeObjectURL(url), 60000);
}

export async function fetchFileBlob(path) {
  const token = getToken();
  ensureTokenIsActive(token);

  const response = await fetch(`${API_BASE}${path}`, {
    method: "GET",
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  if (!response.ok) {
    let message = "فشل تحميل الملف";
    try {
      const data = await response.json();
      message = data?.message || data?.detail || data?.title || message;
    } catch {
      try {
        message = await response.text();
      } catch {
        // ignore
      }
    }
    if (response.status === 401) {
      redirectToLogin(message);
      throw buildApiError(message, response.status);
    }

    throw buildApiError(message, response.status);
  }

  const blob = await response.blob();
  const disposition = response.headers.get("content-disposition") || "";

  return {
    blob,
    disposition,
  };
}

function extractFileName(disposition, fallback = "downloaded-file") {
  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) return decodeURIComponent(utf8Match[1]);

  const asciiMatch = disposition.match(/filename="?([^"]+)"?/i);
  if (asciiMatch?.[1]) return asciiMatch[1];

  return fallback;
}

export async function openFileWithAuth(path, fallbackFileName = "document") {
  const { blob } = await fetchFileBlob(path);
  const url = window.URL.createObjectURL(blob);
  window.open(url, "_blank", "noopener,noreferrer");
  setTimeout(() => window.URL.revokeObjectURL(url), 60000);
}

export async function downloadFileWithAuth(path, fallbackFileName = "downloaded-file") {
  const { blob, disposition } = await fetchFileBlob(path);
  const fileName = extractFileName(disposition, fallbackFileName);

  const url = window.URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  a.remove();

  setTimeout(() => window.URL.revokeObjectURL(url), 60000);
}

export async function getUsers() {
  return apiRequest("/admin/users");
}

export async function createManagedUser(payload) {
  return apiJson("/admin/users", "POST", payload);
}

export async function updateManagedUser(userId, payload) {
  return apiJson(`/admin/users/${userId}`, "PUT", payload);
}

export async function toggleManagedUserStatus(userId, isActive) {
  return apiRequest(`/admin/users/${userId}/status?isActive=${isActive}`, {
    method: "PATCH",
  });
}

export async function deleteManagedUser(userId) {
  return apiRequest(`/admin/users/${userId}`, {
    method: "DELETE",
  });
}

export async function adminResetManagedUserPassword(userId, newPassword) {
  return apiJson(`/admin/users/${userId}/reset-password`, "POST", { newPassword });
}

export async function getStudentLookups() {
  // Scoped to the current logged-in institution/school.
  return apiRequest("/admin/school/students");
}

export async function getParentLookups() {
  return apiRequest("/admin/school/parents");
}

export function normalizeStartedExamPayload(data, fallbackExamId = null) {
  const source = data || {};
  const questionsSource =
    source.questions ||
    source.examQuestions ||
    source.paperQuestions ||
    source.items ||
    [];

  const questions = questionsSource.map((q, index) => ({
    id: q.id || q.questionId || q.examQuestionId || `q-${index + 1}`,
    questionText: q.questionText || q.text || q.title || "",
    choices: (q.choices || []).map((c, cIndex) => ({
      displayLabel: c.displayLabel || c.label || ["A", "B", "C", "D"][cIndex],
      originalKey: c.originalKey || c.value || c.key || "",
      text: c.text || c.choiceText || "",
    })),
  }));

  return {
    examId: source.examId || fallbackExamId,
    title: source.title || source.examTitle || "الاختبار",
    examCode: source.examCode || source.code || "",
    attemptId: source.attemptId || null,
    questions,
    startAtUtc: source.startAtUtc || null,
    endAtUtc: source.endAtUtc || null,
    allowStudentExit: source.allowStudentExit !== false,
  };
}

export async function startStudentExam(examId) {
  const data = await apiRequest(`/student/exams/${examId}/start`, {
    method: "POST",
  });

  return normalizeStartedExamPayload(data, examId);
}


export function toAbsoluteFileUrl(url) {
  if (!url) return "";
  if (url.startsWith("http://") || url.startsWith("https://")) return url;

  const apiRoot = API_BASE.replace(/\/api\/?$/, "");
  return `${apiRoot}${url.startsWith("/") ? url : `/${url}`}`;
}

export async function uploadQuestionImage(file) {
  const formData = new FormData();
  formData.append("file", file);

  const token = getToken();
  ensureTokenIsActive(token);

  const response = await fetch(`${API_BASE}/admin/exams/questions/images`, {
    method: "POST",
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: formData,
  });

  if (!response.ok) {
    let message = "فشل رفع صورة السؤال";
    try {
      const data = await response.json();
      message = data?.detail || data?.message || data?.title || message;
    } catch {
      try {
        message = await response.text();
      } catch {
        // ignore
      }
    }
    if (response.status === 401) {
      redirectToLogin(message);
      throw buildApiError(message, response.status);
    }

    throw buildApiError(message, response.status);
  }

  return await response.json();
}


function normalizeSubmitAnswersForSnapshot(payload) {
  if (!payload || !Array.isArray(payload.answers)) return payload;

  return {
    ...payload,
    answers: payload.answers.map((answer) => ({
      questionSnapshotId:
        answer.questionSnapshotId ||
        answer.snapshotId ||
        answer.attemptQuestionSnapshotId ||
        answer.questionId,
      selectedAnswer:
        answer.selectedAnswer ||
        answer.selectedOption ||
        answer.originalKey ||
        answer.displayLabel ||
        "",
    })),
  };
}


export async function submitExamKeepAlive(payload) {
  const token = getToken();
  ensureTokenIsActive(token);

  return fetch(`${API_BASE}/student/exams/submit`, {
    method: "POST",
    keepalive: true,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(payload),
  });
}


export async function saveExamDraft(examId, answers) {
  try {
    return await apiJson(`/student/exams/${examId}/draft`, "POST", { answers });
  } catch (err) {
    if (err?.status === 404) return null;
    throw err;
  }
}

export async function getExamProgress(examId) {
  return apiRequest(`/student/exams/${examId}/progress`);
}

export async function registerExamViolation(examId, payload) {
  return apiJson(`/student/exams/${examId}/violation`, "POST", payload);
}


export const superAdminApi = {
  getInstitutions: () =>
    apiRequest("/super-admin/institutions"),

  createInstitution: (payload) =>
    apiJson("/super-admin/institutions", "POST", payload),

  updateInstitution: (institutionId, payload) =>
    apiJson(`/super-admin/institutions/${institutionId}`, "PUT", payload),

  createInstitutionAdmin: (institutionId, payload) =>
    apiJson(`/super-admin/institutions/${institutionId}/admins`, "POST", payload),

  updateInstitutionAdmin: (institutionId, adminId, payload) =>
    apiJson(`/super-admin/institutions/${institutionId}/admins/${adminId}`, "PUT", payload),

  changeInstitutionStatus: (institutionId, isActive) =>
    apiRequest(`/super-admin/institutions/${institutionId}/status?isActive=${isActive}`, { method: "PATCH" }),

  createDataResetChallenge: (institutionId = null) =>
    apiJson("/super-admin/data-reset/challenge", "POST", { institutionId }),

  resetData: (payload) =>
    apiJson("/super-admin/data-reset", "POST", payload),

  getDashboard: () =>
    apiRequest("/super-admin/dashboard"),
};

// Educational Administration CRUD API
// Keeps all school-management endpoints in one small wrapper so the page stays clean.
export async function getEducationEntities(entity) {
  return apiRequest(`/admin/education/${entity}`);
}

export async function createEducationEntity(entity, payload) {
  return apiJson(`/admin/education/${entity}`, "POST", payload);
}

export async function updateEducationEntity(entity, id, payload) {
  return apiJson(`/admin/education/${entity}/${id}`, "PUT", payload);
}

export async function deleteEducationEntity(entity, id) {
  return apiRequest(`/admin/education/${entity}/${id}`, { method: "DELETE" });
}

export async function toggleEducationEntityStatus(entity, id, isActive) {
  return apiRequest(`/admin/education/${entity}/${id}/status?isActive=${isActive}`, {
    method: "PATCH",
  });
}

// =====================================================
// School Management API - current Swagger routes
// Source routes:
// /api/admin/school/grade-levels
// /api/admin/school/subjects
// /api/admin/school/teachers
// /api/admin/school/class-sections
// =====================================================
export const schoolApi = {
  // Grade levels
  getGradeLevels: () => apiRequest("/admin/school/grade-levels"),
  createGradeLevel: (payload) => apiJson("/admin/school/grade-levels", "POST", payload),
  updateGradeLevel: (id, payload) => apiJson(`/admin/school/grade-levels/${id}`, "PUT", payload),
  deleteGradeLevel: (id) => apiRequest(`/admin/school/grade-levels/${id}`, { method: "DELETE" }),
  changeGradeLevelStatus: (id, isActive) =>
    apiRequest(`/admin/school/grade-levels/${id}/status?isActive=${encodeURIComponent(isActive)}`, {
      method: "PATCH",
    }),

  // Subjects
  getSubjects: (gradeLevelId = "") =>
    apiRequest(`/admin/school/subjects${gradeLevelId ? `?gradeLevelId=${encodeURIComponent(gradeLevelId)}` : ""}`),
  createSubject: (payload) => apiJson("/admin/school/subjects", "POST", payload),
  updateSubject: (id, payload) => apiJson(`/admin/school/subjects/${id}`, "PUT", payload),
  deleteSubject: (id) => apiRequest(`/admin/school/subjects/${id}`, { method: "DELETE" }),
  changeSubjectStatus: (id, isActive) =>
    apiRequest(`/admin/school/subjects/${id}/status?isActive=${encodeURIComponent(isActive)}`, {
      method: "PATCH",
    }),

  // Teachers
  getTeachers: () => apiRequest("/admin/school/teachers"),
  createTeacher: (payload) => apiJson("/admin/school/teachers", "POST", payload),
  updateTeacher: (id, payload) => apiJson(`/admin/school/teachers/${id}`, "PUT", payload),
  deleteTeacher: (id) => apiRequest(`/admin/school/teachers/${id}`, { method: "DELETE" }),
  changeTeacherStatus: (id, isActive) =>
    apiRequest(`/admin/school/teachers/${id}/status?isActive=${encodeURIComponent(isActive)}`, {
      method: "PATCH",
    }),
  assignTeacherSubjects: (id, subjectIds) =>
    apiJson(`/admin/school/teachers/${id}/subjects`, "POST", { subjectIds }),

  // Class sections
  getClassSections: (filters = {}) => {
    const qs = new URLSearchParams();
    if (filters.gradeLevelId) qs.set("gradeLevelId", filters.gradeLevelId);
    if (filters.subjectId) qs.set("subjectId", filters.subjectId);
    if (filters.teacherProfileId) qs.set("teacherProfileId", filters.teacherProfileId);
    const query = qs.toString();
    return apiRequest(`/admin/school/class-sections${query ? `?${query}` : ""}`);
  },
  createClassSection: (payload) => apiJson("/admin/school/class-sections", "POST", payload),
  updateClassSection: (id, payload) => apiJson(`/admin/school/class-sections/${id}`, "PUT", payload),
  deleteClassSection: (id) => apiRequest(`/admin/school/class-sections/${id}`, { method: "DELETE" }),
  changeClassSectionStatus: (id, isActive) =>
    apiRequest(`/admin/school/class-sections/${id}/status?isActive=${encodeURIComponent(isActive)}`, {
      method: "PATCH",
    }),
  getSectionStudents: (id) => apiRequest(`/admin/school/class-sections/${id}/students`),
  assignSectionStudents: (id, studentProfileIds, replaceExisting = false) =>
    apiJson(`/admin/school/class-sections/${id}/students`, "POST", {
      studentProfileIds,
      replaceExisting,
    }),
  removeSectionStudent: (id, studentProfileId) =>
    apiRequest(`/admin/school/class-sections/${id}/students/${studentProfileId}`, { method: "DELETE" }),
};

// Backward-compatible names used by previous EducationAdminPage scripts.
export const getEducationGrades = schoolApi.getGradeLevels;
export const createEducationGrade = schoolApi.createGradeLevel;
export const updateEducationGrade = schoolApi.updateGradeLevel;
export const deleteEducationGrade = schoolApi.deleteGradeLevel;
export const toggleEducationGradeStatus = schoolApi.changeGradeLevelStatus;

export const getEducationSubjects = schoolApi.getSubjects;
export const createEducationSubject = schoolApi.createSubject;
export const updateEducationSubject = schoolApi.updateSubject;
export const deleteEducationSubject = schoolApi.deleteSubject;
export const toggleEducationSubjectStatus = schoolApi.changeSubjectStatus;

export const getEducationTeachers = schoolApi.getTeachers;

export const getCourseClos = (subjectId) => apiRequest(`/courses/${subjectId}/clos`);
export const getAssignedCourses = () => apiRequest("/courses/assigned");
export const getCourseSupervisorAssignments = () => apiRequest("/courses/supervisor-assignments");
export const setCourseSupervisors = (subjectId, teacherProfileIds) => apiJson(`/courses/${subjectId}/supervisors`, "PUT", { teacherProfileIds });
export const createCourseClo = (subjectId, payload) => apiJson(`/courses/${subjectId}/clos`, "POST", payload);
export const deleteCourseClo = (subjectId, id) => apiJson(`/courses/${subjectId}/clos/${id}`, "DELETE");
export const importCourseClos = (subjectId, file) => apiUpload(`/courses/${subjectId}/clos/import`, file);
export const getCourseCloReport = (subjectId) => apiRequest(`/courses/${subjectId}/clo-report`);
export const createEducationTeacher = schoolApi.createTeacher;
export const updateEducationTeacher = schoolApi.updateTeacher;
export const deleteEducationTeacher = schoolApi.deleteTeacher;
export const toggleEducationTeacherStatus = schoolApi.changeTeacherStatus;

export const getEducationSections = schoolApi.getClassSections;
export const createEducationSection = schoolApi.createClassSection;
export const updateEducationSection = schoolApi.updateClassSection;
export const deleteEducationSection = schoolApi.deleteClassSection;
export const toggleEducationSectionStatus = schoolApi.changeClassSectionStatus;

// There is no /student-enrollments endpoint in current Swagger.
// Student registration into sections is handled by class-sections/{id}/students.
export const getEducationStudentEnrollments = async () => [];

// School management API helpers - current Swagger routes
export async function getSchoolGradeLevels() {
  return apiRequest("/admin/school/grade-levels");
}

export async function getSchoolSubjects(gradeLevelId = "") {
  const query = gradeLevelId ? `?gradeLevelId=${encodeURIComponent(gradeLevelId)}` : "";
  return apiRequest(`/admin/school/subjects${query}`);
}

export async function getSchoolTeachers() {
  return apiRequest("/admin/school/teachers");
}

export async function getSchoolClassSections(filters = {}) {
  const params = new URLSearchParams();
  if (filters.gradeLevelId) params.set("gradeLevelId", filters.gradeLevelId);
  if (filters.subjectId) params.set("subjectId", filters.subjectId);
  if (filters.teacherProfileId) params.set("teacherProfileId", filters.teacherProfileId);
  const query = params.toString() ? `?${params.toString()}` : "";
  return apiRequest(`/admin/school/class-sections${query}`);
}

export async function getSchoolParents() {
  return apiRequest("/admin/school/parents");
}

export async function getStudents() {
  return apiRequest("/admin/school/students");
}

export async function updateStudent(studentId, payload) {
  return apiJson(`/admin/students/${studentId}`, "PUT", payload);
}

export async function deleteStudent(studentId) {
  return apiRequest(`/admin/students/${studentId}`, { method: "DELETE" });
}

export async function toggleStudentStatus(studentId, isActive) {
  return apiRequest(`/admin/students/${studentId}/status?isActive=${isActive}`, { method: "PATCH" });
}

// School admin current API helpers
export async function getSchoolNationalities() {
  return apiRequest("/admin/school/nationalities");
}

export async function getSchoolStudents() {
  return apiRequest("/admin/school/students");
}

export async function downloadSchoolReport(reportKey, format = "excel") {
  const safeKey = String(reportKey || "students").trim();
  const safeFormat = String(format || "excel").toLowerCase() === "pdf" ? "pdf" : "excel";

  const map = {
    students: {
      excel: "/admin/school/reports/students/excel",
      pdf: "/admin/school/reports/students/pdf",
      fallback: "students-report",
    },
    parents: {
      excel: "/admin/school/reports/parents/excel",
      pdf: "/admin/school/reports/parents/pdf",
      fallback: "parents-report",
    },
    sections: {
      excel: "/admin/school/reports/sections/excel",
      pdf: "/admin/school/reports/sections/pdf",
      fallback: "sections-report",
    },
    subjects: {
      excel: "/admin/school/reports/subjects/excel",
      pdf: "/admin/school/reports/subjects/pdf",
      fallback: "subjects-report",
    },
    teachers: {
      excel: "/admin/school/reports/teachers/excel",
      pdf: "/admin/school/reports/teachers/pdf",
      fallback: "teachers-report",
    },
  };

  const item = map[safeKey] || map.students;
  const extension = safeFormat === "pdf" ? "pdf" : "xlsx";
  return downloadFileWithAuth(item[safeFormat], `${item.fallback}.${extension}`);
}


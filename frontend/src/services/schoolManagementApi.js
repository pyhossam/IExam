import { apiJson, apiRequest } from "./api";

const BASE = "/admin/school";

function query(params = {}) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") search.append(key, value);
  });
  const text = search.toString();
  return text ? `?${text}` : "";
}

export const schoolApi = {
  getGradeLevels: () => apiRequest(`${BASE}/grade-levels`),
  createGradeLevel: (payload) => apiJson(`${BASE}/grade-levels`, "POST", payload),
  updateGradeLevel: (id, payload) => apiJson(`${BASE}/grade-levels/${id}`, "PUT", payload),
  setGradeLevelStatus: (id, isActive) => apiRequest(`${BASE}/grade-levels/${id}/status?isActive=${isActive}`, { method: "PATCH" }),
  deleteGradeLevel: (id) => apiRequest(`${BASE}/grade-levels/${id}`, { method: "DELETE" }),

  getSubjects: (params = {}) => apiRequest(`${BASE}/subjects${query(params)}`),
  createSubject: (payload) => apiJson(`${BASE}/subjects`, "POST", payload),
  updateSubject: (id, payload) => apiJson(`${BASE}/subjects/${id}`, "PUT", payload),
  setSubjectStatus: (id, isActive) => apiRequest(`${BASE}/subjects/${id}/status?isActive=${isActive}`, { method: "PATCH" }),
  deleteSubject: (id) => apiRequest(`${BASE}/subjects/${id}`, { method: "DELETE" }),

  getTeachers: () => apiRequest(`${BASE}/teachers`),
  createTeacher: (payload) => apiJson(`${BASE}/teachers`, "POST", payload),
  updateTeacher: (id, payload) => apiJson(`${BASE}/teachers/${id}`, "PUT", payload),
  setTeacherStatus: (id, isActive) => apiRequest(`${BASE}/teachers/${id}/status?isActive=${isActive}`, { method: "PATCH" }),
  deleteTeacher: (id) => apiRequest(`${BASE}/teachers/${id}`, { method: "DELETE" }),
  assignTeacherSubjects: (id, subjectIds) => apiJson(`${BASE}/teachers/${id}/subjects`, "POST", { subjectIds }),

  getClassSections: (params = {}) => apiRequest(`${BASE}/class-sections${query(params)}`),
  createClassSection: (payload) => apiJson(`${BASE}/class-sections`, "POST", payload),
  updateClassSection: (id, payload) => apiJson(`${BASE}/class-sections/${id}`, "PUT", payload),
  setClassSectionStatus: (id, isActive) => apiRequest(`${BASE}/class-sections/${id}/status?isActive=${isActive}`, { method: "PATCH" }),
  deleteClassSection: (id) => apiRequest(`${BASE}/class-sections/${id}`, { method: "DELETE" }),

  getSectionStudents: (sectionId) => apiRequest(`${BASE}/class-sections/${sectionId}/students`),
  assignSectionStudents: (sectionId, studentProfileIds, replaceExisting = false) =>
    apiJson(`${BASE}/class-sections/${sectionId}/students`, "POST", { studentProfileIds, replaceExisting }),
  removeSectionStudent: (sectionId, studentProfileId) =>
    apiRequest(`${BASE}/class-sections/${sectionId}/students/${studentProfileId}`, { method: "DELETE" }),
  transferSectionStudent: (sectionId, studentProfileId, targetSectionId) =>
    apiRequest(`${BASE}/class-sections/${sectionId}/students/${studentProfileId}/transfer/${targetSectionId}`, { method: "POST" }),
};

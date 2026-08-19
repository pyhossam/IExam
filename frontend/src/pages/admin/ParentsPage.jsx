import { useEffect, useMemo, useState } from "react";
import {
  apiJson,
  apiRequest,
  createParent,
  getParentLookups,
  getReadableErrorMessage,
  getStudentLookups,
} from "../../services/api";
import PageIntro from "../../components/ui/PageIntro";
import SectionCard from "../../components/ui/SectionCard";

const PAGE_SIZE_OPTIONS = [6, 12, 24, 48];

const initialForm = {
  fullName: "",
  parentCode: "",
  phoneNumber: "",
  userName: "",
  password: "",
  studentIds: [],
  isActive: true,
};

function getParentName(parent) {
  return parent?.fullName || parent?.name || parent?.parentName || "ولي أمر";
}

function getParentCode(parent) {
  return parent?.parentCode || parent?.code || "";
}

function getParentPhone(parent) {
  return parent?.phoneNumber || parent?.phone || "-";
}

function getChildrenCount(parent) {
  if (typeof parent?.childrenCount === "number") return parent.childrenCount;
  if (typeof parent?.childCount === "number") return parent.childCount;
  if (typeof parent?.linkedStudentsCount === "number") return parent.linkedStudentsCount;
  if (typeof parent?.studentCount === "number") return parent.studentCount;
  if (typeof parent?.studentsCount === "number") return parent.studentsCount;
  if (Array.isArray(parent?.students)) return parent.students.length;
  if (Array.isArray(parent?.children)) return parent.children.length;
  return 0;
}

function getStudentName(student) {
  return student?.fullName || student?.name || student?.studentName || "طالب";
}

function getStudentCode(student) {
  return student?.studentCode || student?.code || "";
}

async function getParentsManagement() {
  return apiRequest("/admin/parents");
}

async function updateParent(parentId, payload) {
  return apiJson(`/admin/parents/${parentId}`, "PUT", payload);
}

async function toggleParentStatus(parentId, isActive) {
  return apiRequest(`/admin/parents/${parentId}/status?isActive=${isActive}`, {
    method: "PATCH",
  });
}

async function deleteParent(parentId) {
  return apiRequest(`/admin/parents/${parentId}`, { method: "DELETE" });
}

function exportParentsCsv(rows) {
  const headers = ["الاسم", "الكود", "الجوال", "اسم المستخدم", "عدد الأبناء", "الحالة"];
  const data = rows.map((item) => [
    getParentName(item),
    getParentCode(item),
    getParentPhone(item),
    item?.userName || "",
    getChildrenCount(item),
    item?.isActive === false ? "غير نشط" : "نشط",
  ]);

  const csv = [headers, ...data]
    .map((row) => row.map((cell) => `"${String(cell ?? "").replace(/"/g, '""')}"`).join(","))
    .join("\n");

  const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "parents-list.csv";
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1500);
}

export default function ParentsPage() {
  const [parents, setParents] = useState([]);
  const [students, setStudents] = useState([]);
  const [search, setSearch] = useState("");
  const [pageLoading, setPageLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [menuOpenId, setMenuOpenId] = useState("");
  const [editingId, setEditingId] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [pageSize, setPageSize] = useState(12);
  const [currentPage, setCurrentPage] = useState(1);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [form, setForm] = useState(initialForm);

  async function load() {
    try {
      setPageLoading(true);
      setError("");

      const [parentsData, studentsData] = await Promise.all([
        getParentsManagement(),
        getStudentLookups(),
      ]);

      setParents(Array.isArray(parentsData) ? parentsData : []);
      setStudents(Array.isArray(studentsData) ? studentsData : []);
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحميل بيانات أولياء الأمور"));
    } finally {
      setPageLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  const filteredParents = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return parents;

    return parents.filter((item) => {
      const name = getParentName(item).toLowerCase();
      const code = getParentCode(item).toLowerCase();
      const phone = getParentPhone(item).toLowerCase();
      const userName = (item?.userName || "").toLowerCase();

      return name.includes(q) || code.includes(q) || phone.includes(q) || userName.includes(q);
    });
  }, [parents, search]);

  useEffect(() => {
    setCurrentPage(1);
  }, [search, pageSize, parents.length]);

  const totalPages = Math.max(1, Math.ceil(filteredParents.length / pageSize));
  const safeCurrentPage = Math.min(currentPage, totalPages);
  const startIndex = (safeCurrentPage - 1) * pageSize;
  const pagedParents = filteredParents.slice(startIndex, startIndex + pageSize);

  const stats = useMemo(() => {
    return {
      total: parents.length,
      linkedChildren: parents.reduce((sum, item) => sum + getChildrenCount(item), 0),
      active: parents.filter((item) => item?.isActive !== false).length,
    };
  }, [parents]);

  function resetForm() {
    setForm(initialForm);
    setEditingId("");
  }

  function openCreateModal() {
    resetForm();
    setError("");
    setSuccess("");
    setIsModalOpen(true);
  }

  function openEditModal(parent) {
    setEditingId(parent.id || "");
    setForm({
      fullName: parent?.fullName || parent?.name || "",
      parentCode: parent?.parentCode || parent?.code || "",
      phoneNumber: parent?.phoneNumber || parent?.phone || "",
      userName: parent?.userName || "",
      password: "",
      studentIds: Array.isArray(parent?.studentIds)
        ? parent.studentIds
        : Array.isArray(parent?.students)
        ? parent.students.map((x) => x.id)
        : [],
      isActive: parent?.isActive !== false,
    });
    setMenuOpenId("");
    setError("");
    setSuccess("");
    setIsModalOpen(true);
  }

  function closeModal() {
    setIsModalOpen(false);
    resetForm();
  }

  function toggleStudent(studentId) {
    setForm((prev) => ({
      ...prev,
      studentIds: prev.studentIds.includes(studentId)
        ? prev.studentIds.filter((id) => id !== studentId)
        : [...prev.studentIds, studentId],
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();

    try {
      setSaving(true);
      setError("");
      setSuccess("");

      if (!form.fullName || !form.parentCode || !form.userName) {
        throw new Error("أكمل البيانات الأساسية أولًا");
      }

      const payload = {
        fullName: form.fullName,
        parentCode: form.parentCode,
        phoneNumber: form.phoneNumber,
        userName: form.userName,
        studentIds: form.studentIds,
        isActive: form.isActive,
      };

      if (editingId) {
        await updateParent(editingId, {
          ...payload,
          password: form.password || null,
        });
        setSuccess("تم تحديث بيانات ولي الأمر بنجاح");
      } else {
        if (!form.password) throw new Error("أدخل كلمة المرور");
        await createParent({
          ...payload,
          password: form.password,
        });
        setSuccess("تم إنشاء ولي الأمر بنجاح");
      }

      closeModal();
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حفظ بيانات ولي الأمر"));
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleStatus(parent) {
    try {
      setError("");
      setSuccess("");
      const nextStatus = parent?.isActive === false;
      await toggleParentStatus(parent.id, nextStatus);

      setParents((prev) =>
        prev.map((item) =>
          item.id === parent.id ? { ...item, isActive: nextStatus } : item
        )
      );

      setSuccess(nextStatus ? "تم تفعيل ولي الأمر" : "تم تعطيل ولي الأمر");
      setMenuOpenId("");
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل تحديث حالة ولي الأمر"));
    }
  }

  async function handleDelete(parent) {
    const ok = window.confirm(`هل تريد حذف ولي الأمر "${getParentName(parent)}"؟`);
    if (!ok) return;

    try {
      setError("");
      setSuccess("");
      await deleteParent(parent.id);
      setSuccess("تم حذف ولي الأمر");
      setMenuOpenId("");
      await load();
    } catch (err) {
      setError(getReadableErrorMessage(err, "فشل حذف ولي الأمر"));
    }
  }

  return (
    <div className="entity-page">
      <PageIntro
        title="إدارة أولياء الأمور"
        description="إدارة احترافية لبيانات أولياء الأمور وربطهم بالطلاب ضمن واجهة واضحة وسريعة."
      />

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <section className="entity-hero">
        <div className="entity-hero-copy">
          <span className="entity-badge">Parents Center</span>
          <h2>لوحة أولياء الأمور</h2>
          <p>أضف ولي أمر جديدًا، عدّل بياناته، عطّل حسابه أو احذفه من نفس الصفحة.</p>
        </div>

        <div className="entity-hero-stats">
          <div className="entity-hero-stat">
            <span>إجمالي أولياء الأمور</span>
            <strong>{stats.total}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>إجمالي الأبناء المرتبطين</span>
            <strong>{stats.linkedChildren}</strong>
          </div>
          <div className="entity-hero-stat">
            <span>النشطون</span>
            <strong>{stats.active}</strong>
          </div>
        </div>
      </section>

      <SectionCard
        title="قائمة أولياء الأمور"
        subtitle="بحث سريع، إضافة جديدة، وتصدير مباشر للقائمة"
      >
        <div className="entity-toolbar">
          <div className="entity-search-box">
            <label>بحث</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="ابحث بالاسم أو الكود أو الجوال"
            />
          </div>

          <div className="entity-toolbar-actions">
            <select
              className="paging-size-select"
              value={pageSize}
              onChange={(e) => setPageSize(Number(e.target.value))}
            >
              {PAGE_SIZE_OPTIONS.map((size) => (
                <option key={size} value={size}>
                  {size} لكل صفحة
                </option>
              ))}
            </select>

            <button className="primary-btn slim" type="button" onClick={openCreateModal}>
              إضافة ولي أمر
            </button>
            <button
              className="ghost-btn slim"
              type="button"
              onClick={() => exportParentsCsv(filteredParents)}
              disabled={filteredParents.length === 0}
            >
              تصدير CSV
            </button>
          </div>
        </div>

        {pageLoading ? (
          <div className="empty-box top-space">جاري التحميل...</div>
        ) : filteredParents.length === 0 ? (
          <div className="empty-box top-space">لا يوجد أولياء أمور</div>
        ) : (
          <>
            <div className="entity-cards-grid top-space">
              {pagedParents.map((parent) => (
                <div className="entity-card" key={parent.id || getParentCode(parent)}>
                  <div className="entity-card-head">
                    <div>
                      <h3>{getParentName(parent)}</h3>
                      <p>{parent?.userName || getParentCode(parent) || "ولي أمر"}</p>
                    </div>

                    <div className="entity-card-menu">
                      <span className="mini-pill">{getParentCode(parent) || "بدون كود"}</span>
                      <button
                        type="button"
                        className="entity-menu-trigger"
                        onClick={() => setMenuOpenId(menuOpenId === parent.id ? "" : parent.id)}
                      >
                        ⋮
                      </button>

                      {menuOpenId === parent.id && (
                        <div className="entity-actions-menu">
                          <button
                            type="button"
                            className="entity-actions-menu-btn"
                            onClick={() => openEditModal(parent)}
                          >
                            تعديل
                          </button>
                          <button
                            type="button"
                            className="entity-actions-menu-btn"
                            onClick={() => handleToggleStatus(parent)}
                          >
                            {parent?.isActive === false ? "تفعيل" : "تعطيل"}
                          </button>
                          <button
                            type="button"
                            className="entity-actions-menu-btn danger"
                            onClick={() => handleDelete(parent)}
                          >
                            حذف
                          </button>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="entity-card-body">
                    <div className="entity-meta-row">
                      <span>الجوال</span>
                      <strong>{getParentPhone(parent)}</strong>
                    </div>

                    <div className="entity-meta-row">
                      <span>عدد الأبناء</span>
                      <strong>{getChildrenCount(parent)}</strong>
                    </div>

                    <div className="entity-meta-row">
                      <span>الحالة</span>
                      <strong>
                        <span
                          className={`status-badge ${
                            parent?.isActive === false ? "status-danger" : "status-success"
                          }`}
                        >
                          {parent?.isActive === false ? "غير نشط" : "نشط"}
                        </span>
                      </strong>
                    </div>
                  </div>
                </div>
              ))}
            </div>

            <div className="entity-pagination">
              <div className="entity-pagination-summary">
                عرض {startIndex + 1} - {Math.min(startIndex + pageSize, filteredParents.length)} من{" "}
                {filteredParents.length}
              </div>

              <div className="entity-pagination-actions">
                <button
                  className="ghost-btn slim"
                  type="button"
                  disabled={safeCurrentPage <= 1}
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                >
                  السابق
                </button>

                <span className="entity-pagination-page">
                  صفحة {safeCurrentPage} من {totalPages}
                </span>

                <button
                  className="ghost-btn slim"
                  type="button"
                  disabled={safeCurrentPage >= totalPages}
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                >
                  التالي
                </button>
              </div>
            </div>
          </>
        )}
      </SectionCard>

      {isModalOpen && (
        <div className="entity-modal-backdrop" onClick={closeModal}>
          <div className="entity-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="entity-modal-head">
              <div>
                <h2>{editingId ? "تعديل بيانات ولي الأمر" : "إضافة ولي أمر جديد"}</h2>
                <p>يمكن ربط ولي الأمر بأكثر من طالب من نفس النافذة</p>
              </div>

              <button className="ghost-btn slim" type="button" onClick={closeModal}>
                إغلاق
              </button>
            </div>

            <form className="entity-form-grid" onSubmit={handleSubmit}>
              <div className="entity-form-field">
                <label>اسم ولي الأمر</label>
                <input
                  value={form.fullName}
                  onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                  placeholder="الاسم الكامل"
                />
              </div>

              <div className="entity-form-field">
                <label>كود ولي الأمر</label>
                <input
                  value={form.parentCode}
                  onChange={(e) => setForm({ ...form, parentCode: e.target.value })}
                  placeholder="PAR-1001"
                />
              </div>

              <div className="entity-form-field">
                <label>رقم الجوال</label>
                <input
                  value={form.phoneNumber}
                  onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
                  placeholder="05xxxxxxxx"
                />
              </div>

              <div className="entity-form-field">
                <label>اسم المستخدم</label>
                <input
                  value={form.userName}
                  onChange={(e) => setForm({ ...form, userName: e.target.value })}
                  placeholder="username"
                />
              </div>

              <div className="entity-form-field entity-form-field-wide">
                <label>{editingId ? "كلمة المرور الجديدة" : "كلمة المرور"}</label>
                <input
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                  placeholder={editingId ? "اتركها فارغة بدون تغيير" : "password"}
                />
              </div>

              <div className="entity-form-field entity-form-field-wide">
                <label>ربط الطلاب</label>
                <div className="entity-check-grid">
                  {students.length === 0 ? (
                    <div className="empty-box">لا يوجد طلاب متاحون للربط</div>
                  ) : (
                    students.map((student) => (
                      <label className="entity-check-item" key={student.id}>
                        <input
                          type="checkbox"
                          checked={form.studentIds.includes(student.id)}
                          onChange={() => toggleStudent(student.id)}
                        />
                        <div>
                          <strong>{getStudentName(student)}</strong>
                          <span>{getStudentCode(student) || "بدون كود"}</span>
                        </div>
                      </label>
                    ))
                  )}
                </div>
              </div>

              <div className="entity-form-field entity-form-field-wide">
                <label className="checkbox-line">
                  <input
                    type="checkbox"
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                  />
                  <span>الحساب نشط</span>
                </label>
              </div>

              <div className="entity-form-actions entity-form-field-wide">
                <button className="primary-btn" type="submit" disabled={saving}>
                  {saving ? "جاري الحفظ..." : editingId ? "حفظ التعديلات" : "إنشاء ولي الأمر"}
                </button>
                <button className="ghost-btn" type="button" onClick={closeModal}>
                  إلغاء
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
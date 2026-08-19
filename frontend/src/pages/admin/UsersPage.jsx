import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  createManagedUser,
  deleteManagedUser,
  getParentLookups,
  getStudentLookups,
  getUsers,
  toggleManagedUserStatus,
  updateManagedUser,
  adminResetManagedUserPassword,
  getEducationTeachers,
} from "../../services/api";
import PageIntro from "../../components/ui/PageIntro";
import SectionCard from "../../components/ui/SectionCard";

const initialForm = {
  userName: "",
  password: "",
  role: "Admin",
  studentProfileId: "",
  parentProfileId: "",
  teacherProfileId: "",
  isActive: true,
};

function roleLabel(role) {
  switch (role) {
    case "SuperAdmin":
      return "مشرف عام";
    case "InstitutionAdmin":
    case "SchoolAdmin":
      return "مشرف مؤسسة";
    case "Admin":
      return "مدير النظام";
    case "ExamSupervisor":
      return "مشرف اختبارات";
    case "Student":
      return "طالب";
    case "Parent":
      return "ولي أمر";
    case "Teacher":
      return "معلم";
    default:
      return role || "-";
  }
}

export default function UsersPage() {
  const [rows, setRows] = useState([]);
  const [students, setStudents] = useState([]);
  const [parents, setParents] = useState([]);
  const [teachers, setTeachers] = useState([]);
  const [form, setForm] = useState(initialForm);
  const [editingId, setEditingId] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [search, setSearch] = useState("");
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [openMenuId, setOpenMenuId] = useState("");

  async function load() {
    try {
      setError("");
      const [usersData, studentsData, parentsData, teachersData] = await Promise.all([
        getUsers(),
        getStudentLookups(),
        getParentLookups(),
        getEducationTeachers(),
      ]);

      setRows(Array.isArray(usersData) ? usersData : []);
      setStudents(Array.isArray(studentsData) ? studentsData : []);
      setParents(Array.isArray(parentsData) ? parentsData : []);
      setTeachers(Array.isArray(teachersData) ? teachersData : []);
    } catch (err) {
      setError(err.message || "فشل تحميل المستخدمين");
    }
  }

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    function handleClick() {
      setOpenMenuId("");
    }

    if (openMenuId) {
      window.addEventListener("click", handleClick);
    }

    return () => window.removeEventListener("click", handleClick);
  }, [openMenuId]);

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;

    return rows.filter((x) =>
      (x.userName || "").toLowerCase().includes(q) ||
      (x.role || "").toLowerCase().includes(q) ||
      (x.studentName || "").toLowerCase().includes(q) ||
      (x.parentName || "").toLowerCase().includes(q)
    );
  }, [rows, search]);

  const stats = useMemo(() => {
    const total = rows.length;
    const active = rows.filter((x) => x.isActive).length;
    const inactive = total - active;
    const admins = rows.filter((x) => x.role === "Admin").length;
    const studentsCount = rows.filter((x) => x.role === "Student").length;
    const parentsCount = rows.filter((x) => x.role === "Parent").length;

    return { total, active, inactive, admins, studentsCount, parentsCount };
  }, [rows]);

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

  function closeModal() {
    resetForm();
    setIsModalOpen(false);
  }

  async function handleSubmit(e) {
    e.preventDefault();

    try {
      setLoading(true);
      setError("");
      setSuccess("");

      const payload = {
        userName: form.userName,
        role: form.role,
        isActive: form.isActive,
        studentProfileId: form.studentProfileId || null,
        parentProfileId: form.parentProfileId || null,
        teacherProfileId: form.teacherProfileId || null,
      };

      if (editingId) {
        await updateManagedUser(editingId, payload);
        setSuccess("تم تحديث المستخدم بنجاح");
      } else {
        await createManagedUser({
          ...payload,
          password: form.password,
        });
        setSuccess("تم إنشاء المستخدم بنجاح");
      }

      closeModal();
      await load();
    } catch (err) {
      setError(err.message || "فشل حفظ المستخدم");
    } finally {
      setLoading(false);
    }
  }

  async function handleToggle(user) {
    try {
      setLoading(true);
      setError("");
      setSuccess("");
      await toggleManagedUserStatus(user.id, !user.isActive);
      setSuccess("تم تحديث حالة المستخدم");
      await load();
    } catch (err) {
      setError(err.message || "فشل تحديث الحالة");
    } finally {
      setLoading(false);
    }
  }

  async function handleDelete(user) {
    if (!window.confirm(`هل تريد حذف المستخدم ${user.userName}؟`)) return;

    try {
      setLoading(true);
      setError("");
      setSuccess("");
      await deleteManagedUser(user.id);
      setSuccess("تم حذف المستخدم");
      await load();
    } catch (err) {
      setError(err.message || "فشل حذف المستخدم");
    } finally {
      setLoading(false);
    }
  }

  async function handleResetPassword(user) {
    const newPassword = window.prompt(`أدخل كلمة مرور مؤقتة جديدة للمستخدم ${user.userName}`);
    if (!newPassword) return;
    if (newPassword.length < 8 || !/[A-Za-zأ-ي]/.test(newPassword) || !/\d/.test(newPassword)) {
      setError("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي حروفًا وأرقامًا");
      return;
    }
    try {
      setLoading(true); setError(""); setSuccess("");
      await adminResetManagedUserPassword(user.id, newPassword);
      setSuccess("تم تعيين كلمة مرور مؤقتة، وسيُطلب من المستخدم تغييرها عند أول دخول");
      await load();
    } catch (err) { setError(err.message || "فشل إعادة تعيين كلمة المرور"); }
    finally { setLoading(false); }
  }

  function startEdit(user) {
    setEditingId(user.id);
    setForm({
      userName: user.userName || "",
      password: "",
      role: user.role || "Admin",
      studentProfileId: user.studentProfileId || "",
      parentProfileId: user.parentProfileId || "",
      teacherProfileId: user.teacherProfileId || "",
      isActive: !!user.isActive,
    });
    setError("");
    setSuccess("");
    setIsModalOpen(true);
  }

  return (
    <div className="users-admin-page">
      <PageIntro
        title="إدارة المستخدمين"
        description="واجهة أبسط وأكثر عملية لإدارة جميع الحسابات في النظام."
        actions={
          <div className="topbar-actions">
            <button className="primary-btn" type="button" onClick={openCreateModal}>
              إضافة مستخدم جديد
            </button>
            <Link to="/admin" className="ghost-btn">
              الرئيسية
            </Link>
          </div>
        }
      />

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="users-stats-grid">
        <div className="users-stat-card">
          <span>إجمالي المستخدمين</span>
          <strong>{stats.total}</strong>
        </div>
        <div className="users-stat-card">
          <span>الحسابات النشطة</span>
          <strong>{stats.active}</strong>
        </div>
        <div className="users-stat-card">
          <span>الحسابات المعطلة</span>
          <strong>{stats.inactive}</strong>
        </div>
        <div className="users-stat-card">
          <span>المديرون</span>
          <strong>{stats.admins}</strong>
        </div>
        <div className="users-stat-card">
          <span>الطلاب</span>
          <strong>{stats.studentsCount}</strong>
        </div>
        <div className="users-stat-card">
          <span>أولياء الأمور</span>
          <strong>{stats.parentsCount}</strong>
        </div>
      </div>

      <SectionCard
        title="قائمة المستخدمين"
        subtitle="بطاقات واضحة بدون جدول عريض أو scroll أفقي"
      >
        <div className="users-table-toolbar">
          <div className="users-search-box">
            <label>بحث</label>
            <input
              placeholder="ابحث باسم المستخدم أو الدور أو الربط"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>

          <button className="primary-btn slim" type="button" onClick={openCreateModal}>
            إضافة مستخدم
          </button>
        </div>

        {filteredRows.length === 0 ? (
          <div className="empty-box">لا يوجد مستخدمون</div>
        ) : (
          <div className="users-list-grid">
            {filteredRows.map((user) => (
              <div className="user-list-card" key={user.id}>
                <div className="user-list-card-head">
                  <div>
                    <h3>{user.userName}</h3>
                    <p>{roleLabel(user.role)}</p>
                  </div>

                  <div className="user-card-menu-wrap">
                    <span className={user.isActive ? "status-badge active" : "status-badge inactive"}>
                      {user.isActive ? "نشط" : "معطل"}
                    </span>

                    <button
                      className="user-menu-trigger"
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        setOpenMenuId((prev) => (prev === user.id ? "" : user.id));
                      }}
                    >
                      ⋮
                    </button>

                    {openMenuId === user.id && (
                      <div
                        className="user-actions-menu"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <button
                          className="user-actions-menu-btn"
                          type="button"
                          onClick={() => {
                            setOpenMenuId("");
                            startEdit(user);
                          }}
                        >
                          تعديل
                        </button>

                        <button
                          className="user-actions-menu-btn"
                          type="button"
                          onClick={() => {
                            setOpenMenuId("");
                            handleResetPassword(user);
                          }}
                        >
                          إعادة تعيين كلمة المرور
                        </button>

                        <button
                          className="user-actions-menu-btn"
                          type="button"
                          onClick={() => {
                            setOpenMenuId("");
                            handleToggle(user);
                          }}
                        >
                          {user.isActive ? "تعطيل" : "تفعيل"}
                        </button>

                        <button
                          className="user-actions-menu-btn danger"
                          type="button"
                          onClick={() => {
                            setOpenMenuId("");
                            handleDelete(user);
                          }}
                        >
                          حذف
                        </button>
                      </div>
                    )}
                  </div>
                </div>

                <div className="user-list-card-body">
                  <div className="user-meta-row">
                    <span className="user-meta-label">البريد والحساب</span>
                    <div className="user-role-cell">
                      <div>{user.email || "لم يضف البريد بعد"}</div>
                      {user.mustChangePassword && <div className="user-link-line muted">مطلوب تغيير كلمة المرور عند الدخول</div>}
                    </div>
                  </div>
                  <div className="user-meta-row">
                    <span className="user-meta-label">الربط</span>
                    <div className="user-role-cell">
                      {user.studentName && (
                        <div className="user-link-line">
                          <span className="user-link-label">الطالب:</span>
                          <span>{user.studentName}</span>
                        </div>
                      )}

                      {user.parentName && (
                        <div className="user-link-line">
                          <span className="user-link-label">ولي الأمر:</span>
                          <span>{user.parentName}</span>
                        </div>
                      )}

                      {!user.studentName && !user.parentName && (
                        <div className="user-link-line muted">لا يوجد ربط مباشر</div>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </SectionCard>

      {isModalOpen && (
        <div className="users-modal-backdrop" onClick={closeModal}>
          <div className="users-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="users-modal-head">
              <div>
                <h2>{editingId ? "تعديل مستخدم" : "إضافة مستخدم جديد"}</h2>
                <p>إعداد الحساب وربطه بالملف المناسب</p>
              </div>

              <button className="ghost-btn slim" type="button" onClick={closeModal}>
                إغلاق
              </button>
            </div>

            <form className="users-form-grid" onSubmit={handleSubmit}>
              <div className="users-form-field">
                <label>اسم المستخدم</label>
                <input
                  value={form.userName}
                  onChange={(e) => setForm({ ...form, userName: e.target.value })}
                  placeholder="ادخل اسم المستخدم"
                />
              </div>

              {!editingId && (
                <div className="users-form-field">
                  <label>كلمة المرور</label>
                  <input
                    type="password"
                    value={form.password}
                    onChange={(e) => setForm({ ...form, password: e.target.value })}
                    placeholder="ادخل كلمة المرور"
                  />
                </div>
              )}

              <div className="users-form-field">
                <label>الدور</label>
                <select
                  value={form.role}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      role: e.target.value,
                      studentProfileId: e.target.value === "Student" ? form.studentProfileId : "",
                      parentProfileId: e.target.value === "Parent" ? form.parentProfileId : "",
                      teacherProfileId: ["Teacher", "CourseSupervisor"].includes(e.target.value) ? form.teacherProfileId : "",
                    })
                  }
                >
                  <option value="InstitutionAdmin">مشرف مؤسسة</option>
                  <option value="Admin">مدير النظام</option>
                  <option value="ExamSupervisor">مشرف اختبارات</option>
                  <option value="Teacher">معلم</option>
                  <option value="CourseSupervisor">مشرف مقرر</option>
                  <option value="Student">طالب</option>
                  <option value="Parent">ولي أمر</option>
                </select>
              </div>

              {form.role === "Student" && (
                <div className="users-form-field">
                  <label>ربط الطالب</label>
                  <select
                    value={form.studentProfileId}
                    onChange={(e) => setForm({ ...form, studentProfileId: e.target.value })}
                  >
                    <option value="">اختر الطالب</option>
                    {students.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.fullName || item.name || item.studentName || "طالب"}
                        {item.code ? ` - ${item.code}` : item.studentCode ? ` - ${item.studentCode}` : ""}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {form.role === "Parent" && (
                <div className="users-form-field">
                  <label>ربط ولي الأمر</label>
                  <select
                    value={form.parentProfileId}
                    onChange={(e) => setForm({ ...form, parentProfileId: e.target.value })}
                  >
                    <option value="">اختر ولي الأمر</option>
                    {parents.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.fullName || item.name || item.parentName || "ولي أمر"}
                        {item.code ? ` - ${item.code}` : item.parentCode ? ` - ${item.parentCode}` : ""}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {["Teacher", "CourseSupervisor"].includes(form.role) && (
                <div className="users-form-field">
                  <label>ملف المعلم المرتبط</label>
                  <select required value={form.teacherProfileId} onChange={(e) => setForm({ ...form, teacherProfileId: e.target.value })}>
                    <option value="">اختر المعلم</option>
                    {teachers.map((item) => (
                      <option key={item.id} value={item.id}>
                        {item.fullName || item.name || "معلم"}{item.teacherCode ? ` - ${item.teacherCode}` : ""}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {editingId && (
                <div className="users-form-field users-form-switch">
                  <label className="checkbox-line">
                    <input
                      type="checkbox"
                      checked={form.isActive}
                      onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    />
                    الحساب نشط
                  </label>
                </div>
              )}

              <div className="users-form-actions users-form-field-wide">
                <button className="primary-btn" type="submit" disabled={loading}>
                  {loading ? "جاري الحفظ..." : editingId ? "تحديث المستخدم" : "إضافة المستخدم"}
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

// STUDENT_EXTRA_FIELDS_PATCH_MARKER: Student fields required: branch, nationalId, mobile, nationality, imagePath. Grade should use /api/admin/school/grade-levels.
import { useEffect, useMemo, useState } from "react";
import {
  createStudent,
  deleteStudent,
  getParentLookups,
  getSchoolGradeLevels,
  getStudents,
  toggleStudentStatus,
  updateStudent,
  getSchoolNationalities,
  getSchoolStudents,
} from "../../services/api";
import "../admin/school/schoolManagement.css";



const NATIONALITIES = [
  "سعودي",
  "مصري",
  "سوداني",
  "يمني",
  "سوري",
  "أردني",
  "فلسطيني",
  "لبناني",
  "عراقي",
  "كويتي",
  "بحريني",
  "قطري",
  "إماراتي",
  "عماني",
  "مغربي",
  "جزائري",
  "تونسي",
  "ليبي",
  "هندي",
  "باكستاني",
  "بنغلاديشي",
  "فلبيني",
  "إندونيسي",
  "أخرى",
];

const emptyForm = {
  fullName: "",
  studentCode: "",
  grade: "",
  userName: "",
  password: "",
  parentProfileId: "",
  isActive: true,
};

export default function StudentsPage() {
  const [rows, setRows] = useState([]);
  const [grades, setGrades] = useState([]);
  const [parents, setParents] = useState([]);
  const [search, setSearch] = useState("");
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function load() {
    try {
      setError("");
      const [studentsData, gradesData, parentsData] = await Promise.all([
        getStudents(),
        getSchoolGradeLevels(),
        getParentLookups(),
      ]);

      setRows(Array.isArray(studentsData) ? studentsData : []);
      setGrades(Array.isArray(gradesData) ? gradesData : []);
      setParents(Array.isArray(parentsData) ? parentsData : []);
    } catch (err) {
      setError(err.message || "فشل تحميل بيانات الطلاب");
    }
  }

  useEffect(() => {
    load();
  }, []);

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;

    return rows.filter((x) =>
      String(x.fullName || x.name || "").toLowerCase().includes(q) ||
      String(x.studentCode || x.code || "").toLowerCase().includes(q) ||
      String(x.grade || "").toLowerCase().includes(q)
    );
  }, [rows, search]);

  function openCreate() {
    setEditingId("");
    setForm({ ...emptyForm });
    setModalOpen(true);
    setError("");
    setSuccess("");
  }

  function openEdit(row) {
    setEditingId(row.id);
    setForm({
      fullName: row.fullName || row.name || "",
      studentCode: row.studentCode || row.code || "",
      grade: row.grade || "",
      userName: row.userName || "",
      password: "",
      parentProfileId: row.parentProfileId || "",
      isActive: row.isActive !== false,
    });
    setModalOpen(true);
    setError("");
    setSuccess("");
  }

  async function save(e) {
    e.preventDefault();

    try {
      setLoading(true);
      setError("");
      setSuccess("");

      const payload = {
        fullName: form.fullName,
        studentCode: form.studentCode,
        grade: form.grade,
        branch: form.branch,
        nationalId: form.nationalId,
        mobile: form.mobile,
        nationality: form.nationality,
        imagePath: form.imagePath || null,
        userName: form.userName || null,
        password: form.password || null,
      };

      if (editingId) {
        await updateStudent(editingId, {
          ...payload,
          parentProfileId: form.parentProfileId || null,
          isActive: form.isActive,
        });
        setSuccess("تم تحديث بيانات الطالب بنجاح");
      } else {
        await createStudent(payload);
        setSuccess("تم إضافة الطالب بنجاح");
      }

      setModalOpen(false);
      await load();
    } catch (err) {
      setError(err.message || "فشل حفظ الطالب");
    } finally {
      setLoading(false);
    }
  }

  async function toggle(row) {
    try {
      setLoading(true);
      await toggleStudentStatus(row.id, !row.isActive);
      await load();
    } catch (err) {
      setError(err.message || "فشل تغيير حالة الطالب");
    } finally {
      setLoading(false);
    }
  }

  async function remove(row) {
    if (!window.confirm(`هل تريد حذف الطالب ${row.fullName || row.name}؟`)) return;

    try {
      setLoading(true);
      await deleteStudent(row.id);
      await load();
    } catch (err) {
      setError(err.message || "فشل حذف الطالب");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="education-admin-page">
      <div className="education-page-header">
        <div>
          <h1>إدارة الطلاب</h1>
          <p>إضافة وتعديل الطلاب مع ربط الصف بالمراحل المنشأة داخل الإدارة التعليمية.</p>
        </div>
        <button className="education-primary-btn" type="button" onClick={openCreate}>
          إضافة طالب
        </button>
      </div>

      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}

      <div className="education-stats-grid">
        <div className="education-stat-card">
          <span>إجمالي الطلاب</span>
          <strong>{rows.length}</strong>
        </div>
        <div className="education-stat-card">
          <span>الطلاب النشطون</span>
          <strong>{rows.filter((x) => x.isActive !== false).length}</strong>
        </div>
        <div className="education-stat-card">
          <span>المراحل المتاحة</span>
          <strong>{grades.length}</strong>
        </div>
      </div>

      <div className="education-toolbar">
        <div className="education-search-box">
          <label>بحث</label>
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="ابحث باسم الطالب أو الكود أو المرحلة"
          />
        </div>
      </div>

      <div className="education-table-wrap">
        <table className="education-table">
          <thead>
            <tr>
              <th>اسم الطالب</th>
              <th>كود الطالب</th>
              <th>المرحلة / الصف</th>
              <th>الحالة</th>
              <th>الإجراءات</th>
            </tr>
          </thead>
          <tbody>
            {filteredRows.length === 0 ? (
              <tr>
                <td colSpan="5">
                  <div className="education-empty-box">لا توجد بيانات طلاب</div>
                </td>
              </tr>
            ) : (
              filteredRows.map((row) => (
                <tr key={row.id}>
                  <td><strong>{row.fullName || row.name}</strong></td>
                  <td>{row.studentCode || row.code || "-"}</td>
                  <td>{row.grade || "-"}</td>
                  <td>
                    <span className={row.isActive !== false ? "status-badge active" : "status-badge inactive"}>
                      {row.isActive !== false ? "نشط" : "معطل"}
                    </span>
                  </td>
                  <td>
                    <div className="education-row-actions">
                      <button className="education-action-btn education-edit-btn" type="button" onClick={() => openEdit(row)}>
                        تعديل
                      </button>
                      <button className="education-action-btn" type="button" onClick={() => toggle(row)}>
                        {row.isActive !== false ? "تعطيل" : "تفعيل"}
                      </button>
                      <button className="education-action-btn education-delete-btn" type="button" onClick={() => remove(row)}>
                        حذف
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {modalOpen && (
        <div className="education-modal-backdrop" onClick={() => setModalOpen(false)}>
          <div className="education-modal-card" onClick={(e) => e.stopPropagation()}>
            <div className="education-modal-head">
              <div>
                <h2>{editingId ? "تعديل طالب" : "إضافة طالب"}</h2>
                <p>اختر المرحلة من المراحل المنشأة بالفعل داخل الإدارة التعليمية.</p>
              </div>
              <button className="education-ghost-btn" type="button" onClick={() => setModalOpen(false)}>
                إغلاق
              </button>
            </div>

            <form className="education-form-grid" onSubmit={save}>
              <div className="education-form-field">
                <label>اسم الطالب</label>
                <input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required />
              </div>

              <div className="education-form-field">
                <label>كود الطالب</label>
                <input value={form.studentCode} onChange={(e) => setForm({ ...form, studentCode: e.target.value })} required />
              </div>

              <div className="education-form-field">
                <label>المرحلة / الصف</label>
                <select value={form.grade} onChange={(e) => setForm({ ...form, grade: e.target.value })} required>
                  <option value="">اختر المرحلة</option>
                  {grades.map((grade) => (
                    <option key={grade.id || grade.name} value={grade.name}>
                      {grade.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="education-form-field">
                <label>اسم المستخدم</label>
                <input value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} />
              </div>

              <div className="education-form-field">
                <label>كلمة المرور {editingId ? "(اختياري)" : ""}</label>
                <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
              </div>

              {editingId && (
                <div className="education-form-field">
                  <label>ولي الأمر</label>
                  <select value={form.parentProfileId} onChange={(e) => setForm({ ...form, parentProfileId: e.target.value })}>
                    <option value="">بدون ربط</option>
                    {parents.map((parent) => (
                      <option key={parent.id} value={parent.id}>
                        {parent.fullName || parent.name || parent.parentName}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              {editingId && (
                <label className="education-switch-line">
                  <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
                  الطالب نشط
                </label>
              )}

              
              <div className="education-form-field">
                <label>الفرع</label>
                <select value={form.branch} onChange={(e) => setForm({ ...form, branch: e.target.value })}>
                  <option value="male">بنين</option>
                  <option value="female">بنات</option>
                </select>
              </div>

              <div className="education-form-field">
                <label>الرقم القومي / الهوية</label>
                <input value={form.nationalId} onChange={(e) => setForm({ ...form, nationalId: e.target.value })} placeholder="رقم فريد" />
              </div>

              <div className="education-form-field">
                <label>الجوال</label>
                <input value={form.mobile} onChange={(e) => setForm({ ...form, mobile: e.target.value })} placeholder="05xxxxxxxx" />
              </div>

              <div className="education-form-field">
                <label>الجنسية</label>
                <input list="nationalities-list" value={form.nationality} onChange={(e) => setForm({ ...form, nationality: e.target.value })} placeholder="ابحث أو اختر الجنسية" />
                <datalist id="nationalities-list">
                  {NATIONALITIES.map((item) => <option key={item} value={item} />)}
                </datalist>
              </div>

              <div className="education-form-field">
                <label>مسار الصورة</label>
                <input value={form.imagePath} onChange={(e) => setForm({ ...form, imagePath: e.target.value })} placeholder="اختياري" />
              </div>

              <div className="education-form-actions">
                <button className="education-primary-btn" type="submit" disabled={loading}>
                  {loading ? "جاري الحفظ..." : "حفظ"}
                </button>
                <button className="education-ghost-btn" type="button" onClick={() => setModalOpen(false)}>
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

import { useEffect, useMemo, useState } from "react";
import { schoolApi, getStudentLookups } from "../../services/api";
import "../admin/school/schoolManagement.css";

const TABS = [
  { key: "grades", label: "المراحل الدراسية", icon: "🎓" },
  { key: "subjects", label: "المقررات", icon: "📚" },
  { key: "teachers", label: "المعلمون", icon: "👨‍🏫" },
  { key: "sections", label: "الشعب", icon: "🏫" },
  { key: "students", label: "تسجيل الطلاب", icon: "🧑‍🎓" },
];

const emptyForms = {
  grades: { name: "", order: 1, isActive: true },
  subjects: { gradeLevelId: "", name: "", code: "", isActive: true },
  teachers: { userId: "", fullName: "", phoneNumber: "", email: "", teacherCode: "", subjectIds: [], isActive: true },
  sections: { gradeLevelId: "", subjectId: "", teacherProfileId: "", name: "", genderType: "عام", academicYear: "2025/2026", term: "الأول", capacity: 30, isActive: true },
  students: { classSectionId: "", studentProfileIds: [], replaceExisting: false },
};

function asArray(value) {
  if (Array.isArray(value)) return value;
  if (Array.isArray(value?.items)) return value.items;
  if (Array.isArray(value?.data)) return value.data;
  if (Array.isArray(value?.results)) return value.results;
  return [];
}

function itemName(item) {
  return item?.name || item?.fullName || item?.title || item?.studentName || item?.subjectName || item?.gradeLevelName || "-";
}

function itemId(item) {
  return item?.id || item?.studentProfileId || item?.teacherProfileId || item?.subjectId || item?.gradeLevelId;
}

function StatusBadge({ active }) {
  return <span className={active ? "edu-status active" : "edu-status inactive"}>{active ? "نشط" : "معطل"}</span>;
}

export default function EducationAdminPage() {
  const [activeTab, setActiveTab] = useState("grades");
  const [grades, setGrades] = useState([]);
  const [subjects, setSubjects] = useState([]);
  const [teachers, setTeachers] = useState([]);
  const [sections, setSections] = useState([]);
  const [students, setStudents] = useState([]);
  const [sectionStudents, setSectionStudents] = useState([]);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState("");
  const [form, setForm] = useState(emptyForms.grades);

  async function loadAll() {
    setLoading(true);
    setError("");
    try {
      const [g, s, t, c, st] = await Promise.all([
        schoolApi.getGradeLevels(),
        schoolApi.getSubjects(),
        schoolApi.getTeachers(),
        schoolApi.getClassSections(),
        getStudentLookups().catch(() => []),
      ]);
      setGrades(asArray(g));
      setSubjects(asArray(s));
      setTeachers(asArray(t));
      setSections(asArray(c));
      setStudents(asArray(st));
    } catch (err) {
      setError(err?.message || "فشل تحميل بيانات الإدارة التعليمية");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { loadAll(); }, []);

  async function loadSectionStudents(sectionId) {
    if (!sectionId) {
      setSectionStudents([]);
      return;
    }
    try {
      const data = await schoolApi.getSectionStudents(sectionId);
      setSectionStudents(asArray(data));
    } catch {
      setSectionStudents([]);
    }
  }

  function openCreate(tab = activeTab) {
    setEditingId("");
    const base = { ...emptyForms[tab] };
    if (tab === "subjects" && grades[0]) base.gradeLevelId = itemId(grades[0]);
    if (tab === "sections") {
      if (grades[0]) base.gradeLevelId = itemId(grades[0]);
      if (subjects[0]) base.subjectId = itemId(subjects[0]);
      if (teachers[0]) base.teacherProfileId = itemId(teachers[0]);
    }
    if (tab === "students" && sections[0]) base.classSectionId = itemId(sections[0]);
    setForm(base);
    setError("");
    setSuccess("");
    setModalOpen(true);
  }

  function openEdit(tab, row) {
    setEditingId(itemId(row));
    if (tab === "grades") setForm({ name: row.name || "", order: row.order || 1, isActive: row.isActive !== false });
    if (tab === "subjects") setForm({ gradeLevelId: row.gradeLevelId || "", name: row.name || "", code: row.code || "", isActive: row.isActive !== false });
    if (tab === "teachers") setForm({ userId: row.userId || "", fullName: row.fullName || "", phoneNumber: row.phoneNumber || "", email: row.email || "", teacherCode: row.teacherCode || "", subjectIds: row.subjectIds || [], isActive: row.isActive !== false });
    if (tab === "sections") setForm({ gradeLevelId: row.gradeLevelId || "", subjectId: row.subjectId || "", teacherProfileId: row.teacherProfileId || "", name: row.name || "", genderType: row.genderType || "عام", academicYear: row.academicYear || "2025/2026", term: row.term || "الأول", capacity: row.capacity || 30, isActive: row.isActive !== false });
    setError("");
    setSuccess("");
    setModalOpen(true);
  }

  async function save(e) {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");
    try {
      if (activeTab === "grades") {
        const payload = { name: form.name, order: Number(form.order || 1), isActive: !!form.isActive };
        editingId ? await schoolApi.updateGradeLevel(editingId, payload) : await schoolApi.createGradeLevel(payload);
      }
      if (activeTab === "subjects") {
        const payload = { gradeLevelId: form.gradeLevelId, name: form.name, code: form.code, isActive: !!form.isActive };
        editingId ? await schoolApi.updateSubject(editingId, payload) : await schoolApi.createSubject(payload);
      }
      if (activeTab === "teachers") {
        const payload = { userId: form.userId || null, fullName: form.fullName, phoneNumber: form.phoneNumber, email: form.email, teacherCode: form.teacherCode, isActive: !!form.isActive, subjectIds: form.subjectIds || [] };
        editingId ? await schoolApi.updateTeacher(editingId, payload) : await schoolApi.createTeacher(payload);
      }
      if (activeTab === "sections") {
        const payload = { gradeLevelId: form.gradeLevelId, subjectId: form.subjectId, teacherProfileId: form.teacherProfileId || null, name: form.name, genderType: form.genderType, academicYear: form.academicYear, term: form.term, capacity: Number(form.capacity || 0), isActive: !!form.isActive };
        editingId ? await schoolApi.updateClassSection(editingId, payload) : await schoolApi.createClassSection(payload);
      }
      if (activeTab === "students") {
        await schoolApi.assignSectionStudents(form.classSectionId, form.studentProfileIds || [], !!form.replaceExisting);
        await loadSectionStudents(form.classSectionId);
      }
      setSuccess("تم الحفظ بنجاح");
      setModalOpen(false);
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل حفظ البيانات");
    } finally {
      setLoading(false);
    }
  }

  async function remove(tab, row) {
    if (!confirm("هل تريد الحذف؟")) return;
    setLoading(true);
    setError("");
    try {
      const id = itemId(row);
      if (tab === "grades") await schoolApi.deleteGradeLevel(id);
      if (tab === "subjects") await schoolApi.deleteSubject(id);
      if (tab === "teachers") await schoolApi.deleteTeacher(id);
      if (tab === "sections") await schoolApi.deleteClassSection(id);
      setSuccess("تم الحذف بنجاح");
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل الحذف");
    } finally {
      setLoading(false);
    }
  }

  async function toggle(tab, row) {
    setLoading(true);
    setError("");
    try {
      const id = itemId(row);
      const next = !(row.isActive !== false);
      if (tab === "grades") await schoolApi.changeGradeLevelStatus(id, next);
      if (tab === "subjects") await schoolApi.changeSubjectStatus(id, next);
      if (tab === "teachers") await schoolApi.changeTeacherStatus(id, next);
      if (tab === "sections") await schoolApi.changeClassSectionStatus(id, next);
      setSuccess("تم تحديث الحالة");
      await loadAll();
    } catch (err) {
      setError(err?.message || "فشل تحديث الحالة");
    } finally {
      setLoading(false);
    }
  }

  async function removeStudentFromSection(studentProfileId) {
    if (!form.classSectionId || !studentProfileId) return;
    if (!confirm("هل تريد إزالة الطالب من الشعبة؟")) return;
    await schoolApi.removeSectionStudent(form.classSectionId, studentProfileId);
    await loadSectionStudents(form.classSectionId);
  }

  const rows = useMemo(() => {
    const source = activeTab === "grades" ? grades : activeTab === "subjects" ? subjects : activeTab === "teachers" ? teachers : activeTab === "sections" ? sections : [];
    const q = search.trim().toLowerCase();
    if (!q) return source;
    return source.filter((x) => JSON.stringify(x).toLowerCase().includes(q));
  }, [activeTab, grades, subjects, teachers, sections, search]);

  const stats = [
    { label: "المراحل", value: grades.length },
    { label: "المقررات", value: subjects.length },
    { label: "المعلمون", value: teachers.length },
    { label: "الشعب", value: sections.length },
    { label: "الطلاب", value: students.length },
  ];

  return (
    <div className="education-admin-page" dir="rtl">
      <div className="education-hero">
        <div>
          <h1>الإدارة التعليمية</h1>
                  </div>
        <button className="education-primary-btn" onClick={() => openCreate(activeTab)} disabled={loading}>
          إضافة {TABS.find((x) => x.key === activeTab)?.label}
        </button>
      </div>

      {error && <div className="education-alert error">{error}</div>}
      {success && <div className="education-alert success">{success}</div>}

      <div className="education-stats-grid">
        {stats.map((s) => <div className="education-stat-card" key={s.label}><span>{s.label}</span><strong>{s.value}</strong></div>)}
      </div>

      <div className="education-tabs">
        {TABS.map((tab) => (
          <button key={tab.key} className={activeTab === tab.key ? "education-tab active" : "education-tab"} onClick={() => { setActiveTab(tab.key); setSearch(""); setError(""); setSuccess(""); }}>
            <span>{tab.icon}</span>{tab.label}
          </button>
        ))}
      </div>

      <div className="education-panel">
        <div className="education-toolbar">
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="بحث..." />
          <button className="education-primary-btn slim" onClick={() => openCreate(activeTab)} disabled={loading}>إضافة</button>
        </div>

        {activeTab === "students" ? (
          <StudentRegistrationPanel
            sections={sections}
            students={students}
            sectionStudents={sectionStudents}
            form={form}
            setForm={setForm}
            openCreate={openCreate}
            loadSectionStudents={loadSectionStudents}
            removeStudentFromSection={removeStudentFromSection}
          />
        ) : (
          <EntityTable tab={activeTab} rows={rows} grades={grades} subjects={subjects} teachers={teachers} onEdit={openEdit} onDelete={remove} onToggle={toggle} />
        )}
      </div>

      {modalOpen && (
        <div className="education-modal-backdrop" onMouseDown={() => setModalOpen(false)}>
          <div className="education-modal-card" onMouseDown={(e) => e.stopPropagation()}>
            <div className="education-modal-head">
              <div><h2>{editingId ? "تعديل" : "إضافة"} {TABS.find((x) => x.key === activeTab)?.label}</h2><p>أدخل البيانات المطلوبة ثم اضغط حفظ.</p></div>
              <button className="education-ghost-btn" onClick={() => setModalOpen(false)}>إغلاق</button>
            </div>
            <form className="education-form-grid" onSubmit={save}>
              <FormFields activeTab={activeTab} form={form} setForm={setForm} grades={grades} subjects={subjects} teachers={teachers} sections={sections} students={students} />
              <div className="education-form-actions">
                <button className="education-primary-btn" disabled={loading}>{loading ? "جاري الحفظ..." : "حفظ"}</button>
                <button type="button" className="education-ghost-btn" onClick={() => setModalOpen(false)}>إلغاء</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}

function EntityTable({ tab, rows, grades, subjects, teachers, onEdit, onDelete, onToggle }) {
  if (!rows.length) return <div className="education-empty">لا توجد بيانات</div>;
  const gradeName = (id) => itemName(grades.find((x) => itemId(x) === id));
  const subjectName = (id) => itemName(subjects.find((x) => itemId(x) === id));
  const teacherName = (id) => itemName(teachers.find((x) => itemId(x) === id));

  return (
    <div className="education-table-wrap">
      <table className="education-table">
        <thead><tr><th>الاسم</th><th>بيانات إضافية</th><th>الحالة</th><th>الإجراءات</th></tr></thead>
        <tbody>
          {rows.map((row) => (
            <tr key={itemId(row)}>
              <td><strong>{itemName(row)}</strong></td>
              <td>
                {tab === "grades" && <span>الترتيب: {row.order ?? "-"}</span>}
                {tab === "subjects" && <span>{row.code || "-"} | المرحلة: {row.gradeLevelName || gradeName(row.gradeLevelId)}</span>}
                {tab === "teachers" && <span>{row.teacherCode || "-"} | {row.email || "-"} | {row.phoneNumber || "-"}</span>}
                {tab === "sections" && <span>{row.academicYear || "-"} | {row.term || "-"} | {subjectName(row.subjectId)} | {teacherName(row.teacherProfileId)}</span>}
              </td>
              <td><StatusBadge active={row.isActive !== false} /></td>
              <td className="education-actions">
                <button onClick={() => onEdit(tab, row)}>تعديل</button>
                <button onClick={() => onToggle(tab, row)}>{row.isActive !== false ? "تعطيل" : "تفعيل"}</button>
                <button className="danger" onClick={() => onDelete(tab, row)}>حذف</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function StudentRegistrationPanel({ sections, students, sectionStudents, form, setForm, openCreate, loadSectionStudents, removeStudentFromSection }) {
  return (
    <div className="education-student-registration">
      <div className="education-form-grid compact">
        <label>الشعبة</label>
        <select value={form.classSectionId || ""} onChange={(e) => { setForm({ ...form, classSectionId: e.target.value }); loadSectionStudents(e.target.value); }}>
          <option value="">اختر الشعبة</option>
          {sections.map((x) => <option key={itemId(x)} value={itemId(x)}>{itemName(x)}</option>)}
        </select>
        <button className="education-primary-btn" onClick={() => openCreate("students")}>تسجيل طلاب</button>
      </div>
      <h3>طلاب الشعبة</h3>
      {!sectionStudents.length ? <div className="education-empty">لا يوجد طلاب مسجلون في الشعبة المحددة</div> : (
        <div className="education-table-wrap"><table className="education-table"><tbody>{sectionStudents.map((s) => <tr key={itemId(s)}><td>{itemName(s)}</td><td>{s.studentCode || s.code || "-"}</td><td><button className="danger" onClick={() => removeStudentFromSection(itemId(s))}>إزالة</button></td></tr>)}</tbody></table></div>
      )}
    </div>
  );
}

function FormFields({ activeTab, form, setForm, grades, subjects, teachers, sections, students }) {
  const set = (k, v) => setForm({ ...form, [k]: v });
  if (activeTab === "grades") return <><Field label="اسم المرحلة *"><input required value={form.name} onChange={(e) => set("name", e.target.value)} /></Field><Field label="الترتيب"><input type="number" value={form.order} onChange={(e) => set("order", e.target.value)} /></Field><ActiveField form={form} set={set} /></>;
  if (activeTab === "subjects") return <><Field label="المرحلة *"><Select items={grades} value={form.gradeLevelId} onChange={(v) => set("gradeLevelId", v)} required /></Field><Field label="اسم المقرر *"><input required value={form.name} onChange={(e) => set("name", e.target.value)} /></Field><Field label="كود المقرر"><input value={form.code} onChange={(e) => set("code", e.target.value)} /></Field><ActiveField form={form} set={set} /></>;
  if (activeTab === "teachers") return <><Field label="اسم المعلم *"><input required value={form.fullName} onChange={(e) => set("fullName", e.target.value)} /></Field><Field label="كود المعلم"><input value={form.teacherCode} onChange={(e) => set("teacherCode", e.target.value)} /></Field><Field label="الجوال"><input value={form.phoneNumber} onChange={(e) => set("phoneNumber", e.target.value)} /></Field><Field label="البريد"><input value={form.email} onChange={(e) => set("email", e.target.value)} /></Field><Field label="المقررات"><MultiSelect items={subjects} value={form.subjectIds || []} onChange={(v) => set("subjectIds", v)} /></Field><ActiveField form={form} set={set} /></>;
  if (activeTab === "sections") return <><Field label="المرحلة *"><Select items={grades} value={form.gradeLevelId} onChange={(v) => set("gradeLevelId", v)} required /></Field><Field label="المقرر *"><Select items={subjects} value={form.subjectId} onChange={(v) => set("subjectId", v)} required /></Field><Field label="المعلم"><Select items={teachers} value={form.teacherProfileId} onChange={(v) => set("teacherProfileId", v)} /></Field><Field label="اسم الشعبة *"><input required value={form.name} onChange={(e) => set("name", e.target.value)} /></Field><Field label="النوع"><input value={form.genderType} onChange={(e) => set("genderType", e.target.value)} /></Field><Field label="العام الدراسي"><input value={form.academicYear} onChange={(e) => set("academicYear", e.target.value)} /></Field><Field label="الفصل"><input value={form.term} onChange={(e) => set("term", e.target.value)} /></Field><Field label="السعة"><input type="number" value={form.capacity} onChange={(e) => set("capacity", e.target.value)} /></Field><ActiveField form={form} set={set} /></>;
  if (activeTab === "students") return <><Field label="الشعبة *"><Select items={sections} value={form.classSectionId} onChange={(v) => set("classSectionId", v)} required /></Field><Field label="الطلاب"><MultiSelect items={students} value={form.studentProfileIds || []} onChange={(v) => set("studentProfileIds", v)} /></Field><label className="education-check"><input type="checkbox" checked={!!form.replaceExisting} onChange={(e) => set("replaceExisting", e.target.checked)} /> استبدال الطلاب الحاليين</label></>;
  return null;
}

function Field({ label, children }) { return <div className="education-field"><label>{label}</label>{children}</div>; }
function ActiveField({ form, set }) { return <label className="education-check"><input type="checkbox" checked={!!form.isActive} onChange={(e) => set("isActive", e.target.checked)} /> السجل نشط</label>; }
function Select({ items, value, onChange, required }) { return <select required={required} value={value || ""} onChange={(e) => onChange(e.target.value)}><option value="">اختر</option>{items.map((x) => <option key={itemId(x)} value={itemId(x)}>{itemName(x)}</option>)}</select>; }
function MultiSelect({ items, value, onChange }) { return <select multiple value={value || []} onChange={(e) => onChange(Array.from(e.target.selectedOptions).map((o) => o.value))}>{items.map((x) => <option key={itemId(x)} value={itemId(x)}>{itemName(x)}</option>)}</select>; }

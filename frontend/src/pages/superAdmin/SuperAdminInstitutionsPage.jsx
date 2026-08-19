import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { superAdminApi } from "../../services/api";
import "./superAdmin.css";

const emptyInstitution = {
  name: "",
  type: "School",
  address: "",
  phoneNumber: "",
  email: "",
  logoUrl: "",
  isActive: true,
};

const emptyAdmin = { institutionId: "", userName: "", email: "", password: "" };

const typeLabels = {
  School: "مدرسة",
  Academy: "أكاديمية",
  TrainingCenter: "مركز تدريب",
  Institute: "معهد",
};

function typeLabel(type) {
  return typeLabels[type] || type || "-";
}

export default function SuperAdminInstitutionsPage() {
  const [rows, setRows] = useState([]);
  const [institutionForm, setInstitutionForm] = useState(emptyInstitution);
  const [adminForm, setAdminForm] = useState(emptyAdmin);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [editingInstitution, setEditingInstitution] = useState(null);
  const [editingAdmin, setEditingAdmin] = useState(null);

  async function load() {
    const data = await superAdminApi.getInstitutions();
    setRows(Array.isArray(data) ? data : []);
  }

  useEffect(() => {
    load().catch((err) => setError(err.message || "فشل تحميل المؤسسات"));
  }, []);

  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;

    return rows.filter((item) =>
      [item.name, item.type, item.phoneNumber, item.email, item.address]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(q))
    );
  }, [rows, search]);

  const stats = useMemo(() => {
    const total = rows.length;
    const active = rows.filter((x) => x.isActive).length;
    return { total, active, inactive: total - active };
  }, [rows]);

  async function createInstitution(e) {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      await superAdminApi.createInstitution(institutionForm);
      setInstitutionForm(emptyInstitution);
      setSuccess("تم إنشاء المؤسسة بنجاح");
      await load();
    } catch (err) {
      setError(err.message || "فشل إنشاء المؤسسة");
    } finally {
      setLoading(false);
    }
  }

  async function createAdmin(e) {
    e.preventDefault();
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      await superAdminApi.createInstitutionAdmin(adminForm.institutionId, {
        userName: adminForm.userName,
        email: adminForm.email || null,
        password: adminForm.password,
      });
      setAdminForm(emptyAdmin);
      setSuccess("تم إنشاء مشرف المؤسسة بنجاح");
    } catch (err) {
      setError(err.message || "فشل إنشاء مشرف المؤسسة");
    } finally {
      setLoading(false);
    }
  }

  async function toggleStatus(row) {
    setLoading(true);
    setError("");
    setSuccess("");

    try {
      await superAdminApi.changeInstitutionStatus(row.id, !row.isActive);
      setSuccess(row.isActive ? "تم إيقاف المؤسسة" : "تم تفعيل المؤسسة");
      await load();
    } catch (err) {
      setError(err.message || "فشل تحديث حالة المؤسسة");
    } finally {
      setLoading(false);
    }
  }

  async function updateInstitution(e) {
    e.preventDefault();
    setLoading(true);
    setError("");
    try {
      await superAdminApi.updateInstitution(editingInstitution.id, editingInstitution);
      setEditingInstitution(null);
      setSuccess("تم تحديث بيانات المؤسسة بنجاح");
      await load();
    } catch (err) {
      setError(err.message || "تعذر تحديث بيانات المؤسسة");
    } finally {
      setLoading(false);
    }
  }

  async function updateAdmin(e) {
    e.preventDefault();
    setLoading(true);
    setError("");
    try {
      await superAdminApi.updateInstitutionAdmin(editingAdmin.institutionId, editingAdmin.id, {
        userName: editingAdmin.userName,
        email: editingAdmin.email || null,
        password: editingAdmin.password || null,
        isActive: editingAdmin.isActive,
        mustChangePassword: editingAdmin.mustChangePassword,
      });
      setEditingAdmin(null);
      setSuccess("تم تحديث حساب مدير المؤسسة بنجاح");
      await load();
    } catch (err) {
      setError(err.message || "تعذر تحديث حساب مدير المؤسسة");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="sa-page" dir="rtl">
      <section className="sa-hero institutions">
        <div className="sa-hero-content">
          <span className="sa-eyebrow">إدارة المؤسسات</span>
          <h1>أنشئ بيئات تعليمية مستقلة بسهولة</h1>
          <p>
            أضف المدارس والأكاديميات ومراكز التدريب، ثم اربط مشرفًا لكل مؤسسة لإدارة بياناتها بشكل مستقل وآمن.
          </p>
          <div className="sa-hero-actions">
            <Link className="sa-btn soft" to="/super-admin">لوحة المشرف العام</Link>
          </div>
        </div>

        <div className="sa-hero-panel mini-stats">
          <div><span>الإجمالي</span><strong>{stats.total}</strong></div>
          <div><span>النشطة</span><strong>{stats.active}</strong></div>
          <div><span>الموقوفة</span><strong>{stats.inactive}</strong></div>
        </div>
      </section>

      {error && <div className="sa-alert error">{error}</div>}
      {success && <div className="sa-alert success">{success}</div>}

      <div className="sa-forms-grid">
        <form className="sa-card sa-form" onSubmit={createInstitution}>
          <div className="sa-card-head compact">
            <div>
              <h2>إضافة مؤسسة جديدة</h2>
              <p>بيانات المؤسسة الأساسية داخل المنصة.</p>
            </div>
          </div>

          <label className="sa-field">
            <span>اسم المؤسسة</span>
            <input
              placeholder="مثال: أكاديمية المستقبل"
              value={institutionForm.name}
              onChange={(e) => setInstitutionForm({ ...institutionForm, name: e.target.value })}
              required
            />
          </label>

          <label className="sa-field">
            <span>نوع المؤسسة</span>
            <select
              value={institutionForm.type}
              onChange={(e) => setInstitutionForm({ ...institutionForm, type: e.target.value })}
            >
              <option value="School">مدرسة</option>
              <option value="Academy">أكاديمية</option>
              <option value="TrainingCenter">مركز تدريب</option>
              <option value="Institute">معهد</option>
            </select>
          </label>

          <label className="sa-field">
            <span>العنوان</span>
            <input
              placeholder="المدينة / الحي"
              value={institutionForm.address}
              onChange={(e) => setInstitutionForm({ ...institutionForm, address: e.target.value })}
            />
          </label>

          <div className="sa-field-row">
            <label className="sa-field">
              <span>الجوال</span>
              <input
                placeholder="05xxxxxxxx"
                value={institutionForm.phoneNumber}
                onChange={(e) => setInstitutionForm({ ...institutionForm, phoneNumber: e.target.value })}
              />
            </label>

            <label className="sa-field">
              <span>البريد الإلكتروني</span>
              <input
                type="email"
                placeholder="name@example.com"
                value={institutionForm.email}
                onChange={(e) => setInstitutionForm({ ...institutionForm, email: e.target.value })}
              />
            </label>
          </div>

          <button className="sa-btn primary full" type="submit" disabled={loading}>
            {loading ? "جاري الحفظ..." : "حفظ المؤسسة"}
          </button>
        </form>

        <form className="sa-card sa-form" onSubmit={createAdmin}>
          <div className="sa-card-head compact">
            <div>
              <h2>إضافة مشرف مؤسسة</h2>
              <p>حساب مسؤول لإدارة مؤسسة واحدة فقط.</p>
            </div>
          </div>

          <label className="sa-field">
            <span>المؤسسة</span>
            <select
              value={adminForm.institutionId}
              onChange={(e) => setAdminForm({ ...adminForm, institutionId: e.target.value })}
              required
            >
              <option value="">اختر المؤسسة</option>
              {rows.map((item) => (
                <option key={item.id} value={item.id}>{item.name}</option>
              ))}
            </select>
          </label>

          <label className="sa-field">
            <span>اسم المستخدم</span>
            <input
              placeholder="institution.admin"
              value={adminForm.userName}
              onChange={(e) => setAdminForm({ ...adminForm, userName: e.target.value })}
              required
            />
          </label>

          <label className="sa-field">
            <span>البريد الإلكتروني</span>
            <input
              type="email"
              placeholder="admin@example.com"
              value={adminForm.email}
              onChange={(e) => setAdminForm({ ...adminForm, email: e.target.value })}
            />
          </label>

          <label className="sa-field">
            <span>كلمة المرور</span>
            <input
              type="password"
              placeholder="كلمة مرور قوية"
              value={adminForm.password}
              onChange={(e) => setAdminForm({ ...adminForm, password: e.target.value })}
              required
            />
          </label>

          <button className="sa-btn primary full" type="submit" disabled={loading}>
            {loading ? "جاري الإنشاء..." : "إنشاء مشرف المؤسسة"}
          </button>
        </form>
      </div>

      <section className="sa-card">
        <div className="sa-card-head">
          <div>
            <h2>قائمة المؤسسات</h2>
            <p>إدارة الحالة ومراجعة بيانات المؤسسات المسجلة.</p>
          </div>
          <div className="sa-search-box">
            <input
              placeholder="بحث باسم المؤسسة أو البريد أو النوع..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </div>

        {filteredRows.length === 0 ? (
          <div className="sa-empty">لا توجد مؤسسات مطابقة للبحث.</div>
        ) : (
          <div className="sa-institution-grid">
            {filteredRows.map((item) => (
              <article className="sa-institution-card" key={item.id}>
                <div className="sa-institution-top">
                  <div className="sa-avatar">{(item.name || "م").slice(0, 1)}</div>
                  <div>
                    <h3>{item.name}</h3>
                    <p>{typeLabel(item.type)}</p>
                  </div>
                  <span className={item.isActive ? "sa-status active" : "sa-status inactive"}>
                    {item.isActive ? "نشطة" : "موقوفة"}
                  </span>
                </div>

                <div className="sa-info-list">
                  <div><span>الجوال</span><strong>{item.phoneNumber || "-"}</strong></div>
                  <div><span>البريد</span><strong>{item.email || "-"}</strong></div>
                  <div><span>العنوان</span><strong>{item.address || "-"}</strong></div>
                </div>

                <div className="sa-admins-block">
                  <div className="sa-admins-title"><span>مديرو المؤسسة</span><strong>{item.admins?.length || 0}</strong></div>
                  {item.admins?.length ? item.admins.map((admin) => (
                    <div className="sa-admin-row" key={admin.id}>
                      <div><strong>{admin.userName}</strong><small>{admin.email || "لا يوجد بريد إلكتروني"}</small></div>
                      <span className={admin.isActive ? "sa-status active" : "sa-status inactive"}>{admin.isActive ? "فعال" : "موقوف"}</span>
                      <button type="button" className="sa-mini-btn" onClick={() => setEditingAdmin({
                        ...admin, institutionId: item.id, institutionName: item.name,
                        email: admin.email || "", password: "", mustChangePassword: true,
                      })}>تعديل</button>
                    </div>
                  )) : <div className="sa-empty small">لم يتم إنشاء مدير لهذه المؤسسة.</div>}
                </div>

                <div className="sa-card-actions">
                  <button className="sa-btn soft" type="button" onClick={() => setEditingInstitution({ ...item })}>تعديل البيانات</button>
                  <button className="sa-btn ghost" type="button" disabled={loading} onClick={() => toggleStatus(item)}>
                    {item.isActive ? "إيقاف المؤسسة" : "تفعيل المؤسسة"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {editingInstitution && (
        <div className="sa-modal-backdrop" role="presentation" onMouseDown={() => setEditingInstitution(null)}>
          <form className="sa-modal sa-form" onSubmit={updateInstitution} onMouseDown={(e) => e.stopPropagation()}>
            <div className="sa-modal-head"><div><h2>تعديل المؤسسة</h2><p>{editingInstitution.name}</p></div><button type="button" onClick={() => setEditingInstitution(null)}>×</button></div>
            <label className="sa-field"><span>اسم المؤسسة</span><input required value={editingInstitution.name} onChange={(e) => setEditingInstitution({ ...editingInstitution, name: e.target.value })} /></label>
            <label className="sa-field"><span>نوع المؤسسة</span><select value={editingInstitution.type || "School"} onChange={(e) => setEditingInstitution({ ...editingInstitution, type: e.target.value })}><option value="School">مدرسة</option><option value="Academy">أكاديمية</option><option value="TrainingCenter">مركز تدريب</option><option value="Institute">معهد</option></select></label>
            <label className="sa-field"><span>العنوان</span><input value={editingInstitution.address || ""} onChange={(e) => setEditingInstitution({ ...editingInstitution, address: e.target.value })} /></label>
            <div className="sa-field-row">
              <label className="sa-field"><span>الجوال</span><input value={editingInstitution.phoneNumber || ""} onChange={(e) => setEditingInstitution({ ...editingInstitution, phoneNumber: e.target.value })} /></label>
              <label className="sa-field"><span>البريد الإلكتروني</span><input type="email" value={editingInstitution.email || ""} onChange={(e) => setEditingInstitution({ ...editingInstitution, email: e.target.value })} /></label>
            </div>
            <label className="sa-check"><input type="checkbox" checked={editingInstitution.isActive} onChange={(e) => setEditingInstitution({ ...editingInstitution, isActive: e.target.checked })} /><span>المؤسسة فعالة</span></label>
            <button className="sa-btn primary full" disabled={loading}>{loading ? "جاري الحفظ..." : "حفظ التعديلات"}</button>
          </form>
        </div>
      )}

      {editingAdmin && (
        <div className="sa-modal-backdrop" role="presentation" onMouseDown={() => setEditingAdmin(null)}>
          <form className="sa-modal sa-form" onSubmit={updateAdmin} onMouseDown={(e) => e.stopPropagation()}>
            <div className="sa-modal-head"><div><h2>تعديل مدير المؤسسة</h2><p>{editingAdmin.institutionName}</p></div><button type="button" onClick={() => setEditingAdmin(null)}>×</button></div>
            <label className="sa-field"><span>اسم المستخدم</span><input required value={editingAdmin.userName} onChange={(e) => setEditingAdmin({ ...editingAdmin, userName: e.target.value })} /></label>
            <label className="sa-field"><span>البريد الإلكتروني</span><input type="email" value={editingAdmin.email} onChange={(e) => setEditingAdmin({ ...editingAdmin, email: e.target.value })} /></label>
            <label className="sa-field"><span>كلمة مرور جديدة (اختياري)</span><input type="password" minLength="8" value={editingAdmin.password} onChange={(e) => setEditingAdmin({ ...editingAdmin, password: e.target.value })} placeholder="اتركها فارغة للاحتفاظ بالكلمة الحالية" /></label>
            <label className="sa-check"><input type="checkbox" checked={editingAdmin.isActive} onChange={(e) => setEditingAdmin({ ...editingAdmin, isActive: e.target.checked })} /><span>الحساب فعال</span></label>
            {editingAdmin.password && <label className="sa-check"><input type="checkbox" checked={editingAdmin.mustChangePassword} onChange={(e) => setEditingAdmin({ ...editingAdmin, mustChangePassword: e.target.checked })} /><span>إلزام المدير بتغيير كلمة المرور عند أول دخول</span></label>}
            <button className="sa-btn primary full" disabled={loading}>{loading ? "جاري الحفظ..." : "حفظ بيانات المدير"}</button>
          </form>
        </div>
      )}
    </div>
  );
}

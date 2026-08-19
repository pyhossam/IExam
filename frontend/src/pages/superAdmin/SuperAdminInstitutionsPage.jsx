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

const emptyAdmin = { institutionId: "", userName: "", password: "" };

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

                <button className="sa-btn ghost full" type="button" disabled={loading} onClick={() => toggleStatus(item)}>
                  {item.isActive ? "إيقاف المؤسسة" : "تفعيل المؤسسة"}
                </button>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

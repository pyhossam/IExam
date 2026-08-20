import { useEffect, useMemo, useState } from "react";
import { getAssignedCourses, getCourseBloomReport, getCourseCloReport, getReadableErrorMessage } from "../../services/api";
import "./educationReports.css";

const percent = (value) => Math.max(0, Math.min(100, Number(value || 0)));

function downloadCsv(course, rows) {
  const header = ["CLO", "Description", "Questions", "Answers", "Correct", "Attainment %", "Target %", "Status"];
  const data = rows.map((row) => [row.code, row.description, row.questions, row.answered, row.correct, row.attainmentPercentage, row.targetPercentage, row.achieved ? "Achieved" : "Needs improvement"]);
  const csv = [header, ...data].map((line) => line.map((cell) => `"${String(cell ?? "").replaceAll('"', '""')}"`).join(",")).join("\n");
  const blob = new Blob(["\ufeff", csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `CLO-analysis-${course?.code || "course"}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

export default function EducationReportsPage() {
  const [courses, setCourses] = useState([]);
  const [courseId, setCourseId] = useState("");
  const [rows, setRows] = useState([]);
  const [bloomRows, setBloomRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    getAssignedCourses().then((data) => {
      const list = Array.isArray(data) ? data : [];
      setCourses(list);
      if (list[0]) setCourseId(list[0].id);
    }).catch((err) => setError(getReadableErrorMessage(err))).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!courseId) return setRows([]);
    setLoading(true);
    setError("");
    Promise.all([getCourseCloReport(courseId), getCourseBloomReport(courseId)]).then(([clo,bloom]) => { setRows(Array.isArray(clo) ? clo : []); setBloomRows(Array.isArray(bloom) ? bloom : []); })
      .catch((err) => setError(getReadableErrorMessage(err))).finally(() => setLoading(false));
  }, [courseId]);

  const course = courses.find((item) => String(item.id) === String(courseId));
  const summary = useMemo(() => {
    const answered = rows.reduce((sum, row) => sum + Number(row.answered || 0), 0);
    const correct = rows.reduce((sum, row) => sum + Number(row.correct || 0), 0);
    const achieved = rows.filter((row) => row.achieved).length;
    return {
      attainment: answered ? Math.round((correct * 1000) / answered) / 10 : 0,
      coverage: rows.reduce((sum, row) => sum + Number(row.questions || 0), 0),
      achieved,
      needsSupport: rows.length - achieved,
    };
  }, [rows]);

  return (
    <div className="reports-page">
      <header className="reports-hero">
        <div>
          <span className="reports-eyebrow">مركز ذكاء القياس</span>
          <h1>تقارير الاختبارات وتحليل مخرجات التعلم</h1>
          <p>قراءة واضحة لأداء الطلاب، مستوى تحقق كل CLO، والفجوة عن النسبة المستهدفة لاتخاذ قرار تعليمي أدق.</p>
        </div>
        <div className="reports-hero-orbit" aria-hidden="true"><i /><i /><i /><strong>CLO</strong></div>
      </header>

      <section className="reports-toolbar">
        <div><label htmlFor="report-course">المقرر</label><select id="report-course" value={courseId} onChange={(e) => setCourseId(e.target.value)}><option value="">اختر المقرر</option>{courses.map((item) => <option key={item.id} value={item.id}>{item.name} ({item.code})</option>)}</select></div>
        <div className="reports-actions"><button type="button" onClick={() => downloadCsv(course, rows)} disabled={!rows.length}>تصدير Excel / CSV</button><button className="secondary" type="button" onClick={() => window.print()} disabled={!rows.length}>طباعة / حفظ PDF</button></div>
      </section>

      <section className="reports-panel">
        <div className="reports-panel-head"><div><h2>تحليل النتائج حسب مستويات Bloom</h2><p>يوضح عدد الأسئلة والإجابات ونسبة الإتقان في كل مستوى معرفي.</p></div></div>
        {!bloomRows.length ? <div className="reports-empty">لا توجد إجابات مصنفة حسب Bloom حتى الآن.</div> : <div className="table-wrap"><table><thead><tr><th>المستوى</th><th>عدد الأسئلة</th><th>الإجابات</th><th>الصحيحة</th><th>نسبة الإتقان</th></tr></thead><tbody>{bloomRows.map(row=><tr key={row.cognitiveLevel}><td>{row.cognitiveLevel}</td><td>{row.questions}</td><td>{row.answered}</td><td>{row.correct}</td><td>{row.attainmentPercentage}%</td></tr>)}</tbody></table></div>}
      </section>

      {error && <div className="alert error">{error}</div>}

      <section className="reports-kpis">
        <article><span>التحقق العام</span><strong>{summary.attainment}%</strong><small>من إجمالي الإجابات المقاسة</small></article>
        <article><span>تغطية الأسئلة</span><strong>{summary.coverage}</strong><small>سؤال مرتبط بمخرجات التعلم</small></article>
        <article className="success"><span>مخرجات متحققة</span><strong>{summary.achieved}</strong><small>بلغت الهدف أو تجاوزته</small></article>
        <article className="warning"><span>تحتاج تحسينًا</span><strong>{summary.needsSupport}</strong><small>أقل من النسبة المستهدفة</small></article>
      </section>

      <section className="reports-panel">
        <div className="reports-panel-head"><div><h2>خريطة تحقق CLO</h2><p>{course ? `${course.name} · ${course.code}` : "اختر مقررًا لعرض التحليل"}</p></div><span className="reports-live-dot">بيانات فعلية</span></div>
        {loading ? <div className="reports-empty">جاري تحليل النتائج…</div> : !rows.length ? <div className="reports-empty">لا توجد نتائج CLO لهذا المقرر حتى الآن. اربط أسئلة الاختبار بالمخرجات ثم نفّذ الاختبار لظهور التحليل.</div> : <div className="clo-analysis-list">{rows.map((row) => {
          const score = percent(row.attainmentPercentage);
          const target = percent(row.targetPercentage);
          const gap = Math.round((score - target) * 100) / 100;
          return <article className={`clo-analysis-card ${row.achieved ? "achieved" : "at-risk"}`} key={row.id}>
            <div className="clo-code"><strong>{row.code}</strong><span>{row.achieved ? "متحقق" : "يحتاج تدخلًا"}</span></div>
            <div className="clo-main"><h3>{row.description}</h3><div className="clo-progress"><div style={{ width: `${score}%` }} /><i style={{ insetInlineStart: `${target}%` }} /></div><div className="clo-scale"><span>التحقق {score}%</span><span>الهدف {target}%</span><span className={gap >= 0 ? "positive" : "negative"}>الفجوة {gap > 0 ? "+" : ""}{gap}%</span></div></div>
            <div className="clo-metrics"><span><b>{row.questions}</b> أسئلة</span><span><b>{row.answered}</b> إجابات</span><span><b>{row.correct}</b> صحيحة</span></div>
          </article>;
        })}</div>}
      </section>
    </div>
  );
}

export default function SectionCard({ title, subtitle, children }) {
  return (
    <section className="section-card">
      <div className="section-head">
        <div>
          <h3>{title}</h3>
          {subtitle ? <p>{subtitle}</p> : null}
        </div>
      </div>
      {children}
    </section>
  );
}

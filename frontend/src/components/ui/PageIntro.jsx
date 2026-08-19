export default function PageIntro({ title, description, actions }) {
  return (
    <div className="page-intro">
      <div>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      {actions ? <div className="action-row">{actions}</div> : null}
    </div>
  );
}

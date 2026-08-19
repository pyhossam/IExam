import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { getAttemptDetails, getReadableErrorMessage, toAbsoluteFileUrl } from "../../services/api";
import { formatSaudiDateTime } from "../../utils/dateTime";

function answerText(answer, key) {
  if (!key) return "لم تتم الإجابة";
  const choice = answer.choices?.find((item) => item.originalKey === key);
  return choice ? `${choice.displayLabel} — ${choice.text}` : key;
}

export default function AttemptReviewPage() {
  const { examId, attemptId } = useParams();
  const [attempt, setAttempt] = useState(null);
  const [error, setError] = useState("");
  const [printInfo, setPrintInfo] = useState(null);

  useEffect(() => {
    getAttemptDetails(attemptId)
      .then(setAttempt)
      .catch((err) => setError(getReadableErrorMessage(err, "تعذر تحميل ورقة الطالب")));
  }, [attemptId]);

  function printPaper() {
    const info = {
      user: localStorage.getItem("userName") || "مستخدم النظام",
      time: new Date(),
    };
    setPrintInfo(info);
    document.title = `${attempt.examCode}-${attempt.studentCode}-submitted-paper`;
    setTimeout(() => window.print(), 80);
  }

  if (error) return <div className="standalone-page"><div className="alert error">{error}</div></div>;
  if (!attempt) return <div className="standalone-page"><div className="section-card">جاري تحميل ورقة الطالب...</div></div>;

  return (
    <div className="attempt-review-page" dir="rtl">
      <div className="attempt-print-toolbar">
        <Link className="ghost-btn" to={`/admin/exams/${examId}`}>العودة إلى الاختبار</Link>
        <button className="primary-btn" type="button" onClick={printPaper}>طباعة ورقة الطالب</button>
      </div>

      <main className="attempt-paper">
        {printInfo && (
          <div className="attempt-watermark" aria-hidden="true">
            <strong>{attempt.studentName}</strong>
            <span>طبع بواسطة: {printInfo.user}</span>
            <span>{formatSaudiDateTime(printInfo.time)}</span>
          </div>
        )}

        <header className="attempt-paper-header">
          <div><span>الاختبار</span><h1>{attempt.examTitle}</h1><b>{attempt.examCode}</b></div>
          <div className="attempt-score"><span>النتيجة</span><strong>{attempt.score} / {attempt.totalQuestions}</strong><b>{attempt.percentage}%</b></div>
        </header>

        <section className="attempt-student-meta">
          <div><span>اسم الطالب</span><strong>{attempt.studentName}</strong></div>
          <div><span>كود الطالب</span><strong>{attempt.studentCode}</strong></div>
          <div><span>بدء المحاولة</span><strong>{formatSaudiDateTime(attempt.startedAtUtc)}</strong></div>
          <div><span>وقت التسليم</span><strong>{formatSaudiDateTime(attempt.submittedAtUtc)}</strong></div>
        </section>

        <div className="attempt-answer-list">
          {attempt.answers.map((answer, index) => (
            <article className={`attempt-answer ${answer.isCorrect ? "correct" : "wrong"}`} key={`${answer.questionId}-${index}`}>
              <div className="attempt-answer-head">
                <h2>{answer.displayOrder || index + 1}. {answer.questionText}</h2>
                <span>{answer.isCorrect ? "إجابة صحيحة" : "إجابة غير صحيحة"}</span>
              </div>
              {answer.questionImageUrl && <img src={toAbsoluteFileUrl(answer.questionImageUrl)} alt="صورة السؤال" />}
              <div className="attempt-choices">
                {answer.choices?.map((choice) => (
                  <div key={choice.originalKey} className={`${choice.originalKey === answer.selectedAnswer ? "selected" : ""} ${choice.originalKey === answer.correctAnswer ? "answer-key" : ""}`}>
                    <b>{choice.displayLabel}</b><span>{choice.text}</span>
                    {choice.imageUrl && <img src={toAbsoluteFileUrl(choice.imageUrl)} alt="صورة الاختيار" />}
                  </div>
                ))}
              </div>
              <div className="attempt-answer-summary">
                <p><b>إجابة الطالب:</b> {answerText(answer, answer.selectedAnswer)}</p>
                <p><b>الإجابة الصحيحة:</b> {answerText(answer, answer.correctAnswer)}</p>
                {answer.explanation && <p><b>التفسير:</b> {answer.explanation}</p>}
              </div>
            </article>
          ))}
        </div>

        {printInfo && <footer className="attempt-print-footer">نسخة مراجعة — {attempt.studentName} — طبع بواسطة {printInfo.user} في {formatSaudiDateTime(printInfo.time)}</footer>}
      </main>
    </div>
  );
}

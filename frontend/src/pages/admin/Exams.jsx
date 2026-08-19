
import { useState } from "react";
import { apiJson } from "../../services/api";

export default function Exams() {
  const [topic, setTopic] = useState("");

  async function create() {
    await apiJson("/exams/ai", "POST", {
      topic,
      exam_code: "AI-" + Date.now(),
      bank_question_count: 20,
      exam_question_count: 10
    });
    alert("Created");
  }

  return (
    <div>
      <h1>Exams</h1>

      <div className="card">
        <h3>Create AI Exam</h3>
        <input value={topic} onChange={e=>setTopic(e.target.value)} />
        <button onClick={create}>Create</button>
      </div>
    </div>
  );
}

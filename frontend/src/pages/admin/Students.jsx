
import { useState } from "react";
import { apiJson } from "../../services/api";

export default function Students() {
  const [name, setName] = useState("");

  async function addStudent(e) {
    e.preventDefault();
    await apiJson("/admin/students", "POST", {
      fullName: name,
      studentCode: Math.random().toString(36).substring(2)
    });
    alert("Added");
  }

  return (
    <div>
      <h1>Students</h1>

      <div className="card">
        <h3>Add Student</h3>
        <form onSubmit={addStudent}>
          <input value={name} onChange={e=>setName(e.target.value)} />
          <button>Add</button>
        </form>
      </div>

      <div className="card">
        <h3>Upload Excel</h3>
        <input type="file" />
      </div>
    </div>
  );
}

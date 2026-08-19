
import { useEffect, useState } from "react";
import Card from "../../components/ui/Card";
import { apiRequest } from "../../services/api";

export default function Dashboard(){
  const [stats,setStats]=useState({});

  useEffect(()=>{
    apiRequest("/admin/dashboard").then(r=>setStats(r.stats))
  },[])

  return(
    <div>
      <h1>Dashboard</h1>

      <div className="grid">
        <Card>Students: {stats.students}</Card>
        <Card>Exams: {stats.exams}</Card>
        <Card>Parents: {stats.parents}</Card>
        <Card>Attempts: {stats.attempts}</Card>
      </div>
    </div>
  )
}

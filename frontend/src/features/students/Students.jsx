
import { useEffect, useState } from "react";
import Table from "../../components/ui/Table";
import { apiRequest } from "../../services/api";

export default function Students(){
  const [data,setData]=useState([]);

  useEffect(()=>{
    apiRequest("/admin/students").then(setData)
  },[])

  return(
    <div>
      <h1>Students</h1>
      <Table data={data}/>
    </div>
  )
}


export default function Table({data=[]}){
  return(
    <table className="table">
      <thead>
        <tr>
          {Object.keys(data[0]||{}).map(k=><th key={k}>{k}</th>)}
        </tr>
      </thead>
      <tbody>
        {data.map((row,i)=>(
          <tr key={i}>
            {Object.values(row).map((v,i)=><td key={i}>{v}</td>)}
          </tr>
        ))}
      </tbody>
    </table>
  )
}

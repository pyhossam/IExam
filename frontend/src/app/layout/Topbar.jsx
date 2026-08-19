import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { clearToken, getDashboardOverview, getUserName } from "../../services/api";
import { useLanguage } from "../i18n/LanguageContext";

export default function Topbar({onMenuClick}) {
  const location=useLocation(), navigate=useNavigate(), userName=getUserName();
  const [institutionName,setInstitutionName]=useState(""); const {t,toggleLanguage}=useLanguage();
  useEffect(()=>{if(!location.pathname.startsWith("/admin")){setInstitutionName("");return}let active=true;getDashboardOverview().then(x=>{if(active)setInstitutionName(x?.institutionName||"")}).catch(()=>{if(active)setInstitutionName("")});return()=>{active=false}},[location.pathname]);
  function logout(){if(!window.confirm(t("logoutConfirm")))return;clearToken();navigate("/login",{replace:true})}
  return <div className="topbar"><div className="topbar-main"><button type="button" className="mobile-menu-btn" onClick={onMenuClick}>☰</button><div><span className="topbar-badge">{t("adminPanel")}</span><h1 className="topbar-title">{t("dashboard")}{institutionName&&<span className="topbar-institution-name"> - {institutionName}</span>}</h1></div></div><div className="topbar-actions"><button className="ghost-btn language-toggle" type="button" onClick={toggleLanguage}>{t("language")}</button><div className="user-chip">👤 {userName||t("user")}</div><button className="logout-btn" onClick={logout}>{t("logout")}</button></div></div>;
}

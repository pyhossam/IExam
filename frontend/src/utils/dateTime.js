import { toArabicDigits } from "./arabicNumbers";

export const SAUDI_TIME_ZONE = "Asia/Riyadh";

export function formatSaudiDateTime(value) {
  if (!value) return "-";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return "-";

  return toArabicDigits(
    new Intl.DateTimeFormat("ar-SA", {
      timeZone: SAUDI_TIME_ZONE,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: true,
    }).format(date)
  );
}

export function formatSaudiDate(value) {
  if (!value) return "-";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return "-";

  return toArabicDigits(
    new Intl.DateTimeFormat("ar-SA", {
      timeZone: SAUDI_TIME_ZONE,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    }).format(date)
  );
}

export function formatSaudiTime(value) {
  if (!value) return "-";

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) return "-";

  return toArabicDigits(
    new Intl.DateTimeFormat("ar-SA", {
      timeZone: SAUDI_TIME_ZONE,
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: true,
    }).format(date)
  );
}

export function toArabicDigits(value) {
  if (value === null || value === undefined) return "";

  return String(value).replace(/\d/g, (digit) => "٠١٢٣٤٥٦٧٨٩"[digit]);
}

export function toArabicNumber(value) {
  if (value === null || value === undefined || value === "") return "";

  const number = Number(value);

  if (Number.isNaN(number)) {
    return toArabicDigits(value);
  }

  return new Intl.NumberFormat("ar-EG", {
    useGrouping: true,
  }).format(number);
}

export function toArabicPercent(value) {
  if (value === null || value === undefined || value === "") return "٠٪";
  return `${toArabicNumber(value)}٪`;
}

export function toArabicTimePart(value) {
  return toArabicDigits(String(value ?? 0).padStart(2, "0"));
}

export function toArabicDateTime(value) {
  if (!value) return "-";

  try {
    return toArabicDigits(
      new Date(value).toLocaleString("ar-SA", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
      })
    );
  } catch {
    return toArabicDigits(value);
  }
}

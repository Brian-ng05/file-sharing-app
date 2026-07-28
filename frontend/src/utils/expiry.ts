export interface ExpiryDisplay {
  relativeText: string;
  localTimeText: string | null;
  expired: boolean;
  active: boolean;
}

const VIETNAM_TIMEZONE = "Asia/Ho_Chi_Minh";

const vietnamFormatter = new Intl.DateTimeFormat("en-CA", {
  timeZone: VIETNAM_TIMEZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
  hour12: false,
});

function formatVietnamTime(date: Date): string {
  // Intl.DateTimeFormat with en-CA locale gives "YYYY-MM-DD HH:mm:ss" format
  const parts = vietnamFormatter.formatToParts(date);
  const get = (type: string) => parts.find((p) => p.type === type)?.value ?? "00";
  const y = get("year");
  const m = get("month");
  const d = get("day");
  const h = get("hour");
  const min = get("minute");
  const s = get("second");
  return `${y}-${m}-${d} ${h}:${min}:${s} ICT`;
}

export function formatExpiryDisplay(expiresAt?: string, nowMs?: number): ExpiryDisplay {
  const currentMs = nowMs ?? Date.now();

  if (!expiresAt) {
    return { relativeText: "Active", localTimeText: null, expired: false, active: true };
  }

  const expiryMs = new Date(expiresAt).getTime();
  const diffMs = expiryMs - currentMs;
  const localTimeText = formatVietnamTime(new Date(expiryMs));

  if (diffMs <= 0) {
    return { relativeText: "Expired", localTimeText, expired: true, active: false };
  }

  const totalMinutes = Math.floor(diffMs / (1000 * 60));
  const totalHours = Math.floor(diffMs / (1000 * 60 * 60));
  const totalDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  let relativeText: string;
  if (totalDays >= 1) {
    relativeText = `${totalDays} ${totalDays === 1 ? "day" : "days"} left`;
  } else if (totalHours >= 1) {
    relativeText = `${totalHours} ${totalHours === 1 ? "hour" : "hours"} left`;
  } else if (totalMinutes >= 1) {
    relativeText = `${totalMinutes} ${totalMinutes === 1 ? "minute" : "minutes"} left`;
  } else {
    relativeText = "Less than 1 minute left";
  }

  return { relativeText, localTimeText, expired: false, active: false };
}

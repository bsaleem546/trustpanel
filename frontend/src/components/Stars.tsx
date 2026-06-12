import { Star } from "lucide-react";

export function Stars({ value, size = 14 }: { value: number; size?: number }) {
  return (
    <div className="flex items-center gap-0.5">
      {Array.from({ length: 5 }).map((_, i) => (
        <Star
          key={i}
          size={size}
          strokeWidth={1.5}
          className={i < value ? "" : "opacity-25"}
          style={{ color: i < value ? "var(--warning)" : "var(--subtle)", fill: i < value ? "var(--warning)" : "transparent" }}
        />
      ))}
    </div>
  );
}

export function Avatar({ name, color, size = 36 }: { name: string; color: string; size?: number }) {
  const initials = name
    .split(" ")
    .map((p) => p[0])
    .slice(0, 2)
    .join("");
  return (
    <div
      className="rounded-full flex items-center justify-center font-medium shrink-0"
      style={{
        width: size,
        height: size,
        background: `${color}22`,
        color,
        fontSize: size * 0.4,
        border: `1px solid ${color}44`,
      }}
    >
      {initials}
    </div>
  );
}

export function Pill({
  tone = "neutral",
  children,
}: {
  tone?: "neutral" | "success" | "warning" | "danger" | "info" | "primary";
  children: React.ReactNode;
}) {
  const map: Record<string, { bg: string; fg: string }> = {
    neutral: { bg: "rgba(255,255,255,0.06)", fg: "var(--muted-foreground)" },
    success: { bg: "rgba(52,211,153,0.12)", fg: "var(--success)" },
    warning: { bg: "rgba(251,191,36,0.12)", fg: "var(--warning)" },
    danger: { bg: "rgba(248,113,113,0.12)", fg: "var(--danger)" },
    info: { bg: "rgba(96,165,250,0.12)", fg: "var(--info)" },
    primary: { bg: "rgba(124,106,247,0.14)", fg: "var(--primary-light)" },
  };
  const s = map[tone];
  return (
    <span className="tp-pill" style={{ background: s.bg, color: s.fg }}>
      {children}
    </span>
  );
}

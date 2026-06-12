export function Logo({ size = 28 }: { size?: number }) {
  return (
    <div className="flex items-center gap-2">
      <div
        className="rounded-lg flex items-center justify-center"
        style={{
          width: size,
          height: size,
          background: "var(--primary)",
          color: "white",
          fontWeight: 700,
          fontSize: size * 0.5,
        }}
      >
        T
      </div>
      <span className="font-semibold tracking-tight" style={{ fontSize: 16 }}>
        TrustPanel
      </span>
    </div>
  );
}

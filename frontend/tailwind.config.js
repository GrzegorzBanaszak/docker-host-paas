/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#0f172a",
        mist: "#e2e8f0",
        sand: "#f8fafc",
        coral: "#d97706",
        steel: "#334155",
        mint: "#10b981",
        rose: "#ef4444",
        sky: "#0ea5e9"
      },
      fontFamily: {
        sans: ["'Segoe UI'", "system-ui", "sans-serif"],
        mono: ["'Cascadia Code'", "'Fira Code'", "monospace"]
      },
      boxShadow: {
        panel: "0 20px 60px rgba(15, 23, 42, 0.12)"
      }
    }
  },
  plugins: []
};

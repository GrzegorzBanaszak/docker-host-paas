/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        ink: "#0b1c30",
        mist: "#eaf1ff",
        sand: "#f8f9ff",
        coral: "#fb7185",
        steel: "#45464d",
        mint: "#10b981",
        rose: "#ba1a1a",
        sky: "#39b8fd",
        outline: "#c6c6cd",
        surface: "#f8f9ff",
        "surface-low": "#eff4ff",
        "surface-high": "#dce9ff",
        variant: "#d3e4fe",
        secondary: "#006591"
      },
      fontFamily: {
        sans: ["Inter", "system-ui", "sans-serif"],
        mono: ["JetBrains Mono", "monospace"]
      },
      boxShadow: {
        panel: "0 4px 12px rgba(15, 23, 42, 0.08)"
      }
    }
  },
  plugins: []
};

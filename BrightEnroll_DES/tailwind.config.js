/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./wwwroot/**/*.{html,js}",
    "./Components/**/*.{razor,cshtml}",
    "./Components/**/*.razor.css",
    "./Platforms/**/*.{razor,cshtml}",
    "./*.razor",
    "./*.cshtml"
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}


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
    extend: {
      fontFamily: {
        'sans': ['Poppins', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
      },
    },
  },
  plugins: [],
}


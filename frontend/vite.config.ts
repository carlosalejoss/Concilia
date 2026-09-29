import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // In development, forward API calls to the .NET API (http profile in
    // launchSettings.json), so the browser sees a single origin and no CORS
    // setup is needed in the API.
    proxy: {
      '/health': 'http://localhost:5292',
      '/projects': 'http://localhost:5292',
      '/runs': 'http://localhost:5292',
      '/jobs': 'http://localhost:5292',
    },
  },
})

import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:7001',
        changeOrigin: true,
        secure: false
      },
      // Proxy static file uploads (photos, signatures, documents) through Vite
      // so the browser loads them from the same origin — avoids CORS on localhost
      '/uploads': {
        target: 'http://localhost:7001',
        changeOrigin: true,
        secure: false
      }
    }
  }
})

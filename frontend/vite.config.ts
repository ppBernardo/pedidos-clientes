import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// O frontend sempre chama /api. Em desenvolvimento, este proxy repassa para a
// API (padrão: http://localhost:8080) removendo o prefixo; no Docker, quem faz
// isso é o nginx (nginx.conf). Assim não há CORS em nenhum ambiente.
const apiUrl = process.env.API_URL ?? 'http://localhost:8080'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: apiUrl,
        changeOrigin: true,
        rewrite: (caminho) => caminho.replace(/^\/api/, ''),
      },
    },
  },
})

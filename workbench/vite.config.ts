import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      // PTY endpoints - high frequency, suppress logging
      '/api/pty': {
        target: 'http://localhost:5218',
        changeOrigin: true,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        configure: (proxy: any) => {
          // Suppress all logging for PTY requests (too noisy - fires on every keystroke)
          proxy.on('proxyReq', () => {})
        },
      },
      // All other API endpoints
      '/api': {
        target: 'http://localhost:5218',
        changeOrigin: true,
      },
    },
  },
})

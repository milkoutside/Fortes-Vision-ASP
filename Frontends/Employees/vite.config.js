import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueDevTools from 'vite-plugin-vue-devtools';

const API_PROXY_TARGET = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5001';

const createProxyConfig = () => ({
  '/api': {
    target: API_PROXY_TARGET,
    changeOrigin: true,
    secure: false,
    rewrite: (path) => path.replace(/^\/api/, ''),
  },
});

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5174,           // ← добавил порт
    host: true,
    proxy: createProxyConfig(),
  },
  preview: {
    port: 5174,           // ← при vite preview тоже этот порт
    proxy: createProxyConfig(),
  },
});

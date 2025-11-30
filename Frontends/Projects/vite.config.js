import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueDevTools from 'vite-plugin-vue-devtools';

const API_PROXY_TARGET = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5002';

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
    proxy: createProxyConfig(),
  },
  preview: {
    proxy: createProxyConfig(),
  },
});


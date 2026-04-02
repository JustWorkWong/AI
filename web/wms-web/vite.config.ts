import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "node"
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5216",
        changeOrigin: true
      }
    }
  }
});

import { defineConfig } from "vite";
import { tanstackStart } from "@tanstack/react-start/plugin/vite";
import viteReact from "@vitejs/plugin-react";
import { nitro } from "nitro/vite";
import tailwindcss from "@tailwindcss/vite";
import tsConfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  plugins: [
    tsConfigPaths(),
    tailwindcss(),
    // src/server.ts wraps the bundled server entry with the SSR error pages.
    tanstackStart({ server: { entry: "server" } }),
    nitro(),
    viteReact(),
  ],
  server: {
    proxy: {
      // Same-origin /api calls in dev are forwarded to the .NET API, so the
      // httpOnly refresh cookie works without CORS config on the backend.
      "/api": {
        target: process.env.API_PROXY_TARGET ?? "http://localhost:5065",
        changeOrigin: true,
      },
    },
  },
});

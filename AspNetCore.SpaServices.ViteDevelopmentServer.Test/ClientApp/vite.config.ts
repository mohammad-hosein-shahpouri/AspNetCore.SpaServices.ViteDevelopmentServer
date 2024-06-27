import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

if (process["isBun"]) console.log("Running using bun")

// https://vitejs.dev/config/
export default defineConfig({
    build: {
        outDir: "build",
    },
    server: {
        port: Number(process.env.PORT) || 5173,
        hmr: {
            protocol: 'ws',
            host: 'localhost'
        },
    },
    plugins: [react()],
})
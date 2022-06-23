import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
    publicDir: 'public',
    build: {
        outDir: "build"
    },
    server: {
        port: Number(process.env.PORT),
        hmr: {
            protocol: 'ws',
            host: 'localhost'
        }
    },
    plugins: [react()],
})
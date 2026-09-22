import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  // Matches the ASP.NET Core "http" launch profile in src/AmiiboRewards.Api/Properties/launchSettings.json.
  const apiProxyTarget = env.VITE_API_PROXY_TARGET ?? 'http://localhost:5091'

  return {
    plugins: [react()],
    server: {
      host: 'localhost',
      port: 5175,
      strictPort: true,
      proxy: {
        '/api': {
          target: apiProxyTarget,
          changeOrigin: true,
        },
      },
    },
  }
})

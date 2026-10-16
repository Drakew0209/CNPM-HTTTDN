import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [react()],
  resolve: { preserveSymlinks: true },
  server: { fs: { strict: true, allow: ['..'] } },
  test: { environment: 'node', include: ['src/**/*.test.{ts,tsx}', '../../mock/**/*.test.ts'] },
  build: { chunkSizeWarningLimit: 1100 },
});

import { reactRouter } from '@react-router/dev/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import mdx from 'fumadocs-mdx/vite';
import * as MdxConfig from './source.config';

export default defineConfig({
  base: process.env.VITE_BASE_PATH ?? '/',
  plugins: [mdx(MdxConfig), tailwindcss(), reactRouter()],
  resolve: {
    tsconfigPaths: true,
  },
});

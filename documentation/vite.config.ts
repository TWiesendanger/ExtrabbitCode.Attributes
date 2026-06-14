import { reactRouter } from '@react-router/dev/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';
import mdx from 'fumadocs-mdx/vite';
import * as MdxConfig from './source.config';

function getBasePath() {
  if (process.env.VITE_BASE_PATH) {
    return process.env.VITE_BASE_PATH;
  }

  if (process.env.NODE_ENV === 'development') {
    return '/';
  }

  const repoName =
    process.env.GITHUB_REPOSITORY?.split('/')[1] ??
    'ExtrabbitCode.Attributes';

  return `/${repoName}/`;
}

export default defineConfig({
  base: getBasePath(),
  plugins: [mdx(MdxConfig), tailwindcss(), reactRouter()],
  resolve: {
    tsconfigPaths: true,
  },
});

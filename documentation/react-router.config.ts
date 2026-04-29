import type { Config } from '@react-router/dev/config';
import { glob } from 'node:fs/promises';
import { createGetUrl, getSlugs } from 'fumadocs-core/source';

const getUrl = createGetUrl('/docs');

function getBasePath() {
  if (process.env.VITE_BASE_PATH) {
    return process.env.VITE_BASE_PATH;
  }

  if (process.env.NODE_ENV === 'development') {
    return '/';
  }

  const repoName =
    process.env.GITHUB_REPOSITORY?.split('/')[1] ??
    'ExtrabbitCode.Inventor.Attributes';

  return `/${repoName}/`;
}

export default {
  ssr: false,
  basename: getBasePath(),
  future: {
    v8_middleware: true,
  },
  async prerender({ getStaticPaths }) {
    const paths: string[] = [];
    const excluded: string[] = [];

    for (const path of getStaticPaths()) {
      if (!excluded.includes(path)) paths.push(path);
    }

    for await (const entry of glob('**/*.mdx', { cwd: 'content/docs' })) {
      const slugs = getSlugs(entry.replace(/\\/g, '/'));
      paths.push(getUrl(slugs), `/llms.mdx/docs/${[...slugs, 'content.md'].join('/')}`);
    }

    return paths;
  },
} satisfies Config;

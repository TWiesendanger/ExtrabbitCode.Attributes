import defaultMdxComponents from 'fumadocs-ui/mdx';
import type { MDXComponents } from 'mdx/types';
import type { ImgHTMLAttributes } from 'react';

function resolvePublicSrc(src: string | undefined): string | undefined {
  if (!src?.startsWith('/')) return src;
  return `${import.meta.env.BASE_URL}${src.slice(1)}`;
}

export function getMDXComponents(components?: MDXComponents) {
  return {
    ...defaultMdxComponents,
    img: ({ src, alt, ...props }: ImgHTMLAttributes<HTMLImageElement>) => (
      <img src={resolvePublicSrc(src)} alt={alt} {...props} />
    ),
    ...components,
  } satisfies MDXComponents;
}

export const useMDXComponents = getMDXComponents;

declare global {
  type MDXProvidedComponents = ReturnType<typeof getMDXComponents>;
}

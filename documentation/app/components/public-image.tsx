type PublicImageProps = React.ImgHTMLAttributes<HTMLImageElement>;

function resolvePublicSrc(src: string | undefined): string | undefined {
  if (!src?.startsWith('/')) return src;
  return `${import.meta.env.BASE_URL}${src.slice(1)}`;
}

export function PublicImage({ src, alt, ...props }: PublicImageProps) {
  return <img src={resolvePublicSrc(src)} alt={alt} {...props} />;
}

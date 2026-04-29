import type { Route } from './+types/home';
import { HomeLayout } from 'fumadocs-ui/layouts/home';
import { Link } from 'react-router';
import { baseOptions } from '@/lib/layout.shared';

export function meta({}: Route.MetaArgs) {
  return [
    { title: 'Attribute Browser for Autodesk Inventor' },
    { name: 'description', content: 'Add, edit, and delete custom attributes on any Inventor object — directly from a dockable panel.' },
  ];
}

export default function Home() {
  return (
    <HomeLayout {...baseOptions()}>
      <main className="flex flex-col items-center justify-center flex-1 px-6 py-20 text-center gap-6">
        <img
          src="/images/branding/AppIcon.png"
          alt="Attribute Browser icon"
          className="w-24 h-24"
        />

        <div className="flex flex-col items-center gap-3 max-w-lg">
          <h1 className="text-3xl font-bold tracking-tight">
            Attribute Browser
          </h1>
          <p className="text-fd-muted-foreground text-base leading-relaxed">
            An Autodesk Inventor add-in for managing custom attributes on any
            object in your assembly. Add, edit, and delete individual attributes
            or entire attribute sets — all from a dockable panel, without
            writing a single line of code.
          </p>
        </div>

        <Link
          className="bg-fd-primary text-fd-primary-foreground rounded-full font-medium px-6 py-2.5 text-sm hover:opacity-90 transition-opacity"
          to="/docs"
        >
          Open Documentation
        </Link>

        <div className="mt-16 flex flex-col items-center gap-2">
          <span className="text-xs text-fd-muted-foreground uppercase tracking-widest">
            Made by
          </span>
          <img
            src="/images/branding/extrabbit.png"
            alt="Extrabbit logo"
            className="h-8 opacity-80"
          />
        </div>
      </main>
    </HomeLayout>
  );
}
# AGENTS.md

## Commits

- Write short, simple commit messages: one conventional-commit subject line (`feat:`, `fix:`, `docs:`, `chore:`, `build:`, `refactor:`), under ~72 characters, imperative mood. Add a body only when the *why* is not obvious from the subject, and keep it to a few lines.
- The message ends with your text. Attribution trailers such as `Co-Authored-By:` or "Generated with ..." lines are never added.
- Stage files explicitly. `documentation/package-lock.json` often has unrelated local changes; leave it out unless the task is about it.
- Branch off an up-to-date `master` (`git fetch origin master:master` first). Commit or push only when asked.

## Releasing a new version

Versions are `X.Y.Z` for tags and `X.Y.Z.0` inside the project. A release is done when every step below is complete.

1. **Version bump**: set `<AppVersion>X.Y.Z.0</AppVersion>` in `ExtrabbitCode.Attributes/Directory.Build.props`. This is the only place the version lives; the installer and the marketplace bundle (`Addin/Bundle/PackageContents.xml`, `__VERSION__`) get it from CI.
2. **Version history**: add an `X.Y.Z.0` entry at the top of `ExtrabbitCode.Attributes/Resources/versionhistory.txt`, same format as the existing entries. It is shown to users in the Info dialog, so write one user-facing bullet per change since the last tag (`git log <last-tag>..HEAD`); leave out dev-only changes such as build scripts. Done when every user-visible commit since the last tag is covered.
3. **Docs**: every new or changed feature has its page under `documentation/content/docs/` updated, and new pages are listed in the folder's `meta.json`.
4. **Build**: `dotnet build ExtrabbitCode.Attributes/ExtrabbitCode.Attributes.csproj -c Release -p:Platform=x64` succeeds with no new warnings.
5. **Commit** the release as `chore: release X.Y.Z` on `master` (merge the feature branch first).
6. **Tag and push**: `git tag X.Y.Z` and push `master` plus the tag. The tag triggers `.github/workflows/release.yml`, which builds the installer and the `ExtrabbitCode.Attributes.bundle.zip` and creates the GitHub release. Pushing publishes the release, so get the user's go-ahead first.
7. **Marketplace**: the user uploads the bundle zip from the GitHub release to the Autodesk Design and Make marketplace.

## Testing in Inventor

`scripts/Start-Inventor.ps1` builds, deploys to `C:\ProgramData\ExtrabbitCode\ExtrabbitCode.Attributes\` and starts Inventor. Close Inventor first; the running add-in locks the DLL. If Inventor shows old behaviour, another `.addin` with the same ClientId is winning (an installed bundle or an old dev manifest). Inventor also loads renamed files like `*.addin.disabled`, so move stale manifests out of the add-in folders.

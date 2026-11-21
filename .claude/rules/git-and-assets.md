# Git and Repository Safety

## Before editing

- Check `git status --short`. Uncommitted changes the user made are theirs: never revert, overwrite or commit them.

## Committing

- Finished, compiled work is committed and pushed to `main` as one focused commit, as this project's normal workflow.
- Stage files by name and review the staged list before every commit; never `git add -A`.
- When a file mixes the user's uncommitted edits with yours (for example `Packages/manifest.json`), commit only your part.
- Leave regenerated prefab, scene and font files out of a commit when they only differ in IDs or glyphs.
- No Claude co-author or "Generated with Claude Code" lines in commits or pull requests.

## Needs the user's explicit approval

- `git reset --hard`, `git clean`, force-push or any history rewrite.
- Deleting or mass-moving assets.
- Adding or changing packages, or project settings with broad impact.
- Changing game maths, save formats or public APIs that other modules use.

## Never commit

- Licensed content (see the asset rules), secrets, keys or credentials.
- Generated folders (`Library`, `Temp`, `Logs`, `obj`, build output) or user-local settings.

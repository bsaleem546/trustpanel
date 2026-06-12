# TrustPanel — Project Rules for Claude

## Git (applies to ALL agents, always)
- **NEVER run `git commit`, `git push`, or any command that creates commits or pushes to a remote.** The user commits manually. This is permanent — do not ask, do not do it "to be safe".
- Read-only git commands (`git status`, `git diff`, `git log`) are fine.

## Tests
- Do **not** run tests (frontend or backend) unless the user explicitly asks in the current conversation.

## Parallel work boundaries
Two agents may work in this repo at the same time:
- **Backend agent**: works only in `backend/` per `plan.md`.
- **Frontend-integration agent**: works only in `frontend/` (connecting the frontend to the backend API). Must NOT modify anything under `backend/` — it will conflict with the backend agent.

## Frontend notes
- `frontend/` is a Vite + TanStack Start (React) app using **npm** (`package-lock.json`). Bun is not installed on this machine.
- The original design was generated with Lovable; all Lovable-specific references (packages, comments, branding, config) are being removed. Do not reintroduce them.

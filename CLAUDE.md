# CLAUDE.md — PharmaSynth (VR / Meta Quest)

Guidance for Claude Code working in this repository. Read this first each session.

## Project summary
- **PharmaSynth** — a VR game for **Meta Quest**, built in **Unity 6000.5.2f1** (URP).
- This is a **client handoff**: the original developer team discontinued the project. We are effectively rebuilding **from scratch**.
- The client says they have most assets; **some assets may be missing** and can be created if needed.
- The **Unity project itself is currently near-empty** — only the default `SampleScene`, XR scaffolding, and input actions. No game scripts, prefabs, or art are imported yet. All source material lives in `Docs/` (see below).
- Genre / core gameplay loop is **not yet confirmed by the user** — do NOT assume it. It must be derived from the Docs during the planning phase and confirmed with the user.

## Current status (as of last session)
- ✅ Unity MCP server connected and working (see setup notes below).
- ✅ VR packages installed (OpenXR, XR Interaction Toolkit, XR Management). XR + XRI folders exist under `Assets/`.
- ⛔ **Nothing from `Docs/` has been processed yet.** Planning phase has not started. Wait for the user to say "we're in the planning stage" / "go".

## Environment
- OS: Windows 11. Shell: PowerShell primary; Bash (Git Bash / POSIX) also available.
- Unity: `6000.5.2f1`, Universal Render Pipeline (URP 17.5.0), new Input System (1.19.0).
- Git: branch `main`. Initial check-in only.
- VR SDK direction chosen by user: **OpenXR + XR Interaction Toolkit + Meta XR SDK**.
  - Installed via `Packages/manifest.json`: `com.unity.xr.management`, `com.unity.xr.openxr`, `com.unity.xr.interaction.toolkit` (Unity auto-resolved to compatible versions).
  - **Meta XR All-in-One SDK is NOT yet imported** — it comes from the Unity Asset Store (account-gated), imported via Package Manager → My Assets. Still a TODO.

## Unity MCP — connection setup & gotchas (IMPORTANT)
Uses Unity's **official** Assistant MCP Server (`com.unity.ai.assistant`), not a community bridge.
- Requires a **Unity AI subscription seat** — Unity Personal users must have an active seat. Without it, the MCP panel shows **"Up to 0 direct connections allowed"** and every tool call returns **"Connection revoked"**.
- **The fix that worked:** subscribe → **assign the seat to the user in the Unity dashboard** (subscription page → tick user → "Assign seats") → **fully quit and reopen the Unity Editor** so it re-fetches the entitlement. Subscription alone is not enough; the seat must be assigned AND Unity restarted.
- Approval lives in **Project Settings → AI → Unity MCP Server** (client `claude-code` must be Accepted; enable the tools you need — all 52 can be ticked safely).
- If tools ever return "Connection revoked" again: check seat is still assigned, then cold-restart Unity (and if needed Claude Code).
- **Credits:** using the MCP tools (scene edits, scripts, console) does **NOT** consume Unity AI credits. Only Unity's **generative** features (AI Assistant chat, `Unity_AssetGeneration_*` model/texture generation) consume credits. Always flag before spending credits on generation.
- Claude's own reasoning/actions bill to the **Claude Code plan**, separate from Unity entirely.

## Testing without hardware
The Meta Quest headset has NOT been delivered yet. Test in-editor using the **XR Device Simulator** (XRI sample — simulates HMD + controllers via mouse/keyboard). ~90% of gameplay is testable this way; comfort/performance/hand-tracking need the real device later.

## Docs handoff — source of truth (in `Docs/`, NOT yet processed)
**`Docs/Documentations/`**
- `gdocs_link_for_the_manuscript` → Google Doc manuscript: https://docs.google.com/document/d/1TUveyyvGDPXEBGNcftsLuKm9VnIdl3pbX5o_7IHxk5k/edit — read via WebFetch (**must be shared "anyone with link → Viewer"**, else user exports to PDF).
- `implementation_plan.pdf` (253 KB) → previous team's build plan. Read via Read (PDF).

**`Docs/handoff_assets/`**
- `PharmaSynth-Storyboard.pdf` (~267 MB, image-heavy) → read in page ranges (max 20 pages/request; several passes).
- `cutscenes-sample.pdf` (~21 MB) → cutscene reference.
- `Assets Capstone-...zip` (~254 MB) → art/asset pack. Unzip + inventory via Bash before importing.
- `Transition.unitypackage` (~106 MB) → importable Unity assets (import via Assets → Import Package / RunCommand).
- `Transition.zip` (~106 MB) → likely same contents as the .unitypackage; verify vs. it.

## Planned workflow for the planning phase (do when user says "go")
1. Read `implementation_plan.pdf` and the Google Doc manuscript → establish game concept, mechanics, scope.
2. Skim `PharmaSynth-Storyboard.pdf` + `cutscenes-sample.pdf` → understand narrative, scenes (e.g. the lab), cutscenes.
3. Unzip + inventory `Assets Capstone` zip and inspect `Transition.unitypackage` → list what art/assets exist.
4. Produce: (a) confirmed game design summary, (b) asset inventory vs. what's needed → gap list of missing assets, (c) a phased implementation plan. Confirm all with the user before building.
5. Only then start building: OpenXR/Quest project settings, XR Origin rig + XR Device Simulator, lab scene blockout, core interaction/gameplay scripts.

## Working conventions
- Prefer building/verifying through the Unity MCP (create GameObjects, wire components, then `Camera_Capture` / `SceneView_CaptureMultiAngleSceneView` to visually confirm).
- Write C# scripts to `Assets/Scripts/...` (create the folder). Keep code style consistent once a pattern is established.
- Do not run destructive git ops or commit/push unless asked.
- Confirm game-design assumptions with the user rather than inventing gameplay.

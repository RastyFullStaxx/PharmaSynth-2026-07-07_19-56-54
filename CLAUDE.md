# CLAUDE.md — PharmaSynth (VR / Meta Quest)

Guidance for Claude Code working in this repository. Read this first each session.

## Project summary
- **PharmaSynth** — a VR chemistry-lab education game for **Meta Quest 3** (confirmed), built in **Unity 6000.5.2f1** (URP).
- Client handoff from a discontinued capstone team. **NOT a from-scratch rebuild**: the previous team's full Unity project (38 scripts, 49 prefabs, assembled lab scene, IntegrationGuide.md) survives inside `Docs/handoff_assets/Transition.unitypackage` — this is an **audit-and-continue** build.
- **Game concept (confirmed):** first-person guided lab sim, "PharmaSynth: Gear Up, Synth It Up!". 11 experiments (Tutorial: Methane; Prelim: Chemical Compounding, Ethyl Alcohol; Midterm: Benzoic Acid, Acetanilide, Acetone, Chloroform; Final: Benzamide, Aspirin, Caffeine, Wine Making). Per-experiment loop: intro cutscene → reagent prep → synthesis → chemical tests → data sheet/quiz → success/fail cutscene → grade screen. Bayesian Knowledge Tracing mastery, **90% gate** to advance. NPCs: Pharmee (robot guide, subtitles + beeps) + Dr. Jimenez (human examiner, no hints).
- **Hard deadline: August 31, 2026** (contract). Tier build order: Tier 1 = Tutorial + Prelim + Benzoic Acid + Aspirin; Tier 2 = rest of Midterm + Benzamide + Wine; Tier 3 = Caffeine.

## THE PLAN (approved 2026-07-07)
`C:\Users\MSI\.claude\plans\you-are-the-best-cozy-possum.md` — the critique-hardened master plan: full design spec, architecture, asset gap list, week-by-week roadmap, error-handling matrix, Quest 3 perf budget, verification strategy. Follow it; update it when decisions change.
Key user requirements not in the old docs: wrist-flip watch checklist (right hand default, button fallback); auto-checking task conditions; end cutscene ALWAYS (success or fail variant); NPC robot dialogues; improved animations; **spacious lab layout** (wide clearances around tables/gather points, unobstructed experimentation).

## Current status (as of 2026-07-07 session)
- ✅ All Docs processed via 13-agent digest → `Docs/digests/doc-digest-13agents.json` (+ plan critique in `plan-critique-3agents.json`; extracted storyboard/cutscene page renders in `Docs/digests/images/`, gitignored).
- ✅ Plan approved. Execution started on branch **`feature/asset-intake`**.
- ✅ Handoff archives backed up (MD5-verified) to `C:\Users\MSI\PharmaSynth-handoff-backup\` — required before any zip deletion.
- ✅ Assets extracted & organized into `Assets/PharmaSynth/Art/{Environment,Equipment,Characters,UI}` (+ empty `Scripts/Prefabs/ScriptableObjects/Scenes/Timeline/Audio/Materials`); raw DCC sources (labcoat OBJ/.zprj, test-tube .blend) in `Docs/raw-art-sources/` (gitignored). Staging leftovers at `C:\Users\MSI\pharma-staging\` (delete when done).
- ✅ `com.unity.cloud.gltfast` added (for the .glb models: RobotNPC, distillation flask, shelves, thermometer).
- ⏳ NEXT: verify clean import (console + FindProjectAssets), then import `Transition.unitypackage` on a scratch branch (manual merge: keep project's Unity-6 `InputSystem_Actions` + `Settings/` canonical), audit the 38 scripts → `Docs/audit-report.md`, URP-convert Laboratory + ChemLab materials (ChemLab ships a URP converter package; custom Glass.shader needs manual URP-17 port with stereo-instancing), rebuild XR rig on XRI 3.5.
- ⏳ Zips are deleted ONLY after verified import + backup (user request); `Transition.unitypackage` deleted after the W1 audit.

## Environment
- OS: Windows 11. Shell: PowerShell primary; Git Bash also available.
- Unity `6000.5.2f1`, URP 17.5, Input System 1.19, XRI 3.5.1, OpenXR 1.17.1, Timeline. **No Cinemachine (deliberate — never animate the XR camera; cutscenes = PlayableDirector staging + fades).**
- XR: add `com.unity.xr.meta-openxr` for Quest FFR (plain OpenXR lacks it); Meta XR All-in-One SDK (Asset Store, account-gated) optional later. Verify exactly ONE active XR loader.
- Git: `main` + `feature/asset-intake`. Handoff binaries + digest images are gitignored (backup is off-repo). Commit only when asked.
- **Gotcha:** the machine's TEMP points into `C:\Program Files\poppler-24.08.0\Library\bin` — poppler is broken, so the Read tool CANNOT read PDFs. Use `"C:/Program Files/Git/mingw64/bin/pdftotext.exe"` for text and python (pypdf + pillow installed) for page/image extraction. Manuscript Google Doc exports cleanly via `curl -sL "<doc-url>/export?format=txt"`.

## Unity MCP — connection setup & gotchas (IMPORTANT)
Uses Unity's **official** Assistant MCP Server (`com.unity.ai.assistant`), not a community bridge.
- Requires a **Unity AI subscription seat** — without it every call returns "Connection revoked". Fix: assign seat in Unity dashboard → fully restart Unity.
- Approval: **Project Settings → AI → Unity MCP Server** (client `claude-code` Accepted; all 52 tools safe to enable).
- **Credits:** MCP tools (scene edits, scripts, console) consume NO Unity AI credits; only `Unity_AssetGeneration_*` / AI Assistant chat do. Always flag before spending credits.
- **Gotcha:** during long imports/package resolves the editor stops answering ("Unity not detected / no fresh discovery files"). Wait for Editor.log to go quiet (~30s inactivity) and retry.

## Testing
Quest 3 headset NOT delivered yet. Test in-editor via **XR Device Simulator**; escalate to client if no device by W5 (Aug 4-10). Wrist-gesture ergonomics + comfort + 90 Hz validation are day-1 on-device items. Verify via MCP captures (`Camera_Capture`, `SceneView_CaptureMultiAngleSceneView`) + `Unity_ReadConsole` zero-error gate.

## Docs handoff (all processed — see digests)
- `Docs/Documentations/gdocs_link_for_the_manuscript` → manuscript (capstone doc; Appendix C = the WCC lab manual with per-experiment procedures, weights, rubric).
- `Docs/Documentations/implementation_plan.pdf` → OUR signed contract (P1–P11 phases, Aug 31 deadline, deliverables).
- `Docs/handoff_assets/` → storyboard + cutscenes PDFs (reference only; we exceed their quality; their labels are garbled and some pages have copy-paste chemistry errors — never copy chemistry from storyboard, use manual/Appendix C).
- Chemistry conflicts already reconciled in the plan (§3.3); flagged for client: acetanilide acylating agent, scoring weights.

## Working conventions
- Build/verify through Unity MCP; capture screenshots to confirm visual changes.
- C# to `Assets/PharmaSynth/Scripts/...`; experiments are DATA (ScriptableObjects), not scenes; match inherited code style where reused.
- No destructive git ops; commit/push only when asked.
- Confirm game-design changes with the user; the plan file records all decisions.

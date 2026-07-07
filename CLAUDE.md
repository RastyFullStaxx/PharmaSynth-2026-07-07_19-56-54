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

## Current status (as of 2026-07-07 session — Week 1 setup DONE)
- ✅ All Docs processed via 13-agent digest → `Docs/digests/doc-digest-13agents.json` (+ plan critique in `plan-critique-3agents.json`; storyboard/cutscene page renders in `Docs/digests/images/`, gitignored).
- ✅ Plan approved: `C:\Users\MSI\.claude\plans\you-are-the-best-cozy-possum.md`. Work on branch **`feature/asset-intake`**.
- ✅ Handoff archives backed up (MD5-verified) to `C:\Users\MSI\PharmaSynth-handoff-backup\` (also holds `pre-import-snapshot/`). All zips + `Transition.unitypackage` now DELETED from repo (backups verified). Only the 2 reference PDFs remain in `Docs/handoff_assets/`.
- ✅ Assets organized into `Assets/PharmaSynth/Art/{Environment,Equipment,Characters,UI}`; raw DCC sources in `Docs/raw-art-sources/` (gitignored).
- ✅ Previous team's project imported & merged (URP-converted; 0 compile errors; 0 missing scripts across scene + 91 prefabs). Their 36 scripts live at `Assets/Scripts/` (+ `IntegrationGuide.md`); the assembled lab is `Assets/Scenes/SampleScene.unity` with Pharmee/RobotNPC already staged.
- ✅ **Script audit** → `Docs/audit-report.md`. Verdict: GO/reuse the framework. 7 reuse / 18 refactor / 4 rewrite / 7 discard. 5 critical items folded into W2 (missing liquid shader, thin reaction model, naive wrong-reagent detect, dead Tunneling.cs→use XRI vignette, cutscene double-fire bug).
- ✅ **XR set up**: `com.unity.cloud.gltfast` + `com.unity.xr.meta-openxr` 2.5.0 (pulls AR Foundation 6.5). XRI Starter Assets + XR Device Simulator samples imported. XR Origin rig (teleport/snap+continuous turn/dynamic+grab move/climb) placed in SampleScene at (0,0,0.8) facing the lab; Device Simulator added. **OpenXR is the sole active loader for Android** (bootstrapped `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`). Play mode runs with ZERO runtime errors.
- ⏳ **XR follow-up (manual, ~2 min in UI):** Project Settings → XR Plug-in Management → OpenXR (Android): enable **Meta Quest** feature group + an interaction profile (Oculus Touch) + **Foveated Rendering**, and switch active build target to Android. Programmatic `OpenXRSettings.GetSettingsForBuildTargetGroup` returned null via MCP, so do this in the UI.
- ⚠️ **Layout finding (spacious-lab requirement):** room is 10.6×11.2 m but the two central islands (`Table_1`, `Table_1 (1)`) sit only **1.2 m apart** — snug for VR. Widen the central aisle to ≥1.5 m during the W1-2 blockout; front of room (Z −1.8→+3.0) is ~4.8 m clear (good). Bench tops at Y≈1.0 m (good VR height); `Table_1*` are tall 1.8 m units (shelf-topped islands).
- ⚠️ **Unity crashed once** during the meta-openxr/AR-Foundation package resolve (crash dump in `%LOCALAPPDATA%/Temp/Unity/Editor/Crashes/`); auto-recovered, no data loss. If MCP returns "named pipe not found", Unity is busy/restarting — wait for `Logs/Editor.log` to go quiet, then retry.
- ⏳ NEXT (W2): move audit-keepers into `Assets/PharmaSynth/Scripts/` (GUID-preserving MoveAsset), delete the 7 discards; author URP liquid-fill shader; build TaskGraph + BKT scoring; fix cutscene controller bugs; widen island aisle.

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

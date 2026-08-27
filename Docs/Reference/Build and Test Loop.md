# Build and Test Loop

Up: [[Home]] · [[Process MOC]] · Related: [[build-and-run]] · [[Gotchas]]

How to run, test and verify. [[build-and-run]] covers the Quest build config and
simulator keys; this note covers the **verification loop** and what to do when the
Unity MCP bridge is unavailable.

---

## The self-test suite

`Tools ▸ PharmaSynth ▸ Run Self-Tests` — roughly **1,350 assertions**, all in
**edit mode**.

> [!warning] Two things make a green suite look broken
> 1. **Play mode.** ~7 `isPlaying`-gated assertions legitimately fail. Check
>    `Unity_ManageEditor GetState` first, or just exit Play.
> 2. **The wrong scene is open.** Scene-pinned assertions — `bench:`, `match:`,
>    `wired:`, `verbwire:`, `simrun:`, `fumehood:` — inspect real SampleScene objects.
>    With **MainMenu** open they fail *en masse* with `found 0` / `did not run`.
>    **That is not a regression.** Open `Assets/Scenes/SampleScene.unity` and re-run.

**Expected warnings** (green run, not failures): the two W5.9 guard tests and the
Unknown-moduleId negative test.

### Reading the result

Read the one-line summary from disk rather than wrapping the run in a capture script:

```bash
cat Logs/selftest-result.txt
```

> [!tip] An assertion count that did not move means the OLD assembly ran
> Whatever the DLL timestamp says. Cross-check compile status before believing a
> suite result. → [[Gotchas]]

---

## Verifying a compile

There is exactly one trustworthy check:

```bash
grep "error CS" Logs/Editor.log | tail
```

`Unity_ReadConsole` reports 0 errors while the assembly refuses to rebuild. A stale
`Library/ScriptAssemblies/*.dll` after a refresh means the compile **failed**, not
that Unity is idle. → [[Gotchas]]

---

## Working without the Unity MCP bridge

The bridge is free as of `com.unity.ai.assistant` **2.16.0-pre.1** (2026-07-21) — no
AI seat required. **"Connection revoked"** means it is **awaiting approval**, not that
you lack entitlement: approve the client under Project Settings ▸ AI ▸ Unity MCP.
The fallbacks below still matter whenever the editor is closed, busy or mid-reload.

> [!info] MCP is a speed layer, not a capability layer
> Everything below still works. What you actually lose is **latency** — editor-side
> work becomes asynchronous and routed through the user — and **screenshots**, which
> have no fallback.

### Run the suite

Write a request file; the suite runs on the next domain reload and writes its result:

```bash
echo "why you are running it" > Temp/selftest-autorun-request.txt
```

Result lands in `Temp/selftest-autorun-result.txt`. Handled by `SelfTestAutoRun`
(`[InitializeOnLoad]`), which consumes the request so it fires once.

> [!warning] A domain reload is required
> Reloads happen on script compile or on entering Play mode — **not** on merely
> opening a scene. If nothing has changed, ask the user to run the menu item.

### Run any menu item

```bash
echo "Tools/PharmaSynth/<Menu Path>" > Logs/menu-autorun-request.txt
```

`Logs/`, **not** `Temp/` — Unity wipes `Temp`. Handled by `MenuAutoRun`.

### Headless

With the editor **closed**:

```bash
Unity.exe -batchmode -quit -projectPath <proj> -executeMethod MenuAutoRun.RunNow
```

### Inspect the scene

Parse the scene YAML directly. Note that most interesting objects — the XR rig
included — are **prefab instances** (`--- !u!1001`), whose name and position live in
`m_Modification.m_Modifications`, not in a `Transform` block. A naive scan will miss
them entirely.

[[Scene Objects]] is generated this way; regenerate with:

```bash
python Tools/gen-vault-reference.py
```

---

## Running the game

- **Headset play:** `Tools ▸ PharmaSynth ▸ Headset Play Mode` (OpenXR on Play). ON
  drives a Quest-Link headset in editor Play; OFF is headless keyboard/simulator.
  Initialising with no headset attached can stall Play.

> [!important] Never trust a doc for the current toggle state
> Read `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` → Standalone
> `m_InitManagerOnStart`.

- Whichever scene is **open** is where Play starts. Open `MainMenu` to test the full
  boot flow; open `SampleScene` to drop straight into the lab.
- **No Cinemachine** — never animate the XR camera.
- Dev keys and simulator bindings: [[build-and-run]].

---

## Simulation harnesses

These prove the *logic* end-to-end without a headset:

| Menu | Proves |
|---|---|
| `Simulate Campaign` | door → picker → PPE → run → quiz → grade → outro → debrief → unlock → next, for all 9 |
| `Simulate Tutorial Guidance` | every module's real graph, step by step, with something lit at each step |
| `Audit Tutorial Targets` | 9/9 modules, 81/81 steps resolve to real objects |
| `Reveal Stage ▸ <module>` | exactly what a module spawns onto the bench |

> [!warning] Re-run `Simulate Campaign` after changing any module's tasks or skills
> The two-part gate was **unwinnable** — mastery stayed under 0.90 on flawless runs —
> until each module's `trackedSkills` was set to its signature skills. The
> `content: … is BEATABLE` pins guard this.

---

## What edit mode can never answer

The feel of pours, grabs and gestures. Glow timing, ghost readability, label density.
Comfort and scale in the headset.

That is why the remaining work is a **joint headset playtest pass** — and why a
report like *"I spawn below the floor"* is worth more than any number of green
assertions. → [[remaining-work-checklist]] §13

---

## Definition of done for a change

1. Suite green, in edit mode, with **SampleScene** open.
2. Zero-error console.
3. DevCapture only if the **visual** was the question — one shot.
4. Docs updated **in the same change** → [[Working Agreement]].
5. `Docs/changelog.md` gets one line: date · name · end state · suite count.

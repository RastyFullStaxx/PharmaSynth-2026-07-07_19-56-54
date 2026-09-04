# Gotchas

Up: [[Home]] · [[Process MOC]] · [[Architecture MOC]]

Traps that have already cost this project real time. Every entry here is a **bug that
actually happened**, not a hypothetical. Read the relevant section before touching
that area; add to it when you lose a day to something new.

---

## Unity Editor & MCP

> [!danger] `Unity_ReadConsole` lies about compile errors
> It returned **0 errors** while a CS0136 sat in `PharmaSelfTests` and the assembly
> silently refused to rebuild. `IsCompiling: false`, and "success" from
> `Assets/Refresh` and `RequestScriptCompilation` — all of it still lies.
>
> **The only source of truth:**
> ```bash
> grep "error CS" Logs/Editor.log | tail
> ```
> A stale `Library/ScriptAssemblies/*.dll` after a refresh means the compile
> **failed**, not that Unity is idle. And a suite run whose *assertion count* did not
> move ran the OLD assembly, whatever the timestamp says.

> [!danger] A new `.cs` can import but never reach the assembly
> `PlaytestFixW533.cs` had a valid `.meta`, `LoadAssetAtPath<MonoScript>` returned it,
> the assembly compiled green — and its class was absent from the DLL with its menus
> unregistered. `ImportAsset(ForceUpdate)` and `RequestScriptCompilation` did nothing.
>
> **Fix:** rename the file to a NEW asset path (fresh GUID) and refresh. Verify the
> type actually landed with
> `grep -a <TypeName> Library/ScriptAssemblies/Assembly-CSharp-Editor.dll` — not with
> a menu list.

- **Never `RunCommand` while the user is in Play mode.** `OpenScene` throws and scene
  edits force-exit play. Check `Unity_ManageEditor GetState` first.
- `Unity_RunCommand` compiles inside `Unity.AI.*` → **fully qualify or alias**
  `UnityEngine.UI.*` (`UImage`, `UButton`). `System.Reflection`, `ISet` and
  `GetInstanceID` are blocked. File writes and `AssetDatabase.DeleteAsset` get flagged
  "requires user interaction" → use Bash for files, load-or-overwrite for assets.
  **Menu items are exempt.**
- One scene per command. `AssetDatabase.Refresh()` before cross-scene asset loads.
  A `Refresh: true` menu execute can swallow the run in the domain reload.
- MCP **"named pipe not found"** = editor busy → wait for `Logs/Editor.log` to go
  quiet, retry.
- MCP **"Connection revoked"** = the bridge is **awaiting approval**, not an
  entitlement problem. Approve the client under Project Settings ▸ AI ▸ Unity MCP.
  The decision is logged verbatim in `Logs/Editor.log` — grep for
  `=== Connection Info ===` and read the `Validation:` / `Reason:` lines rather than
  guessing. Use the file fallbacks in [[Build and Test Loop]] while it is down.
- `Unity_Camera_Capture` is broken → use **DevCapture** (yaw **0–360 only**; negative
  values misparse).

> [!warning] Unity's MCP panel misreports code signatures — ignore the scare banner
> The Connected Clients panel flags `claude.exe` in red — *"This application is
> unsigned or not recognized and may be dangerous!"* — and lists its publisher as
> **NAVER Global Root Certification Authority**. Both are wrong. Windows reports
> `Status: Valid`, signer `CN="Anthropic, PBC"`, issuer DigiCert; and there is **no
> NAVER certificate in any machine store** (LocalMachine\Root, CurrentUser\Root,
> LocalMachine\CA all return zero). Unity's own `relay_win.exe` gets the identical
> bogus NAVER attribution, so this is a Unity publisher-lookup bug, not a property of
> either binary or of this machine.
>
> **Verify with `Get-AuthenticodeSignature <path>`, never with Unity's panel or the
> `Signed:`/`Publisher:` lines in `Logs/Editor.log`.** The `Accepted` state is what
> actually governs access. A 2026-08-27 session trusted Unity's report and wrongly
> concluded a binary was unsigned.

> [!danger] An embedded package silently overrides the registry — and freezes you there
> `com.unity.ai.assistant` had been **vendored into `Packages/`** (523 MB, 6340 tracked
> files) at repo init, because pre-release packages were disabled and it could not
> resolve otherwise. A folder directly under `Packages/` is an *embedded* package: UPM
> ignores the manifest version entirely, so the project sat on **2.13.0-pre.2** for two
> months while `Packages/manifest.json` looked perfectly reasonable.
>
> The visible symptom was **"Connection revoked"** on every MCP call, which was
> misdiagnosed for weeks as a lapsed AI seat — and nearly paid for with a subscription
> that would have changed nothing. The real cause was a June build predating the
> 2026-07-21 release that removed entitlement caps.
>
> **Tells:** `packages-lock.json` says `"source": "embedded"` and
> `"version": "file:<name>"`; editor stack traces read `./Packages/<name>/...` instead
> of `Library/PackageCache/...`.
>
> **Fix:** close the editor (loaded DLLs are file-locked), `git rm -r` the folder, drop
> the stale lock entry, pin the version in the manifest, enable pre-release packages if
> the version needs it. Check `du -sh Packages/*` when a Unity-owned package misbehaves
> in a way its version number cannot explain.

---

## Edit mode vs Play mode

> [!danger] The `AddComponent` mirror traps
> Both cost a hard-stuck playtest on 2026-07-17.
>
> - **Edit mode:** `OnEnable`/`Awake` do *not* fire on `AddComponent` → hence the
>   `Bind()` seams everywhere.
> - **Play mode:** `AddComponent` fires `OnEnable` **immediately** — *before*
>   `Bind()`, while fields are still null. So event subscription must live in the
>   `Bind` seam, not in `OnEnable` alone.
> - `DestroyImmediate` **skips** `OnDisable`/`OnDestroy` for components whose
>   `OnEnable` never ran → call an explicit `Detach()` before destroying, or C# event
>   subscriptions ghost forever.

- **Never `renderer.material` in edit mode** — use `sharedMaterial` or an MPB. TMP's
  `outlineWidth` also instances materials.
- `AddComponent<LiquidPhysics>` needs a Renderer host.
- Before deleting a script, **grep all of `Assets/`** for the type name.
- FBX imports have **no colliders**.
- ChemLab `_WithLiquid` prefabs: verify **mesh** names, not GameObject names.

---

## Builders that destroy what they touch

> [!danger] Rebuilding silently takes components with it
> A 2026-07-16 `Build Reagent Cabinets` run wiped `Raw_Matchsticks`'
> `MatchStrikerSurface` and two `FlameAnchor`s. Striking the box did nothing — which
> killed the methane splint test **and every burner light in the game** — and nothing
> caught it, because the pure `ShouldStrike`/`ShouldIgnite` tests still passed.
>
> **Pure math cannot see a missing component. Pin the actual scene objects.**
>
> Recovery: `Apply W5.8 Verb Data` → `Add Placement Anchors` →
> **`Re-Home Scene Items (Adopt Current)`**. Adopt the CURRENT hand-placement; never
> re-apply a transform from git (a HEAD *local* position against a different parent
> left the ice bucket floating at y = 2.05).

- `Build Reagent Cabinets` preserves only **unit** transforms, never the items inside.
  Hand-placed things must be **excluded** from stocking, not stocked —
  `IsHandPlacedConsumable` covers the ice bucket and the 4 consumable boxes.
- `ShelfY`'s lowest entry (0.11) is the **Base panel's top face**. `BuildUnit` always
  made that surface and nothing stocked it, so every unit wasted a whole bottom row —
  that is why "overflow" was a phantom.
- **Verb wiring is a step.** `Apply W5.8 Verb Data` wires droppers, the 0.1 g
  spatula, the water bath, the ice bath and time-skip. `Name Tubes + Build Rack Slots`
  wires holder snapping. Both are suite-pinned (`wired:`).

---

## The bench and layouts

> [!danger] The bench ALREADY EXISTS — a layout must never stage it
> Cost a 46-object duplication on 2026-07-16. `SpawnRackKit`, `SpawnSpares` and
> `StageConsumables` were **deleted**: they re-spawned a rack + 6 tubes, 2 spare
> beakers, a flask and a redundant `MatchStriker` cube on **every** module, undoing
> the user's hand-deletions.
>
> Vessels set **`Vessel.benchItem`** (e.g. `Kit_TestTube_0`) → the builder *finds* the
> object and attaches only task wiring. `ClearBenchBindings()` strips it each build or
> it leaks into the next module. **Only task-bound VESSELS may be staged.**

Never author a prop for a general tool or a pourable reagent bottle — they all already
exist: `Eq_Dropper`, `Eq_PorcelainSpatula`, `Eq_Funnel`, `Eq_TestTubeBrush`,
`Eq_GlassRod`, `Eq_WashBottle`, `Eq_Beaker_*`, `Eq_GraduatedCylinder_50mL`,
`Eq_WatchGlass`, `Kit_BunsenBurner_0-9`, `Kit_TestTube_0-18`,
`Kit_Hard-GlassTestTube_0-3`, `Kit_Motar`, `Pestle`, `Tripod`, `WireGauze`, …

A station's `ZoneItemSensor` matches a `LabItem.itemId` **wherever it already lives**,
so point `requiredItemId` at the bench (`kit-bunsenburner`, `kit-funnel`,
`kit-waterbath`).

🔎 **See what a module litters the bench with:**
`Tools ▸ PharmaSynth ▸ Reveal Stage ▸ <module>` — builds the stage in edit mode and
logs an inventory grouped by spawner. Deletes nothing.

### Tubes and racks

Two roles share `itemId kit-testtuberack`:

- **STORAGE racks** (`Eq_TestTubeRack`, `TestTubeRack_2-5`) — where tubes live and
  home. **They get NO slots.** ⛔ Never re-derive them: the tubes were duplicated
  *with* their holders and already sit perfectly.
- **WORKSPACE holders** (`Experiment_Tube_Table_Kit_Holder_1-4`) — start **empty**;
  the player drags tubes in mid-experiment. Only these get `Slot_0-5` anchors.

⚠ **A broken tube respawns at its BAKED home**, so positions must be right *before*
re-homing.

---

## Physics and bounds

> [!danger] Measure with `ExperimentSceneBuilder.SolidWorldBounds`
> Never all child renderers. `LiquidPourer`'s **world-space** `StreamLine`/`PourStream`
> outlive the pour still pointing at the FLOOR, so encapsulating them dragged bounds
> down a metre and racked tubes flew into the air. Same trap for any bounds-fitted
> child. `IsEffectChild` is the pinned list.

> [!danger] Never write the rig root's Y from a horizontal system
> `HeadCollisionPushback` applied its wall-sweep correction as a full 3D vector.
> Real head tracking jitters every frame; each downward jitter sank the rig, which put
> the knee sweep deeper into the floor and guaranteed the next hit. Runaway sink → the
> CharacterController ends up fully inside the floor collider, `Move()` can no longer
> resolve it, and the player is **stuck under the lab floor**. Headset-only: the XR
> Device Simulator's head barely moves, so it never fired.
>
> Also: `Physics.CapsuleCast` returns **distance 0** when the sweep starts already
> overlapping. Treating that as a blocking hit pinned the head in world space and
> dragged the rig by the inverse of every head movement.
>
> Vertical placement belongs to the **CharacterController + gravity**, always.

- `SeatedHeightBoost`'s ground guard is now **two-sided**. It was up-only, which is
  why a sink was unrecoverable.
- Fixed eye height is **not relative to real height** — the Quest runtime flip-flops
  between Floor and Device origin spaces across sessions, and every relative scheme
  produced floor-spawns or roof-spawns.

---

## Text and UI

- **`GlyphSafe.Sanitize` every new TMP string.** LiberationSans lacks ☑ ▶ → Δ ↑ °.
- Transparent geometry does not z-write → visibility is **sortingOrder**:
  HUD 30000 · bubble 29000 · world panels 4000–5000 · TMP labels 20000.
- **TMP will not rebuild `textInfo` on an inactive GameObject.** Activate → write text
  → `ForceMeshUpdate`. Doing it in the other order returned the *previous* line's
  `characterCount`, so a longer line exited the reveal loop early and sat truncated.
  Never trust a 0 count — fall back to the raw string.

---

## Shaders

> [!danger] URP's stock Unlit declares no `_ZTest`
> `SetInt("_ZTest", …)` on it is a **silent no-op**. Both tutorial overlay materials
> inspected as correctly configured and would have shown through nothing. Hence
> `Art/Shaders/PharmaGuide.shader`. **Verify by grepping the `.mat` for `_ZTest`.**

---

## Materials, textures and lighting

> [!danger] `_Smoothness` above 1 is a silent mirror
> URP clamps `_Smoothness` to 0-1, so a material authored at **2.19-3.20** renders as a
> **perfect mirror** and nothing warns you. Four Laboratory-pack materials shipped that
> way - `Ceiling_1` 2.60, `Ceiling_2` 3.20, `Floor` 2.51, `Cabinet` 2.19 - and with **no
> reflection probe in the scene** they mirrored the built-in **procedural outdoor sky**
> inside a sealed windowless room. That is why the lab read as wet plastic for months.
>
> **Never trust a pack's PBR values.** Sweep them:
> ```bash
> grep -rH "  - _Smoothness: [2-9]" --include="*.mat" Assets/
> ```
> Now pinned by `SurfaceSuite` (`surface: no material has smoothness > 1`).
> The opposite error shipped too: the four vessel materials the player handles most
> (beaker 100/500, Erlenmeyer, graduated cylinder) sat at smoothness **0** - matte glass,
> no specular, no reflection - while `GlassMat`/`GlassInnerMat`/`GlassOuterMat` next to
> them were correct at 0.92-0.95.

> [!danger] A Volume profile renders nothing if the camera has post-processing off
> `SampleSceneProfile.asset` had Bloom, Vignette and Tonemapping authored and active, and
> a global Volume sat in the scene - while **both cameras had `m_RenderPostProcessing: 0`**.
> Everything inspected as configured and none of it ran. The same shape of waste: the RP
> asset had reflection-probe **blending and box projection enabled with zero probes** in the
> scene to feed them.
>
> Check the CAMERA, not the profile. And retune before enabling: the template Vignette was
> active at full strength and would have switched on the instant post went live.

> [!danger] Do not re-tile a pack texture without looking at it first
> Retiling `Floor.mat` from 1x1 to 6x6 to "add scale detail" produced hard rectangular
> streaks across the floor. `Floor_AlbedoTransparency.png` has a **defective band of
> vertical lines baked along its top edge**; at the pack's 1x1 mapping that band lands once
> against the far wall and reads as nothing, and tiling repeated the defect six times.
>
> It looked exactly like a bad lightmap bake, and cost **three full rebakes** (resolution
> 12 -> 24 -> 40, plus seam stitching) before the lightmap was ruled out.
> **The decisive test took ten seconds:** set the renderer's `lightmapIndex = -1` and
> capture. The streaks survived, so they were never the bake.
> When an artifact is *invariant to every setting you change*, stop changing settings -
> that invariance is the evidence.

> [!warning] A pack albedo is often an ATLAS, not a tiling swatch
> Binding a flat epoxy map to `Table_1`/`Table_2`/`WashTable` turned **every bench cabinet
> in the room black**: those materials already carried a pack albedo covering the white
> cabinet doors *and* the dark worktop in one atlas. Check whether `_BaseMap` is already
> bound before you bind over it - only `Wall_0`, `Wall_1` and `Ceiling_2` genuinely had none.

> [!warning] The Laboratory FBXs bind materials BY NAME, and it is not recorded on disk
> They import with `materialLocation: 0` (External, obsolete in Unity 6) and an **empty
> `externalObjects: {}`**, so bindings are re-resolved by material name on every reimport.
> Editing the `.mat` files themselves is safe; relying on the FBX to remember an assignment
> is not. Reimporting 12 of them to add lightmap UVs did *not* break anything - verified by
> capture - but check after any reimport rather than assuming.

- **`generateSecondaryUV` is off on 36 of 107 models**, including the whole room shell.
  A mesh with no UV2 cannot receive a lightmap. `LabLightingBake` turns it on only for
  meshes it is about to mark static, and anything still lacking UV2 is set to
  `ReceiveGI.LightProbes` so it still occludes and bounces instead of rendering black.
- **Never bake a grabbable.** An object baked into a lightmap carries its baked shadow
  around the room in your hand. `LabLightingBake.IsBakeCandidate` excludes anything with a
  Rigidbody, `XRGrabInteractable`, `DropRespawn`, `LabItem`, or a dynamic-root ancestor.
- **A runtime `Shader.Find` material gets its shader STRIPPED from device builds.**
  `EffectVfx` has guarded against this since W5.7 by instantiating
  `Resources/FxParticleUnlit`; `StationVfx` did not, so every station steam / frost / drip /
  bubble effect was a candidate to render wrong on the Quest while looking perfect in the
  editor. Both now share the guarded path.

---

## Pharmee's transform ownership

> [!danger] Four scripts move Pharmee, and they only compose because nothing overlaps
> | Transform | Sole owner |
> |---|---|
> | root **position** | `FloatBob` — one sum: `home + bob + jitter + giveWay + gesture` |
> | root **rotation** | `FaceCamera` — FloatBob's `applyRotation` is **0** in the scene |
> | **`Robot Origin`** rotation | `PharmeeAttitude`, in LateUpdate, **overwritten absolutely** |
> | **`Wave*`** ring scale | `PharmeeAttitude` |
>
> `PharmeeMover`, `PharmeeGiveWay` and `PharmeeGestures` write **no transform at all** —
> they feed `SetHome()` / `SetGiveWayOffset()` / `SetGestureOffset()` + `SetPose()`.
>
> **Never add a component that writes `Robot Origin`.** `PharmeeAttitude` assigns it
> absolutely every LateUpdate, so a second writer either loses silently or fights per frame.
> New motion composes *into* that one expression. Suite-pinned (`gesture:`), including
> `applyRotation == 0`, because a stray inspector tick there starts a fight with FaceCamera
> that looks like a physics bug.

> [!warning] Pharmee has NO skeleton — and the model ships animation that never plays
> `RobotNPC.glb` and `RobotNPC.fbx` both have zero skins, joints, LimbNodes, Deformers and
> BindPoses; all 12 of his renderers are `MeshRenderer`. Skeletal animation is impossible
> without a re-rig, which is why the animation set is procedural.
>
> The FBX *does* carry ~364 `AnimationCurveNode` of baked per-node TRS (it is a Sketchfab
> "Futuristic flying animated Robot"), with `importAnimation: 1` and no controller — so none
> of it runs. **Do not simply switch it on:** those curves drive the same Wave rings and body
> node `PharmeeAttitude` already animates, including the `waveSpeedMultiplier = 30` the user
> asked for.
>
> He does have two **hand pivots** (`Hand origin`, `Hand origin.002`) that nothing moved
> before W5.38 — that is the pointing affordance, already in the mesh.
> `RobotNPC.glb` is **orphaned**: zero references anywhere.

> [!warning] `Wire NPC Polish` rebuilds Jimenez's subtitle bubble and drops its AudioSource
> A W5.38 run left `JimenezSubtitles` with **no AudioSource at all** and failed
> `voice: every narration channel has an AudioSource`. Recovery is
> **`Voice ▸ Fix Voice Audibility + Music Ducking`**. Same family as the
> `Build Reagent Cabinets` component wipe — run the repair menu after the builder.

> [!warning] A `FloorBusy` suite failure is usually the PREVIOUS run, not the product
> `NPCNarrationController.s_floor` is a **static** and survives every suite run inside one
> editor domain, so temporary narrators from an earlier run strand it and the next run fails
> *"nobody holds the floor when nothing is speaking"* — a false red that clears only on a
> domain reload. `Run()` now calls `NPCNarrationController.ClearFloor()` first.
> **If a suite failure disappears after a domain reload, it was leaked static state.**

---

## Files, YAML and encoding

> [!warning] Hand-written Unity YAML
> **Single-quote any string scalar containing `": "`** — e.g. `"unchanged: that
> contrast"`. Unquoted, YAML parses it as a key/value split and silently truncates the
> field. Double internal apostrophes.

- `re.sub` expands escapes in its **replacement** template → a literal `\n` becomes a
  real newline. Pass a lambda.
- **Windows FS is case-insensitive** → case-only asset renames need
  `AssetDatabase.RenameAsset`.
- **PDFs:** TEMP is hijacked, so the Read tool cannot open them. Use
  `"C:/Program Files/Git/mingw64/bin/pdftotext.exe"` or pypdf.
- Internet via `curl` (github-raw rate-limits at 429).
- `generate-voice.ps1` needs `-Encoding UTF8` on the manifest read **and** a UTF-8
  *byte* body, or PowerShell 5.1 mangles every em-dash. The `.ps1` must keep its BOM.

---

## Test suite

- **Expected warnings** (not failures): the two W5.9 guard tests and the
  Unknown-moduleId negative test.
- The suite **pins behaviour** — Mishandling lists, ContentSuite task counts, layout
  spacing. Move a pinned assertion **in the same change** as the behaviour it pins.
- Scene-pinned assertions fail *en masse* when the wrong scene is open. → [[Build and Test Loop]]

---

## A bug class the simulator cannot catch

> [!danger] `SimulatedRun` pours from the BINDING, never the HINT
> So a hint whose ACTION line contradicts the binding it must satisfy is structurally
> invisible to it. The W5.34 clueless-player audit found an Exp 2 hard-stuck blocker
> this way: the hint said "leave tube 4 alone" for a tube its own `rackGroup`
> required — and there is no feedback, because the have/required readout only appears
> on a vessel you actually pour into.
>
> **Read hints cold, as a first-timer, and cross-check each ACTION line against the
> binding.**

## Verifying a type actually reached the DLL

> [!warning] grep the DLL for TYPE and MEMBER names, never for a string literal
> .NET stores type and member names in the `#Strings` heap as **UTF-8**, but user string
> literals in the `#US` heap as **UTF-16**. So `grep -a "my assertion text"` against
> `Library/ScriptAssemblies/*.dll` returns 0 even when the code is definitely there, and
> reads exactly like "my edit did not compile" (W5.39, 2026-09-02).
>
> Check `grep -ac VerbDemoMath …` or `grep -ac EnsureDemoGhostMaterial …` instead.
> The companion rule still stands: a suite run whose **assertion count did not move**
> ran the old assembly, whatever the DLL timestamp says.

## A guidance rung that computes but is never consumed

> [!danger] `TutorialCoach.LevelFor` returned 1/2/3; `Update()` returned unless it was 3
> The 15 s and 30 s rungs of the stuck ladder were fully specified, pinned by the suite
> (`LevelFor(20f) == 1`), documented in gameplay-flow — and had never once run in the
> shipped game. The pins tested the *calculation*, not that anything acted on it.
>
> This is the same shape as `WaypointGuide` calling `Hide()` in all 9 modules for weeks:
> **a consumer that silently does nothing reads exactly like a feature that is merely
> off.** When a pure function returns a value nobody branches on, pin the CONSUMER too.

## A freshly generated voice clip does not import itself

> [!warning] `Import & Wire Voice Clips` will report the OLD count
> `generate-voice.ps1` writes the mp3 straight to disk from outside Unity, so until the
> editor imports it there is no `.meta` and no `AudioClip` asset — the wire tool then
> scans the bank and truthfully reports the previous total, which reads as "the
> generator silently failed" (2026-09-02).
>
> Force `AssetDatabase.ImportAsset(<path>, ForceUpdate)` (or refocus the editor) and run
> the wire step **again**. Confirm by the count moving, and by grepping
> `VoiceBank.asset` for the clip id — not by the menu returning success.

## Edit-mode simulation: the component never woke up

> [!danger] `OnEnable` subscriptions and `Update`-driven sims are both absent in edit mode
> `MethaneApparatusRig` wires itself to `ExperimentStarted` from `OnEnable`, so in edit mode
> the event passed it by and **none of its five completion conditions registered**. The
> symptom is vicious: the verb is performed perfectly — the grind reported
> `progress 1.00, IsGrindComplete=true` — and the step still never completes, which reads
> as a broken verb rather than a missing subscription. Separately `TemperatureSim.Update`
> (`→ Tick(deltaTime)`) never runs, so a lit burner held to a tube never warms it: the rig
> only ever calls `SetHeating`.
>
> **Prime the seam and drive the sim** — call the component's own public handler and tick
> the sims the simulator replaces. Do not "fix" the game for a frame that edit mode simply
> does not run. Refactor `Update()` into a public `Step(float dt)` so the simulator drives
> the REAL frame rather than a reimplementation that can drift.
>
> `TaskGraph.HasCondition(taskId)` exists to tell these apart: a verb nothing is listening
> for looks identical, from the outside, to a verb performed badly.

## A test harness that contaminates itself

> [!danger] The first `SimulatedMisplay` pass reported 11 bugs. All 11 were the harness.
> Three independent mistakes, each of which produced confident, plausible, wrong findings:
> - **Shared state between probes.** `StartExperiment` rebuilds the task graph but NOT the
>   scene, and a `LiquidTaskBinding` accumulator lives on the component — so the
>   wrong-reagent probe delivered the step in full and the starvation probe then blamed the
>   supply monitor for correctly seeing no shortfall. Rebuild the stage per probe.
> - **Matching by reference where the code matches by name.** The drain compared
>   `currentChemical` by asset reference; `ReagentSupplyMath` keys availability by
>   `chemicalName`. Same-named bottles stayed full.
> - **Asserting the wrong contract.** Recovery was measured on TASK completion, but a task
>   may name several reagents and the probe pours one — the evidence was in the message
>   the whole time ("needs 40.0, accumulated 42.0").
>
> **Make the failure message print the state it judged.** Every one of these was diagnosed
> the moment the report quoted its own numbers instead of just saying "failed".

## Unity_ManageAsset: the action is `Import`

> [!tip] `Refresh` and `Reimport` are not valid AssetActions
> When the editor stops auto-refreshing (MCP menu executes run against the STALE assembly
> and the DLL timestamp never moves), `Unity_ManageAsset` with `action: "Import"` and the
> script's path forces the import and the recompile. `Unity_RunCommand` frequently answers
> "Unity not detected" in the same state.

## With no headset you spawn INSIDE THE FLOOR

> [!danger] `SeatedHeightBoost` never calibrates without a tracked pose
> The rig uses `RequestedTrackingOriginMode: Floor`, and the fixed-eye-height driver only
> applies its offset when `cameraTransform.localPosition.sqrMagnitude > 0.0001f`. An
> untracked HMD reports **exactly (0,0,0) forever**, so in PC Dev Mode the guard never
> passes, the offset stays 0, and the eye sits on the rig root — at floor level. The lower
> half of the view fills with the floor plane seen from inside it, which reads as "the room
> is dark" or "there is a big blue wall" rather than as a height bug (user, 2026-09-02).
>
> On a Quest `head.y` is non-zero from the first tracked frame, so the headset path was
> always fine — which is why months of edit-mode verification never saw it, and why it was
> the play-mode autopilot's very first screenshot that caught it.
>
> **Fixed** by distinguishing *never tracked* (place the eye at the authored height) from
> *tracking dropped mid-session* (hold the last offset, as before) via `_everTracked`. The
> new branch is unreachable on a headset, so the scarred headset behaviour is untouched by
> construction.

## "Not enough reagents left to finish" at 0% progress

> [!danger] The supply monitor counted the module's OWN PRODUCT as a bench reagent
> The 2026-07-16 client rule DELETED ready-made bottles of Acetanilide, Chloroform,
> Benzamide and Wine precisely so the player must synthesise them. Every later step that
> draws from your own product — the chemical tests, the wash, the racking — then looked to
> `ReagentSupplyMonitor` like a step needing a bottle that does not exist. It raised
> `SupplyExhausted` **at 0% progress, before the player had touched anything**, and
> restarting could never help because the bottle is absent by design.
>
> Four of nine experiments were unplayable this way (user, 2026-09-02). It survived every
> edit-mode check because they all ask "does a bottle run DRY during a run", never the
> simpler "can the bench satisfy this module before the player starts".
>
> **Fixed** by skipping steps whose reagent is `DemoMode.ProductFor(moduleId)`. Pinned by
> the battery's new **supply-at-start** section, which starts every module on a fresh stage
> and requires `EvaluateNow()` to be empty.

## Guidance can point at deliberately HIDDEN objects

> [!warning] The target sweep includes inactive objects, and some are hidden ON PURPOSE
> `TutorialTargets.Build()` matches source bottles over all `LiquidPhysics` including
> inactive, so it registers things that are deliberately switched off for that module:
> the module's own end product (`EndProductVisibility`), the methane-only staged props
> (`MethaneStageVisibility`), and the consumable dispensers' hidden `Template_*` clone
> sources. Tutorial Mode then glows and points at something the player can never see.
>
> **Fixed** by one shared rule, `TutorialTargets.Visible(Component)`: a target must be
> `activeInHierarchy` and must not be a `Template_*` clone source. Applied to the source
> lookup and the station-tool lookup.
>
> **Safe for coverage by construction** — the pour branch registers the DESTINATION before
> it looks for a source, and the verb branch registers the station before the tool, so
> dropping a hidden object can never leave a step with nothing to point at. Verified:
> `Audit Tutorial Targets` still 9/9 81/81, `Simulate Tutorial Guidance` still 73/73, and
> the autopilot's four findings went to zero (143/143 objects pickupable).

## Two bakes go stale when furniture moves, not one

> [!warning] The NavMesh bake has the same shelf life as the lightmap bake
> `Build Lab NavMesh` (W5.44) feeds Tutorial Mode's ground path, and the benches ARE the
> obstacles it routes around — so moving furniture invalidates it exactly as it
> invalidates the lighting. Re-run **both** after any `Select Movable Furniture` session:
> `Build Lab NavMesh` · `Build Lab Probes` → `Run Lab Lighting Bake`.
>
> A stale navmesh fails quietly and confidently: the arrows still draw, they simply route
> through a bench that has since moved.

## Two cues answering one question

> [!tip] Attention is the scarce resource in Tutorial Mode, not information
> The mode already carries glow, x-ray ghost, beacon, spotlight dim, labels, watch hint,
> coach ladder and the verb demo. Every cue added makes the others weaker, so W5.44's rule
> is that new cues are **conditional** and mutually exclusive where they overlap: the
> ground path owns "which way round the benches" beyond 2 m, the beacon owns "which object,
> through that door" within it, and `GuidePathMath.ShowPath`/`ShowBeacon` keep that split in
> one suite-pinned place rather than in two components that can drift.

## Baking a NavMesh silently rewrote the whole SCENE as binary

> [!danger] `NavMeshSurface.BuildNavMesh()` leaves its data IN the scene unless you save it
> `BuildNavMesh` produces a NavMeshData object owned by the surface in memory. Saving the
> scene then serialises it INTO the scene — and NavMeshData cannot be written as YAML, so
> Unity **ignores ForceText and rewrites the scene as binary**. `SampleScene.unity` went
> from 5.08 MB of readable YAML to 1.88 MB of bytes, `git diff` reported only
> `Bin 5080510 -> 1879714`, and `gen-vault-reference.py` parsed **0** objects out of it and
> cheerfully reported "15 Scene Objects" (all from MainMenu). Nothing warned (W5.44).
>
> The content was never lost — only the format — but a binary scene destroys diffs, merges
> and every text-based audit this project relies on.
>
> **Fix:** persist the data as its own asset immediately after the bake
> (`AssetDatabase.CreateAsset`, or `CopySerialized` onto the existing one so the surface's
> guid survives) → `Assets/Scenes/LabNavMesh.asset`. The scene stays YAML.
>
> **Check after ANY new scene-saving builder:** `head -c 10 Assets/Scenes/SampleScene.unity`
> must print `%YAML 1.1`. A size that DROPS sharply is the tell.

## Reopen the scene before running a builder that SAVES

> [!warning] Simulators mutate the open scene; builders save it
> `Simulate Everything`, `Audit Tutorial Targets` and `Simulate Tutorial Guidance` all build
> stages into the open scene by design, and say so. Running a builder that calls
> `SaveOpenScenes()` afterwards commits that mutated state to disk. Reopen SampleScene
> between the two, every time — the reports say so for a reason.


## A vessel with residue can never hold anything again

> [!danger] `AddLiquid` refused to adopt while a precipitate sat in the glass
> `PourOut` drops `currentChemical` once the liquid runs dry but leaves the settled
> precipitate behind. The wake-from-empty guard required **both** columns near-empty, so a
> tube emptied after a test held residue and NO chemical — and every later pour piled up
> with no identity at all. `FindReaction(null, x)` misses forever: no reaction, no colour,
> no product, and nothing anywhere says why.
>
> It made **two experiments unplayable** and no edit-mode check could see it, because the
> edit-mode simulator snapshots and restores every vessel around each run while the play
> sweep inherits the previous module's glassware (W5.45).
>
> **Fixed:** `currentChemical == null` now wakes the vessel too. Pinned by
> `liquid: a vessel holding only residue still adopts what is poured in`.

## A product stream that delivers to the wrong glass

> [!danger] "Expected now" is true for every later step, not just this one
> `VaporCollectController.Update` picked the nearest binding satisfying
> `IsExpectedNow(product)`. In Exp 7 four later steps also draw on chloroform, so the
> redistillate condensed into a downstream test tube, emptied the source flask, and
> `dry-redistil` could never complete — while the class doc claimed the stream was
> "targeted, so it can never pollute a bystander tube".
>
> **Fixed** by scoping the receiver search to a binding expecting the product **for the
> controller's own task** (`ExpectsForThisStep`). When a controller owns one task, match on
> that task — a chemical-only match silently spans the whole module.

## Glassware laid on its side drains itself

> [!danger] `LiquidPourer` measures the ROOT transform up, so a rotated prefab pours forever
> Both `DistillingFlask` objects sat at **90° from upright** (a -90° X rotation, the shape a
> glTF import takes). `PourTick` fires on
> `Vector3.Angle(Vector3.up, transform.up) > pourThreshold`, so the flasks drained every drop
> poured into them: a false `SpilledReagent` mistake on a *perfect* Exp 3 run, and an empty
> source for Exp 7's redistillation.
>
> Nothing caught it for months because edit-mode simulation never runs `Update`, so nothing
> ever poured. **Scan before trusting a vessel:** compute each vessel transform tilt from
> world up; only these two were wrong.
>
> **Fixed** by standing them upright and re-running `Re-Home Scene Items (Adopt Current)` —
> the baked home carries the rotation too, so re-homing is part of the fix, not optional.

## A harness that completes steps for the player proves nothing

> [!danger] The escape hatch that makes an edit-mode audit useful is a lie in Play mode
> `SimulatedRun`'s handlers call `runner.CompleteTask(id)` — "force past to keep exploring
> downstream" — in 22 places. That is the right trade for a static audit: one broken step
> should not hide the other twelve. Run the same handlers in PLAY mode and those same lines
> silently mark steps done that no player could have done, which is the one thing a
> play-mode sweep exists to detect (user, 2026-09-02: "no programmatically cheating
> through").
>
> **`SimulatedRun.NeverForce`** disables all 22 while the visual sweep plays. The sweep
> instead waits ~12 s of real frames, re-applies the player physical hold each tick,
> re-serves once, and then reports the step **UNPLAYED** and stops the module.
>
> The first no-force run went from "10 steps forced, everything green" to two genuinely
> unplayable experiments — both of which turned out to be real game bugs.

## An observation that fires, announces itself, and is never drawn

> [!danger] Every precipitate rule deposits exactly 1 ml, and the gate was `> 1f`
> `ApplyReaction` adds the INCOMING pour to the precipitate column, and a dropper squeeze
> is 1 ml — so `currentPptVolume` lands on exactly 1.0 and `1.0 > 1.0` is false. Six of the
> manuscript's headline observations were authored correctly, fired correctly, announced in
> text and **never rendered**: the milky limewater, both iodoform yellows, the acetanilide
> plates, its hydrolysis crystals and the benzamide solid.
>
> Nothing caught it because every check asked "did the reaction fire", never "could the
> player SEE it". The threshold is now `ShowFromMl` (0.05).
>
> **When a gate compares against a quantity the code itself produces, check the exact
> equality case.** Ship a pin for the value the game actually generates, not a round number.

## Data a rule declares but never carries

> [!warning] `outcome: Precipitate` with `hasPrecipitate: 0` and no precipitate chemical
> `Test_TollensAldehyde` — the silver mirror, the whole point of Exp 2's aldehyde test —
> declared the Precipitate outcome and had neither `hasPrecipitate` nor a
> `resultPrecipitate`. It therefore deposited nothing, forever.
>
> The simulator's own watchdog only fires when `hasPrecipitate` is TRUE and the volume is
> zero, so a rule that never claims the precipitate slips past it. **Audit the outcome
> against the payload**, not just the payload against itself. Now `Chem_SilverMirror` +
> a suite pin.

## A visual rule that can make a vessel draw nothing

> [!danger] "Solids show a mound, so hide the liquid" blanked five finished products
> Suppressing the liquid column whenever the contents are a solid is only safe where a
> mound actually exists. A dry solid arrives by POUR as well as by scoop — the filter and
> drying steps decant the product onto a watch glass, and nothing builds a mound there — so
> the vessel drew neither layer and the finished product was invisible in its own glass.
>
> `DrawsLiquidColumn(ml, dryPowder, hasMound)` now requires a real mound before the column
> is suppressed, pinned with the no-mound case. **Any either/or visual rule needs the
> neither branch checked**: the failure is silent and looks like an empty vessel.

## Millilitres the manuscript asks for can be invisible

> [!tip] 2 ml of aniline in a 250 ml Florence flask is 0.8% of its height
> The quantity is correct, the vessel is correct (the manuscript names the Florence flask),
> and the player still sees an empty flask at arm's length. `DisplayFill01` floors a
> non-empty column at `MinVisibleFill01` (6%) so "there is something in here" reads, while
> every number the player is shown or graded on stays exact. Floor the DRAWING, never the
> data.

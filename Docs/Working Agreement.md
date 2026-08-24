# Working Agreement

Up: [[Home]] · [[Process MOC]]

**The protocol every session follows.** This note is binding, not advisory. CLAUDE.md
points here; this is where the detail lives so CLAUDE.md can stay short.

The bargain is simple:

> **Read the vault before acting. Write back to it before finishing.**

A session that does the first but not the second leaves the next session poorer than
it found it. That is the failure this vault exists to prevent.

---

## 1. Before acting — read what the work touches

Do **not** start editing, and do **not** spawn an exploring subagent, until you have
read the notes covering the area. The vault exists precisely so that a cold session
does not have to re-derive what is already written down. An explore agent costs 100k+
tokens to rediscover a paragraph that took ten seconds to read.

| Work | Read first |
|---|---|
| An experiment's chemistry, steps, reagents, tests, quiz, layout | [[experiments-reference]] |
| Which apparatus a step uses | [[experiments-reference]] §Apparatus — stage from the **procedure**, never the lists (they are defective) |
| Polishing a module | [[experiments-reference]] §POLISH STATUS — **before touching any module** |
| Any runtime mechanic | [[Systems MOC]] → [[systems-reference]] |
| Flow, gate states, review, grading, restarts, demo | [[gameplay-flow]] |
| A class or API | [[Class Index]] |
| Running a builder | [[Editor Menus]] **and** [[Gotchas]] |
| Scene layout, what already exists in the room | [[The Lab Scene]] · [[Scene Objects]] |
| Running, building, testing | [[Build and Test Loop]] |
| What's left | [[remaining-work-checklist]] |
| Manuscript evidence | [[manuscript-reconciliation]] · [[Content MOC]] |
| An unfamiliar term | [[Glossary]] |

**Always skim [[Gotchas]]** when touching an unfamiliar area. Every entry there is a
bug that already happened once.

> [!tip] "If necessary" means: if the work touches it
> Not every session needs every note. A one-line copy fix does not need the
> architecture map. But a session that changes behaviour in an area it has not read
> about is guessing, and this codebase punishes guessing — most of [[Gotchas]] is the
> receipt.

---

## 2. While working — trust, but verify anything load-bearing

Notes reflect what was true when written. Before you rely on a specific claim:

- If a note names a **file, type, menu item or flag**, confirm it still exists.
- If a note states a **toggle's current state**, read the asset instead — e.g.
  Headset Play Mode lives in `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`.
- If a note gives a **count** (assertions, clips, objects), treat it as a checkpoint,
  not a guarantee.

Finding one of these stale is not an annoyance — it is **work to do**. Fix it as you
pass. See §3.

---

## 3. Before finishing — write back

> [!important] Update the docs in the SAME change as the code
> Not "later", not in a follow-up. Canonical facts must never be allowed to drift,
> because the next session will trust them and will not re-verify.

### Living, not append-only

When behaviour changes, **edit the affected lines in place**. Never append a dated
narrative blob.

When you **correct or supersede** something — a fact you realise is wrong, or a
mid-work pivot from approach A to B — **edit the canonical line to the final truth**.

> [!danger] Never leave a stale claim with "actually B" appended
> A document that contradicts itself forces a cold session to guess which half is
> current. **A wrong canonical line is worse than a verbose one**, because sessions
> trust the docs and will not re-verify.

### What goes where

| You changed… | Update |
|---|---|
| A mechanic | [[systems-reference]] (in place) |
| An experiment's data | [[experiments-reference]] (in place) |
| Flow or grading | [[gameplay-flow]] (in place) |
| A trap you lost time to | [[Gotchas]] — **add it**, with the symptom and the cost |
| A term you had to work out | [[Glossary]] |
| Anything at all | [[changelog]] — **one line**: date · name · one-sentence end state · suite count |
| A finished item | [[remaining-work-checklist]] — flip `[x]` + ≤1 line |
| The project's shape | [[Home]] / the relevant MOC |

### The changelog is end-state, not journey

One line per batch. What is true now, not how you got there. The *why* of a decision
belongs in a **code comment at the decision site** and in a **suite assertion** — not
in prose.

### Derived notes are never hand-edited

[[Class Index]], [[Editor Menus]] and [[Scene Objects]] are generated:

```bash
python Tools/gen-vault-reference.py
```

To change what they say, change the thing they are derived from — the `///` comment,
the `[MenuItem]`, or the scene — then regenerate. **Regenerate after any session that
added, renamed or removed a type, a menu item or a scene object.**

---

## 4. Adding a note

Only when it is genuinely new ground. Before creating one, check whether it belongs as
a section in an existing note — a scattering of thin notes is worse than one good one.

When you do add one:

- Give it a name a human would search for, not a code name
- Link it from [[Home]] or a MOC, or it is invisible
- Link **out** liberally — wiki-links are what make the graph navigable
- Prefer callouts (`> [!warning]`, `> [!danger]`) for anything that has bitten someone

---

## 5. The standing constraints

These outrank convenience and are repeated here because they are the ones most often
broken by a session moving fast:

1. **The bench already exists.** A layout must never stage general apparatus or a
   reagent bottle. → [[Gotchas]]
2. **Everything is out, always.** Never hide or per-experiment-gate apparatus to
   reduce clutter. Three narrow exceptions only. → [[The Lab Scene]]
3. **Appendix C is the only chemistry authority.** → [[Content MOC]]
4. **Confirm game-design changes with the user.** Experiments are data; changing what
   the player does is their call, not yours.
5. **Suite green + zero-error console** before a turn ends that changed code or data.
   → [[Build and Test Loop]]
6. **CLAUDE.md is injected into every message of every session.** Every KB is a
   recurring tax. Keep it ~100 lines; detail belongs *here*, not there.

---

## 6. Why this exists

This project has already lost real time to documentation problems, not just code
problems:

- A layout re-spawned 46 objects because the "bench already exists" rule was not read.
- A builder wiped a component and every burner in the game went dark — the pure tests
  still passed, and no doc flagged the trap.
- An authored target list drifted from the code and had to be replaced with a derived
  sweep.
- Hints contradicted the bindings they were meant to satisfy, and the simulator
  structurally could not see it.

Each of those is now a callout in [[Gotchas]]. That is the point: **the vault is how
this project stops paying for the same mistake twice.**

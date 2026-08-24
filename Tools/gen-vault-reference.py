# -*- coding: utf-8 -*-
"""Regenerate the DERIVED Obsidian vault reference notes from the codebase.

    python Tools/gen-vault-reference.py        (from the project root)

Writes:
    Docs/Reference/Class Index.md      every type + its /// doc-comment
    Docs/Reference/Editor Menus.md     every [MenuItem] + its doc-comment
    Docs/Reference/Scene Objects.md    key GameObjects in both scenes

These three notes are DERIVED FROM THE CODE. Never hand-edit them: fix the
/// comment at the decision site (or the scene), then re-run this script.
Everything else in the vault is hand-written and IS the source of truth.
"""
import io
import os
import re
import collections

ROOT = os.path.join('Assets', 'PharmaSynth', 'Scripts')
ROOT_URL = 'Assets/PharmaSynth/Scripts'
NL = chr(10)

DECL = re.compile(
    r'^\s*(?:public|internal|private)?\s*'
    r'(?:static\s+|abstract\s+|sealed\s+|partial\s+)*'
    r'(class|struct|enum|interface)\s+([A-Za-z_]\w*)')
PUB = re.compile(r'^\s{4}public\s+(?!class|struct|enum|interface)(.+?)(?:\s*[{;=]|$)')
MENU = re.compile(r'\[MenuItem\("([^"]+)"')

DONT_EDIT = (
    '> [!warning] Generated note - do not hand-edit' + NL +
    '> Derived from the code by `python Tools/gen-vault-reference.py`.' + NL +
    '> To change what it says, fix the thing it is derived FROM, then re-run.' + NL + NL)


def walk_cs():
    for dirpath, _, files in os.walk(ROOT):
        for fn in sorted(files):
            if fn.endswith('.cs'):
                full = os.path.join(dirpath, fn)
                rel = os.path.relpath(full, ROOT).replace(os.sep, '/')
                yield full, rel


def read_lines(path):
    return io.open(path, encoding='utf-8', errors='replace').read().split(NL)


def doc_above(lines, i):
    """The /// block immediately above line i, joined."""
    doc = []
    for j in range(i - 1, max(-1, i - 16), -1):
        st = lines[j].strip()
        if st.startswith('///'):
            t = re.sub(r'</?summary>', '', st[3:].strip()).strip()
            if t:
                doc.insert(0, t)
        elif st.startswith('[') or not st:
            continue
        else:
            break
    return ' '.join(doc)


def gen_class_index():
    out = collections.OrderedDict()
    for full, rel in walk_cs():
        folder = rel.split('/')[0] if '/' in rel else 'root'
        lines = read_lines(full)
        entries = []
        for i, l in enumerate(lines):
            m = DECL.match(l)
            if not m:
                continue
            members = []
            for j in range(i + 1, min(i + 400, len(lines))):
                if DECL.match(lines[j]):
                    break
                pm = PUB.match(lines[j])
                if pm:
                    sig = pm.group(1).strip()
                    if len(sig) < 110 and not sig.startswith('//'):
                        members.append(sig)
            entries.append((m.group(1), m.group(2), doc_above(lines, i)[:900], members[:14]))
        if entries:
            out.setdefault(folder, []).append((rel, entries))

    order = ['Experiment', 'Chemistry', 'Interaction', 'Scoring', 'Progression',
             'NPC', 'UI', 'Safety', 'Audio', 'Tutorial', 'Editor']
    folders = [f for f in order if f in out] + [f for f in out if f not in order]

    b = ['# Class Index' + NL + NL, DONT_EDIT]
    b.append('Every type in `Assets/PharmaSynth/Scripts/`, with the design rationale its' + NL)
    b.append('author left in the `///` comment. The comments are unusually load-bearing in' + NL)
    b.append('this codebase - most record a bug that cost real time.' + NL + NL)
    b.append('Up: [[Home]] - [[Architecture MOC]] - [[Systems MOC]] - [[Gotchas]]' + NL)
    tot = 0
    for f in folders:
        b.append(NL + '---' + NL + NL + '## ' + f + NL + NL)
        b.append('`' + ROOT_URL + '/' + f + '/`' + NL)
        for rel, entries in sorted(out[f]):
            for kind, name, doc, members in entries:
                tot += 1
                b.append(NL + '### `' + name + '` <sub>' + kind + '</sub>' + NL)
                b.append('<sub>`' + ROOT_URL + '/' + rel + '`</sub>' + NL)
                if doc:
                    b.append(NL + doc + NL)
                if members:
                    b.append(NL + '```csharp' + NL + NL.join(members) + NL + '```' + NL)
    io.open(os.path.join('Docs', 'Reference', 'Class Index.md'), 'w',
            encoding='utf-8', newline='').write(''.join(b))
    return tot, len(folders)


def gen_menus():
    rows = []
    for full, rel in walk_cs():
        lines = read_lines(full)
        # Most builders document themselves on the CLASS, not on the attribute,
        # so an empty doc above the [MenuItem] falls back to the file's own.
        file_doc = ''
        for i, l in enumerate(lines):
            if DECL.match(l):
                file_doc = doc_above(lines, i)
                break
        for i, l in enumerate(lines):
            m = MENU.search(l)
            if m:
                rows.append((m.group(1), rel, (doc_above(lines, i) or file_doc)[:500]))
    rows.sort()
    groups = collections.OrderedDict()
    for path, rel, doc in rows:
        parts = path.split('/')
        head = '/'.join(parts[:2]) if len(parts) > 2 else parts[0]
        groups.setdefault(head, []).append((path, rel, doc))

    b = ['# Editor Menus' + NL + NL, DONT_EDIT]
    b.append('Every `[MenuItem]` in the project. Builders are **idempotent and re-runnable**' + NL)
    b.append('by design - that is the project convention for all scene edits.' + NL + NL)
    b.append('> [!danger] Read [[Gotchas]] before running a stocking/rebuilding builder' + NL)
    b.append('> Several of these DESTROY and recreate what they touch, silently taking' + NL)
    b.append('> hand-placed components and transforms with them.' + NL + NL)
    b.append('Up: [[Home]] - [[Process MOC]] - [[Build and Test Loop]]' + NL)
    for head, items in groups.items():
        b.append(NL + '---' + NL + NL + '## ' + head + NL)
        for path, rel, doc in items:
            b.append(NL + '### `' + path + '`' + NL)
            b.append('<sub>`' + ROOT_URL + '/' + rel + '`</sub>' + NL)
            if doc:
                b.append(NL + doc + NL)
    io.open(os.path.join('Docs', 'Reference', 'Editor Menus.md'), 'w',
            encoding='utf-8', newline='').write(''.join(b))
    return len(rows)


# ---- scene objects -------------------------------------------------------

def scene_objects(path):
    """(name, worldY, kind) for every root-ish GameObject and prefab instance."""
    txt = io.open(path, encoding='utf-8', errors='replace').read()
    blocks = re.split(NL + r'--- !u!(\d+) &(\d+)', txt)
    gos, trs, prefabs = {}, {}, []
    for i in range(1, len(blocks), 3):
        cid, fid, body = blocks[i], blocks[i + 1], blocks[i + 2]
        if cid == '1':
            m = re.search(r'^  m_Name: (.*)$', body, re.M)
            gos[fid] = m.group(1).strip() if m else '?'
        elif cid == '4':
            go = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
            pos = re.search(r'm_LocalPosition: \{x: ([-\d.e+]+), y: ([-\d.e+]+), z: ([-\d.e+]+)\}', body)
            par = re.search(r'm_Father: \{fileID: (\d+)\}', body)
            trs[fid] = (go.group(1) if go else None,
                        pos.groups() if pos else None,
                        par.group(1) if par else '0')
        elif cid == '1001':
            name, p = None, {}
            for m in re.finditer(r'propertyPath: (\S+)' + NL + r'\s*value: (.*)', body):
                k, v = m.group(1), m.group(2).strip()
                if k == 'm_Name':
                    name = v
                elif k.startswith('m_LocalPosition.'):
                    p[k[-1]] = v
            if name:
                prefabs.append((name, p))
    roots = []
    for tid, (go, pos, par) in trs.items():
        if par == '0' and go in gos:
            y = float(pos[1]) if pos else 0.0
            roots.append((gos[go], y))
    return sorted(set(roots)), sorted(prefabs)


def gen_scene_objects():
    b = ['# Scene Objects' + NL + NL, DONT_EDIT]
    b.append('Root-level GameObjects in each scene, and every prefab instance. Use this to' + NL)
    b.append('find *where a thing lives* before writing a builder that spawns a duplicate.' + NL + NL)
    b.append('> [!danger] The bench already exists' + NL)
    b.append('> A layout must NEVER stage general apparatus or a reagent bottle. Vessels BIND' + NL)
    b.append('> to what is already here via `Vessel.benchItem`. See [[Gotchas]].' + NL + NL)
    b.append('Up: [[Home]] - [[The Lab Scene]] - [[Gotchas]]' + NL)
    total = 0
    for label, path in (('SampleScene (the lab)', 'Assets/Scenes/SampleScene.unity'),
                        ('MainMenu (the cube room)', 'Assets/Scenes/MainMenu.unity')):
        if not os.path.exists(path):
            continue
        roots, prefabs = scene_objects(path)
        total += len(roots) + len(prefabs)
        b.append(NL + '---' + NL + NL + '## ' + label + NL)
        b.append('<sub>`' + path + '`</sub>' + NL + NL)
        b.append('### Root objects (' + str(len(roots)) + ')' + NL + NL)
        b.append('| Object | local Y |' + NL + '|---|---|' + NL)
        for n, y in roots:
            b.append('| `' + n + '` | ' + ('%.3f' % y) + ' |' + NL)
        if prefabs:
            b.append(NL + '### Prefab instances (' + str(len(prefabs)) + ')' + NL + NL)
            b.append('| Instance | position override |' + NL + '|---|---|' + NL)
            for n, p in prefabs:
                pos = ', '.join(k + '=' + v for k, v in sorted(p.items())) if p else '(prefab default)'
                b.append('| `' + n + '` | ' + pos + ' |' + NL)
    io.open(os.path.join('Docs', 'Reference', 'Scene Objects.md'), 'w',
            encoding='utf-8', newline='').write(''.join(b))
    return total


def check_links():
    """A vault whose links dangle is worse than no vault. Reports broken
    [[wikilinks]] and notes nothing links to (invisible in the graph).
    Code spans are stripped first so documentation ABOUT links is not a link."""
    notes, refs = {}, collections.defaultdict(list)
    for dp, dn, fns in os.walk('Docs'):
        dn[:] = [d for d in dn if d != '.obsidian']
        for f in fns:
            if f.endswith('.md'):
                notes[f[:-3]] = os.path.join(dp, f)
    for name, path in notes.items():
        txt = io.open(path, encoding='utf-8', errors='replace').read()
        txt = re.sub(r'```.*?```', '', txt, flags=re.S)
        txt = re.sub(r'`[^`' + NL + r']*`', '', txt)
        for m in re.finditer(r'\[\[([^\]|#]+)', txt):
            refs[m.group(1).strip()].append(name)
    broken = sorted(t for t in refs if t not in notes)
    orphan = sorted(n for n in notes if n not in refs and n != 'Home')
    for t in broken:
        print('  BROKEN LINK   [[%s]]  <- %s' % (t, ', '.join(sorted(set(refs[t])))))
    for o in orphan:
        print('  UNLINKED NOTE %s' % o)
    return len(notes), len(broken), len(orphan)


if __name__ == '__main__':
    for d in ('Docs/Reference', 'Docs/MOC'):
        if not os.path.isdir(d):
            os.makedirs(d)
    t, f = gen_class_index()
    m = gen_menus()
    s = gen_scene_objects()
    print('Class Index   : %d types across %d folders' % (t, f))
    print('Editor Menus  : %d menu items' % m)
    print('Scene Objects : %d objects' % s)
    n, b, o = check_links()
    print('Vault         : %d notes, %d broken links, %d unlinked' % (n, b, o))

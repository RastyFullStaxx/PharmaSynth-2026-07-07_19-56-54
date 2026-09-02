"""Tile the VISUAL autopilot's close-ups into one captioned contact sheet per module.

Reads Logs/visual-sweep/manifest.tsv (written by VisualSweep.WriteReport) and writes
Logs/visual-sweep/<nn>-<module>-sheet.jpg — nine pictures instead of seventy-odd, each
cell captioned with the step, what the manuscript/rule promised, and the judge's verdict.
A mid-verb shot (the loaded scoop, the dish at the flame, the balance) is inset top-right.

    python Tools/visual-sheet.py
"""
import csv, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIR = os.path.join(ROOT, "Logs", "visual-sweep")
MAN = os.path.join(DIR, "manifest.tsv")
COLS, W, H, CAP = 3, 480, 360, 84
COLOUR = {"OK": (86, 214, 132), "FAIL": (255, 112, 96), "SKIP": (170, 170, 176)}


def font(size, bold=False):
    for name in (("segoeuib.ttf" if bold else "segoeui.ttf"), ("arialbd.ttf" if bold else "arial.ttf")):
        try:
            return ImageFont.truetype(os.path.join("C:/Windows/Fonts", name), size)
        except OSError:
            pass
    return ImageFont.load_default()


def clip(draw, text, f, width):
    while text and draw.textlength(text, font=f) > width:
        text = text[:-4] + "..."
    return text


def main():
    if not os.path.exists(MAN):
        sys.exit("no manifest at " + MAN + " — run Tools ▸ PharmaSynth ▸ Autopilot Playtest (VISUAL) first")
    rows = list(csv.DictReader(open(MAN, encoding="utf-8"), delimiter="\t"))
    modules = []
    for r in rows:
        if r["module"] not in modules:
            modules.append(r["module"])
    f_small, f_bold = font(15), font(16, True)
    for i, module in enumerate(modules, 1):
        steps = [r for r in rows if r["module"] == module]
        n_rows = (len(steps) + COLS - 1) // COLS
        sheet = Image.new("RGB", (COLS * W, 44 + n_rows * (H + CAP)), (22, 24, 28))
        d = ImageDraw.Draw(sheet)
        tally = {k: sum(1 for s in steps if s["status"] == k) for k in COLOUR}
        d.text((12, 10), f"{i:02d}  {module}   —   OK {tally['OK']} · FAIL {tally['FAIL']} · SKIP {tally['SKIP']}",
               fill=(235, 235, 240), font=f_bold)
        for k, s in enumerate(steps):
            x, y = (k % COLS) * W, 44 + (k // COLS) * (H + CAP)
            shot = os.path.join(ROOT, s["file"]) if s["file"] else None
            if shot and os.path.exists(shot):
                sheet.paste(Image.open(shot).convert("RGB").resize((W, H)), (x, y))
            else:
                d.rectangle((x, y, x + W, y + H), fill=(40, 42, 48))
                d.text((x + 16, y + H // 2 - 8), "(no vessel to photograph)", fill=(150, 150, 156), font=f_small)
            mid = os.path.join(ROOT, s["mid"]) if s["mid"] else None
            if mid and os.path.exists(mid):
                inset = Image.open(mid).convert("RGB").resize((W // 3, H // 3))
                sheet.paste(inset, (x + W - W // 3 - 6, y + 6))
                d.rectangle((x + W - W // 3 - 6, y + 6, x + W - 6, y + 6 + H // 3), outline=(255, 220, 120), width=2)
            c = COLOUR.get(s["status"], (200, 200, 200))
            d.rectangle((x, y + H, x + W, y + H + CAP), fill=(30, 32, 38))
            forced = "  FORCED" if s["completion"] == "FORCED" else ""
            d.text((x + 8, y + H + 6), clip(d, f"[{s['status']}] {s['step']}{forced}", f_bold, W - 16), fill=c, font=f_bold)
            d.text((x + 8, y + H + 30), clip(d, "expect: " + s["expected"], f_small, W - 16), fill=(210, 210, 215), font=f_small)
            d.text((x + 8, y + H + 52), clip(d, "saw: " + s["reason"], f_small, W - 16), fill=(180, 190, 200), font=f_small)
        out = os.path.join(DIR, f"{i:02d}-{module}-sheet.jpg")
        sheet.save(out, quality=86)
        print(f"{out}  ({len(steps)} steps: OK {tally['OK']} FAIL {tally['FAIL']} SKIP {tally['SKIP']})")


if __name__ == "__main__":
    main()

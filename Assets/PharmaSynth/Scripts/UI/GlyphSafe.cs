using System.Text;

/// The world-space lab UI (tablet "pads", wrist holo board, reaction footer) renders
/// with LiberationSans SDF, whose baked atlas is missing arrows, Greek and box glyphs
/// (→ ← ↑ ↓ Δ ⇌ ☑ ☐ ▶ …). Those showed as blank "missing-glyph" boxes on the tablets.
/// Rather than degrade the source chemistry data or regenerate the font atlas (fragile),
/// we map the unsupported glyphs to font-safe equivalents at DISPLAY time. Pure + tested.
public static class GlyphSafe
{
    /// Replace glyphs absent from LiberationSans SDF with readable ASCII/Latin-1 forms.
    public static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            switch (c)
            {
                case '→': sb.Append("->");     break; // → yields
                case '←': sb.Append("<-");     break; // ←
                case '↔': sb.Append("<->");    break; // ↔
                case '⇌': sb.Append("<=>");    break; // ⇌ equilibrium
                case '⇄': sb.Append("<=>");    break; // ⇄
                case '↑': sb.Append("(g)");    break; // ↑ gas evolved
                case '↓': sb.Append("(s)");    break; // ↓ precipitate
                case 'Δ':                                 // Δ (heat)
                case '∆': sb.Append("(heat)"); break; // ∆
                case '≡': sb.Append('=');      break; // ≡
                case '≈': sb.Append('~');      break; // ≈
                case '≠': sb.Append("!=");     break; // ≠
                case '≤': sb.Append("<=");     break; // ≤
                case '≥': sb.Append(">=");     break; // ≥
                case '☑':                                 // ☑ checked box
                case '✅': sb.Append("[x]");    break;
                case '☐': sb.Append("[ ]");    break; // ☐ empty box
                case '▶':                                 // ▶
                case '▸':
                case '►': sb.Append('»'); break; // » (font-safe)
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}

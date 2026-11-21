using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace BeanMeUp;

/// <summary>Gemeinsame Basis: selbst gezeichnet, flimmerfrei, jeder Klick zählt (kein Doppelklick-Schlucken).</summary>
[System.ComponentModel.DesignerCategory("Code")]
abstract class FancyControl : Control
{
    protected bool Hover, Down;

    protected FancyControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.StandardDoubleClick | ControlStyles.Selectable, false);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e) { Hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { Hover = Down = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Down = true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e) { Down = false; Invalidate(); base.OnMouseUp(e); }

    // Ausgegraut (z. B. Design von der Firma gesperrt): kein Hand-Cursor, kein Hover-Effekt
    protected override void OnEnabledChanged(EventArgs e)
    {
        Hover = Down = false;
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected static Graphics Prepare(PaintEventArgs e, Color background)
    {
        var g = e.Graphics;
        g.Clear(background);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        return g;
    }
}

/// <summary>
/// Schlichter Button im Windows-11-Stil.
/// Primary = mit Akzentfarbe gefüllt; Selected = dezent getönt mit Akzentrahmen.
/// </summary>
sealed class Pill : FancyControl
{
    public bool Selected { get; set; }
    public bool Primary { get; set; }
    public bool Danger { get; set; }
    public float Radius { get; set; } = 6;
    /// <summary>Optionales Symbol links vom Text: (Graphics, Bereich, Vordergrund, Hintergrund).</summary>
    public Action<Graphics, RectangleF, Color, Color>? Icon { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Palette.Current;
        var g = Prepare(e, Parent?.BackColor ?? p.Bg);

        Color fill, fg, border;
        if (!Enabled)
        {
            // ausgegraut; die aktuelle Auswahl bleibt leicht erkennbar
            fill = Selected ? Art.Mix(p.Card, p.Sub, 0.18f) : p.Card;
            fg = Art.Mix(p.Sub, p.Card, 0.25f);
            border = p.Border;
        }
        else if (Primary) { fill = Hover ? Art.Mix(p.Accent, Color.White, 0.1f) : p.Accent; fg = p.OnAccent; border = fill; }
        else if (Selected) { fill = p.AccentTint; fg = p.AccentText; border = p.Accent; }
        else { fill = Hover ? p.CardHover : p.Card; fg = Danger ? p.Danger : p.Text; border = p.Border; }
        if (Down) fill = Art.Mix(fill, p.Text, 0.08f);

        var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
        using (var path = Art.RoundRect(r, Radius))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        using var textBrush = new SolidBrush(fg);
        using var sf = new StringFormat
        {
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        if (Icon != null)
        {
            float s = Height * 0.42f, gap = s * 0.55f;
            float textW = g.MeasureString(Text, Font).Width;
            float x = (Width - (s + gap + textW)) / 2;
            Icon(g, new RectangleF(x, (Height - s) / 2, s, s), fg, fill);
            g.DrawString(Text, Font, textBrush, new RectangleF(x + s + gap, 0, textW + 4, Height), sf);
        }
        else
        {
            sf.Alignment = StringAlignment.Center;
            g.DrawString(Text, Font, textBrush, r, sf);
        }
    }
}

/// <summary>Schalter im Windows-11-Stil mit Beschriftung links.</summary>
sealed class ToggleSwitch : FancyControl
{
    public bool Checked { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        var p = Palette.Current;
        var g = Prepare(e, Parent?.BackColor ?? p.Bg);

        float h = Height * 0.76f, w = h * 2f;
        var track = new RectangleF(Width - w - 1.5f, (Height - h) / 2, w, h);

        using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
        using (var textBrush = new SolidBrush(p.Text))
            g.DrawString(Text, Font, textBrush, new RectangleF(0, 0, track.X - 8, Height), sf);

        using (var path = Art.RoundRect(track, h / 2))
        {
            if (Checked)
            {
                using var brush = new SolidBrush(Hover ? Art.Mix(p.Accent, Color.White, 0.1f) : p.Accent);
                g.FillPath(brush, path);
            }
            else
            {
                using var brush = new SolidBrush(Hover ? p.CardHover : p.Card);
                using var pen = new Pen(p.Sub, 1.2f);
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }
        }

        float knob = Checked ? h * 0.62f : h * 0.5f;
        float kx = Checked ? track.Right - h / 2 - knob / 2 : track.X + h / 2 - knob / 2;
        using var knobBrush = new SolidBrush(Checked ? p.OnAccent : p.Sub);
        g.FillEllipse(knobBrush, kx, track.Y + (h - knob) / 2, knob, knob);
    }
}

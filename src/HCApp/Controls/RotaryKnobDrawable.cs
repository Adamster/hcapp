using Microsoft.Maui.Graphics;

namespace HCApp.Controls;

public class RotaryKnobDrawable : IDrawable
{
    private const float ArcStartDeg = 160f;
    private const float ArcSpanDeg = 220f;
    private const float CircleStartDeg = 270f;

    private const float TickRadius = 80f;
    private const float NeedleLength = 50f;
    private const float KnobOuterRadius = 28f;
    private const float KnobInnerRadius = 22f;
    private const float KnobRivetRadius = 4f;
    private const float TickDotRadius = 5f;
    private const float TickDotRadiusSelected = 6f;

    private static readonly Color BackgroundColor = Color.FromArgb("#1A2030");
    private static readonly Color BrassBorderColor = Color.FromArgb("#7A6240");
    private static readonly Color TickUnselected = Color.FromArgb("#3A4A65");
    private static readonly Color TickSelected = Color.FromArgb("#C89020");
    private static readonly Color KnobBodyFill = Color.FromArgb("#2A3040");
    private static readonly Color KnobInnerFill = Color.FromArgb("#1A2030");
    private static readonly Color NeedleColor = Color.FromArgb("#C89020");
    private static readonly Color CentreReadoutCaption = Color.FromArgb("#5A6A80");
    private static readonly Color CentreReadoutName = Color.FromArgb("#C8D8F0");

    public int EnvironmentCount { get; set; }
    public int SelectedIndex { get; set; }
    public bool IsCircleMode => EnvironmentCount > 5;
    public string SelectedName { get; set; } = string.Empty;
    public List<string> Labels { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float cx = IsCircleMode ? 110f : 110f;
        float cy = IsCircleMode ? 110f : 110f;

        DrawBackground(canvas, dirtyRect);
        DrawBrassPlate(canvas, dirtyRect);

        if (EnvironmentCount == 0)
        {
            DrawKnob(canvas, cx, cy, -1);
            return;
        }

        DrawTicks(canvas, cx, cy);
        DrawNeedle(canvas, cx, cy);
        DrawKnob(canvas, cx, cy, SelectedIndex);

        if (IsCircleMode)
            DrawCircleModeReadout(canvas, cx, cy);
    }

    private void DrawBackground(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = BackgroundColor;
        canvas.FillRectangle(rect);
    }

    private void DrawBrassPlate(ICanvas canvas, RectF rect)
    {
        canvas.StrokeColor = BrassBorderColor;
        canvas.StrokeSize = 1.5f;
        canvas.DrawRoundedRectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2, 6);
    }

    private float GetAngleDeg(int index)
    {
        if (IsCircleMode)
        {
            if (EnvironmentCount <= 1) return CircleStartDeg;
            return CircleStartDeg + (360f / EnvironmentCount) * index;
        }
        else
        {
            if (EnvironmentCount <= 1) return ArcStartDeg + ArcSpanDeg / 2f;
            return ArcStartDeg + (ArcSpanDeg / (EnvironmentCount - 1)) * index;
        }
    }

    public PointF GetTickPosition(int index, float cx, float cy)
    {
        float angleDeg = GetAngleDeg(index);
        float angleRad = angleDeg * MathF.PI / 180f;
        return new PointF(
            cx + TickRadius * MathF.Cos(angleRad),
            cy + TickRadius * MathF.Sin(angleRad));
    }

    private void DrawTicks(ICanvas canvas, float cx, float cy)
    {
        for (int i = 0; i < EnvironmentCount; i++)
        {
            var pos = GetTickPosition(i, cx, cy);
            bool isSelected = i == SelectedIndex;

            // Selection ring around selected tick
            if (isSelected)
            {
                canvas.StrokeColor = TickSelected;
                canvas.StrokeSize = 1f;
                canvas.DrawCircle(pos.X, pos.Y, TickDotRadiusSelected + 3.5f);
            }

            canvas.FillColor = isSelected ? TickSelected : TickUnselected;
            float r = isSelected ? TickDotRadiusSelected : TickDotRadius;
            canvas.FillCircle(pos.X, pos.Y, r);

            // Arc mode: draw a short label between knob and tick
            // Circle mode: labels would overlap for many envs — use centre readout only
            if (!IsCircleMode && Labels.Count > i)
                DrawArcTickLabel(canvas, Labels[i], i, cx, cy);
        }
    }

    // Draws label at ~radius 58 (between knob edge r=28 and tick r=80), horizontal, no rotation.
    private void DrawArcTickLabel(ICanvas canvas, string label, int index, float cx, float cy)
    {
        float angleDeg = GetAngleDeg(index);
        float angleRad = angleDeg * MathF.PI / 180f;
        bool isSelected = index == SelectedIndex;

        const float labelRadius = 58f;
        float lx = cx + labelRadius * MathF.Cos(angleRad);
        float ly = cy + labelRadius * MathF.Sin(angleRad);

        string display = GetShortLabel(label);
        canvas.FontColor = isSelected ? TickSelected : CentreReadoutCaption;
        canvas.FontSize = isSelected ? 9f : 7.5f;
        canvas.Font = isSelected ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(display, lx - 22f, ly - 7f, 44f, 14f, HorizontalAlignment.Center, VerticalAlignment.Center);
    }

    // Returns the last meaningful segment of a dotted name, capped at 7 chars, uppercase.
    // "hcapp.mobile.monitor" → "MONITOR"
    // "production" → "PRODUCT"
    private static string GetShortLabel(string label)
    {
        var dot = label.LastIndexOf('.');
        var segment = dot >= 0 && dot < label.Length - 1 ? label[(dot + 1)..] : label;
        return (segment.Length > 7 ? segment[..7] : segment).ToUpperInvariant();
    }

    private void DrawNeedle(ICanvas canvas, float cx, float cy)
    {
        if (EnvironmentCount == 0) return;
        float angleDeg = GetAngleDeg(SelectedIndex);
        float angleRad = angleDeg * MathF.PI / 180f;
        float ex = cx + NeedleLength * MathF.Cos(angleRad);
        float ey = cy + NeedleLength * MathF.Sin(angleRad);

        canvas.StrokeColor = NeedleColor;
        canvas.StrokeSize = 2f;
        canvas.DrawLine(cx, cy, ex, ey);
    }

    private void DrawKnob(ICanvas canvas, float cx, float cy, int selectedIndex)
    {
        canvas.FillColor = KnobBodyFill;
        canvas.StrokeColor = BrassBorderColor;
        canvas.StrokeSize = 1.5f;
        canvas.FillCircle(cx, cy, KnobOuterRadius);
        canvas.DrawCircle(cx, cy, KnobOuterRadius);

        canvas.FillColor = KnobInnerFill;
        canvas.StrokeSize = 0;
        canvas.FillCircle(cx, cy, KnobInnerRadius);

        canvas.FillColor = TickSelected;
        canvas.FillCircle(cx, cy, KnobRivetRadius);
    }

    private void DrawCircleModeReadout(ICanvas canvas, float cx, float cy)
    {
        canvas.FontColor = CentreReadoutCaption;
        canvas.FontSize = 7f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString("SELECTED", cx - 22f, cy - 15f, 44f, 11f, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Use last dotted segment for clarity (e.g. "hcapp.mobile.monitor" → "MONITOR")
        string name = GetShortLabel(SelectedName);
        canvas.FontColor = CentreReadoutName;
        canvas.FontSize = 10f;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.DrawString(name, cx - 22f, cy - 1f, 44f, 14f, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

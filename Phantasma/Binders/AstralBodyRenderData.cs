using Avalonia;
using Avalonia.Media;

namespace Phantasma.Binders;

/// <summary>
/// Renderable astral body data prepared by the Binder.
/// </summary>
public struct AstralBodyRenderData
{
    public double X;
    public double Y;
    public bool HasSprite;
    public IImage Image;
    public Rect SourceRect;
    public byte FallbackColorR;
    public byte FallbackColorG;
    public byte FallbackColorB;
}

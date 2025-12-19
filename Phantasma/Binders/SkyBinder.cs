using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Media.Imaging;

using Phantasma.Models;

namespace Phantasma.Binders;

/// <summary>
/// Binder for the SkyView.
/// Exposes sky state (time, astral bodies, wind) to the View.
/// </summary>
public class SkyBinder : INotifyPropertyChanged
{
    private const int SpriteSize = 16;
    
    private Session _session;
    private double _viewWidth = 800;
    private double _viewHeight = 32;
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    // ===================================================================
    // BOUND PROPERTIES
    // ===================================================================
    
    /// <summary>
    /// Current time string (e.g., "12:00PM").
    /// </summary>
    public string TimeString => _session?.Clock?.TimeHHMM ?? "12:00PM";
    
    /// <summary>
    /// Wind direction string (e.g., "North").
    /// </summary>
    public string WindString => _session?.Wind?.DirectionString ?? "North";
    
    /// <summary>
    /// True if sky is visible (not underground).
    /// </summary>
    public bool IsVisible => _session?.CurrentPlace?.Underground != true;
    
    /// <summary>
    /// Sprite Size for Astral Bodies
    /// </summary>
    public int AstralBodySpriteSize => SpriteSize;
    
    // ===================================================================
    // SKY BACKGROUND COLORS
    // ===================================================================
    
    public byte SkyTopR { get; private set; }
    public byte SkyTopG { get; private set; }
    public byte SkyTopB { get; private set; }
    public byte SkyBottomR { get; private set; }
    public byte SkyBottomG { get; private set; }
    public byte SkyBottomB { get; private set; }
    
    // ===================================================================
    // ASTRAL BODIES
    // ===================================================================
    
    private readonly List<AstralBodyRenderData> _astralBodies = new();
    
    /// <summary>
    /// Pre-computed render data for visible astral bodies.
    /// </summary>
    public IReadOnlyList<AstralBodyRenderData> AstralBodies => _astralBodies;
    
    public int SpriteSize_ => SpriteSize;
    
    // ===================================================================
    // INITIALIZATION
    // ===================================================================
    
    public SkyBinder() 
    {
        UpdateSkyColors();
    }
    
    /// <summary>
    /// Bind to a session.
    /// </summary>
    public void BindToSession(Session session)
    {
        // Unsubscribe from old session.
        if (_session != null)
        {
            _session.Sky.AmbientLightChanged -= OnAmbientLightChanged;
            _session.Clock.TimeChanged -= OnTimeChanged;
            _session.Wind.DirectionChanged -= OnWindChanged;
        }
        
        _session = session;
        
        // Subscribe to new session.
        if (_session != null)
        {
            _session.Sky.AmbientLightChanged += OnAmbientLightChanged;
            _session.Clock.TimeChanged += OnTimeChanged;
            _session.Wind.DirectionChanged += OnWindChanged;
        }
        
        // Notify all properties changed.
        Refresh();
    }
    
    /// <summary>
    /// Update view dimensions (call when SkyView resizes).
    /// </summary>
    public void SetViewSize(double width, double height)
    {
        _viewWidth = width;
        _viewHeight = height;
        UpdateAstralBodies();
    }
    
    // ===================================================================
    // EVENT HANDLERS
    // ===================================================================
    
    private void OnAmbientLightChanged(int light)
    {
        UpdateSkyColors();
        UpdateAstralBodies();
        OnPropertyChanged(nameof(AstralBodies));
    }
    
    private void OnTimeChanged()
    {
        OnPropertyChanged(nameof(TimeString));
    }
    
    private void OnWindChanged(int oldDir, int newDir)
    {
        OnPropertyChanged(nameof(WindString));
    }
    
    // ===================================================================
    // SKY COLOR CALCULATION
    // ===================================================================
    
    private void UpdateSkyColors()
    {
        int ambientLight = _session?.Sky?.GetAmbientLight() ?? 128;
        float t = ambientLight / 255f;
        
        // Night Colors
        byte nightR1 = 5, nightG1 = 5, nightB1 = 20;
        byte nightR2 = 15, nightG2 = 15, nightB2 = 40;
        
        // Day Colors
        byte dayR1 = 135, dayG1 = 206, dayB1 = 235;
        byte dayR2 = 100, dayG2 = 180, dayB2 = 220;
        
        // Lerp between night and day.
        SkyTopR = (byte)(nightR1 + (dayR1 - nightR1) * t);
        SkyTopG = (byte)(nightG1 + (dayG1 - nightG1) * t);
        SkyTopB = (byte)(nightB1 + (dayB1 - nightB1) * t);
        SkyBottomR = (byte)(nightR2 + (dayR2 - nightR2) * t);
        SkyBottomG = (byte)(nightG2 + (dayG2 - nightG2) * t);
        SkyBottomB = (byte)(nightB2 + (dayB2 - nightB2) * t);
    }
    
    // ===================================================================
    // ASTRAL BODY POSITION CALCULATION
    // ===================================================================
    
    private void UpdateAstralBodies()
    {
        _astralBodies.Clear();
        
        if (_session?.Sky == null)
            return;
        
        double skyWindowDegrees = Common.SUNSET_DEGREE - Common.SUNRISE_DEGREE;

        foreach (var body in _session.Sky.Bodies)
        {
            if (!body.IsVisible)
                continue;

            double pixelOffset = ArcToPixelOffset(body.Arc, skyWindowDegrees);
            double x = _viewWidth - pixelOffset - SpriteSize;
            double y = (_viewHeight - SpriteSize) / 2;

            var phase = body.CurrentPhase;
            var sprite = phase?.Sprite;
            var (r, g, b) = GetFallbackColor(phase?.MaxLight ?? 0);

            var renderData = new AstralBodyRenderData
            {
                X = x,
                Y = y,
                HasSprite = sprite?.SourceImage != null,
                FallbackColorR = r,
                FallbackColorG = g,
                FallbackColorB = b
            };

            // Extract Avalonia-compatible data from sprite.
            if (sprite?.SourceImage != null)
            {
                renderData.Image = sprite.SourceImage;
                renderData.SourceRect = new Rect(sprite.SourceX, sprite.SourceY, sprite.WPix, sprite.HPix);
            }

            _astralBodies.Add(renderData);
        }
    }
    
    private double ArcToPixelOffset(int arc, double windowDegrees)
    {
        double slope = (_viewWidth + SpriteSize) / windowDegrees;
        double offset = -Common.SUNRISE_DEGREE * slope;
        return slope * arc + offset;
    }
    
    private (byte R, byte G, byte B) GetFallbackColor(int maxLight)
    {
        if (maxLight > 200)
            return (255, 255, 0);      // Yellow (sun)
        else if (maxLight > 100)
            return (255, 255, 255);    // White (full moon)
        else if (maxLight > 0)
            return (192, 192, 192);    // Light gray (crescent)
        else
            return (64, 64, 64);       // Dark gray (new moon)
    }
    
    // ===================================================================
    // PUBLIC METHODS
    // ===================================================================
    
    /// <summary>
    /// Force refresh of all properties.
    /// </summary>
    public void Refresh()
    {
        UpdateSkyColors();
        UpdateAstralBodies();
        OnPropertyChanged(nameof(TimeString));
        OnPropertyChanged(nameof(WindString));
        OnPropertyChanged(nameof(AstralBodies));
        OnPropertyChanged(nameof(IsVisible));
    }
    
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

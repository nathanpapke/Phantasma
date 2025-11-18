using System;
using Avalonia;

namespace Phantasma.Models;

/// <summary>
/// Map Rendering and Camera System
/// Handles viewport management and rendering coordination.
/// </summary>
public class Map
{
    // Camera Position in Map Coordinates
    private int camX;
    private int camY;

    // Camera Bounds (computed from place dimensions)
    private int camMinX;
    private int camMinY;
    private int camMaxX;
    private int camMaxY;

    // Screen Rectangle for the Map Viewport
    private Rect screenRect;

    // The Place Being Rendered
    private Place place;

    // Object the Camera is Attached To (usually the player)
    private Object cameraSubject;

    // Viewport Dimensions in Tiles
    private int viewportWidthTiles;
    private int viewportHeightTiles;

    // Tile Size in Pixels
    private int tileSize;

    // Dirty Flag for Repaint Optimization
    private bool isDirty = true;

    public int CameraX => camX;
    public int CameraY => camY;
    public Place Place => place;
    public Object CameraSubject => cameraSubject;

    /// <summary>
    /// Top-left Corner of Visible Area in Map Coordinates
    /// </summary>
    public int ViewLeft => Math.Max(0, camX - viewportWidthTiles / 2);
    public int ViewTop => Math.Max(0, camY - viewportHeightTiles / 2);
    public int ViewRight => Math.Min(place?.Width ?? 0, ViewLeft + viewportWidthTiles);
    public int ViewBottom => Math.Min(place?.Height ?? 0, ViewTop + viewportHeightTiles);

    public Map(int viewportWidth, int viewportHeight, int tileSize)
    {
        this.tileSize = tileSize;
        this.viewportWidthTiles = viewportWidth / tileSize;
        this.viewportHeightTiles = viewportHeight / tileSize;
        this.screenRect = new Rect(0, 0, viewportWidth, viewportHeight);
    }

    /// <summary>
    /// Set the place being rendered.
    /// </summary>
    public void SetPlace(Place place)
    {
        this.place = place;
        ComputeCameraBounds();
        MarkDirty();
    }

    /// <summary>
    /// Attach camera to follow an object (usually the player).
    /// </summary>
    public void AttachCamera(Object subject)
    {
        cameraSubject = subject;
        if (subject.Position != null)
        {
            CenterCamera(subject.Position.X, subject.Position.Y);
        }
    }

    /// <summary>
    /// Detach camera from following object.
    /// </summary>
    public void DetachCamera()
    {
        cameraSubject = null;
    }

    /// <summary>
    /// Center camera on specific coordinates.
    /// </summary>
    public void CenterCamera(int x, int y)
    {
        camX = x;
        camY = y;
        
        // Handle map wrapping if needed (Nazghul uses place_wrap_x/y).
        if (place != null)
        {
            // For now, just clamp - can add wrapping later.
            AdjustCameraInBounds();
        }
        
        MarkDirty();
    }

    /// <summary>
    /// Move camera by delta.
    /// </summary>
    public void MoveCamera(int dx, int dy)
    {
        CenterCamera(camX + dx, camY + dy);
    }

    /// <summary>
    /// Update camera to follow attached subject.
    /// Call this each frame if camera is attached to an object.
    /// </summary>
    public void UpdateCamera()
    {
        if (cameraSubject != null && cameraSubject.Position != null)
        {
            CenterCamera(cameraSubject.Position.X, cameraSubject.Position.Y);
        }
    }

    /// <summary>
    /// Adjust camera to stay within valid bounds.
    /// </summary>
    private void AdjustCameraInBounds()
    {
        camX = Math.Clamp(camX, camMinX, camMaxX);
        camY = Math.Clamp(camY, camMinY, camMaxY);
    }

    /// <summary>
    /// Compute camera bounds based on place size.
    /// </summary>
    private void ComputeCameraBounds()
    {
        if (place == null)
            return;
        
        int halfViewWidth = viewportWidthTiles / 2;
        int halfViewHeight = viewportHeightTiles / 2;
        
        // If map is smaller than viewport, center the map.
        if (place.Width <= viewportWidthTiles)
        {
            camMinX = place.Width / 2;
            camMaxX = place.Width / 2;
        }
        else
        {
            camMinX = halfViewWidth;
            camMaxX = place.Width - halfViewWidth;
        }
        
        if (place.Height <= viewportHeightTiles)
        {
            camMinY = place.Height / 2;
            camMaxY = place.Height / 2;
        }
        else
        {
            camMinY = halfViewHeight;
            camMaxY = place.Height - halfViewHeight;
        }
    }

    /// <summary>
    /// Convert map coordinates to screen coordinates.
    /// </summary>
    public (int screenX, int screenY) MapToScreen(int mapX, int mapY)
    {
        int screenX = (mapX - ViewLeft) * tileSize;
        int screenY = (mapY - ViewTop) * tileSize;
        return (screenX, screenY);
    }

    /// <summary>
    /// Convert screen coordinates to map coordinates.
    /// </summary>
    public (int mapX, int mapY) ScreenToMap(int screenX, int screenY)
    {
        int mapX = ViewLeft + (screenX / tileSize);
        int mapY = ViewTop + (screenY / tileSize);
        return (mapX, mapY);
    }

    /// <summary>
    /// Check if a tile is visible in viewport.
    /// </summary>
    public bool TileIsWithinViewport(int x, int y)
    {
        return x >= ViewLeft && x < ViewRight && 
               y >= ViewTop && y < ViewBottom;
    }

    /// <summary>
    /// Mark map as needing repaint.
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
    }

    /// <summary>
    /// Check if map needs repaint.
    /// </summary>
    public bool IsDirty => isDirty;

    /// <summary>
    /// Clear dirty flag (call after rendering).
    /// </summary>
    public void ClearDirty()
    {
        isDirty = false;
    }

    public override string ToString()
    {
        return $"Map(Cam: {camX},{camY} | View: {ViewLeft},{ViewTop} to {ViewRight},{ViewBottom} | Place: {place?.Width}x{place?.Height})";
    }
}
// Phantasma - C# port of Nazghul RPG engine
// Sound.cs - Sound model class

//using a cross-platform sound library

namespace Phantasma.Models;

/// <summary>
/// Represents a loaded sound effect.
/// </summary>
public class Sound
{
    /// <summary>
    /// Scheme identifier tag (e.g., "snd-footstep").
    /// </summary>
    public string Tag { get; set; } = string.Empty;
    
    /// <summary>
    /// Original file path the sound was loaded from.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Raw audio data in memory (PCM format after conversion).
    /// </summary>
    public byte[]? AudioData { get; set; }
    
    /// <summary>
    /// Whether the sound was successfully loaded.
    /// </summary>
    public bool IsLoaded { get; set; }
    
    /// <summary>
    /// Sample rate of the audio (typically 22050 or 44100).
    /// </summary>
    public int SampleRate { get; set; } = 22050;
    
    /// <summary>
    /// Number of audio channels (1 = mono, 2 = stereo).
    /// </summary>
    public int Channels { get; set; } = 2;
    
    /// <summary>
    /// Bits per sample (typically 16).
    /// </summary>
    public int BitsPerSample { get; set; } = 16;
    
    /// <summary>
    /// Duration of the sound in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }
    
    /// <summary>
    /// Reference count for memory management.
    /// </summary>
    internal int RefCount { get; set; } = 1;
    
    /// <summary>
    /// Creates an empty, unloaded sound.
    /// </summary>
    public Sound()
    {
    }
    
    /// <summary>
    /// Creates a sound with the given tag and file path.
    /// Does not load the audio data - call SoundManager.LoadSound() for that.
    /// </summary>
    public Sound(string tag, string filePath)
    {
        Tag = tag;
        FilePath = filePath;
    }
    
    public override string ToString()
    {
        return $"Sound[{Tag}] ({(IsLoaded ? "loaded" : "not loaded")}, {DurationMs}ms)";
    }
}

/// <summary>
/// Represents a currently playing sound instance.
/// </summary>
internal class ActiveSound
{
    /// <summary>
    /// The sound being played.
    /// </summary>
    public Sound? Sound { get; set; }
    
    /// <summary>
    /// Current playback position in bytes.
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Total length of audio data in bytes.
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Playback volume (0-100).
    /// </summary>
    public int Volume { get; set; } = 100;
    
    /// <summary>
    /// Whether this slot is currently in use.
    /// </summary>
    public bool IsActive => Sound != null && Position < Length;
}

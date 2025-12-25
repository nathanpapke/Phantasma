// Phantasma - C# port of Nazghul RPG engine
// SoundManager.cs - Audio system manager using NAudio

using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Phantasma.Models;

/// <summary>
/// Manages audio playback for the game.
/// Uses NAudio for cross-platform audio support.
/// </summary>
public class SoundManager
{
    // ========================================================================
    // Constants
    // ========================================================================
    
    /// <summary>
    /// Maximum number of simultaneous sounds.
    /// </summary>
    private const int MaxSimultaneousSounds = 64;
    
    /// <summary>
    /// Target sample rate for all audio (Nazghul uses 22050).
    /// </summary>
    private const int TargetSampleRate = 22050;
    
    /// <summary>
    /// Target channels (stereo).
    /// </summary>
    private const int TargetChannels = 2;
    
    /// <summary>
    /// Maximum volume level (matches SDL_MIX_MAXVOLUME = 128).
    /// </summary>
    public const int MaxVolume = 128;
    
    // ========================================================================
    // Fields
    // ========================================================================
    
    private IWavePlayer? _waveOut;
    private MixingSampleProvider? _mixer;
    private bool _isEnabled;
    private bool _isInitialized;
    private readonly object _lock = new();
    private readonly Dictionary<string, Sound> _loadedSounds = new();
    
    /// <summary>
    /// Master volume (0.0 to 1.0).
    /// </summary>
    private float _masterVolume = 1.0f;
    
    // ========================================================================
    // Singleton Pattern
    // ========================================================================
    
    private static SoundManager? _instance;
    private static readonly object _instanceLock = new();
    
    /// <summary>
    /// Gets the singleton instance of SoundManager.
    /// </summary>
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new SoundManager();
                }
            }
            return _instance;
        }
    }
    
    // ========================================================================
    // Properties
    // ========================================================================
    
    /// <summary>
    /// Whether the sound system is enabled and working.
    /// </summary>
    public bool IsEnabled => _isEnabled && _isInitialized;
    
    /// <summary>
    /// Master volume (0 to MaxVolume).
    /// </summary>
    public int MasterVolume
    {
        get => (int)(_masterVolume * MaxVolume);
        set => _masterVolume = Math.Clamp(value / (float)MaxVolume, 0f, 1f);
    }
    
    // ========================================================================
    // Constructor
    // ========================================================================
    
    private SoundManager()
    {
        // Private constructor for singleton pattern.
    }
    
    // ========================================================================
    // Initialization
    // ========================================================================
    
    /// <summary>
    /// Initializes the audio system.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    public bool Initialize()
    {
        if (_isInitialized)
            return true;
        
        try
        {
            // Create the mixer for combining multiple sounds.
            // WaveFormat: 22050 Hz, stereo, 32-bit float (NAudio's native format).
            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(TargetSampleRate, TargetChannels);
            _mixer = new MixingSampleProvider(waveFormat)
            {
                ReadFully = true  // Return silence when no inputs
            };
            
            // Create the output device.
            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100  // 100ms latency - good for games
            };
            
            // Initialize output with mixer.
            _waveOut.Init(_mixer);
            _waveOut.Play();
            
            _isInitialized = true;
            _isEnabled = true;
            
            Console.WriteLine("[SoundManager] Audio system initialized (NAudio)");
            Console.WriteLine($"  Sample Rate: {TargetSampleRate} Hz");
            Console.WriteLine($"  Channels: {TargetChannels} (stereo)");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SoundManager] Failed to initialize audio: {ex.Message}");
            _isEnabled = false;
            return false;
        }
    }
    
    // ========================================================================
    // Sound Loading
    // ========================================================================
    
    /// <summary>
    /// Loads a sound from a file.
    /// </summary>
    /// <param name="tag">Scheme identifier for the sound.</param>
    /// <param name="filePath">Path to the WAV file.</param>
    /// <returns>The loaded Sound object, or null if loading failed.</returns>
    public Sound? LoadSound(string tag, string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;
        
        // Check if already loaded.
        lock (_lock)
        {
            if (_loadedSounds.TryGetValue(tag, out var existing))
            {
                existing.RefCount++;
                return existing;
            }
        }
        
        // Resolve path relative to game data directory.
        string fullPath = Phantasma.ResolvePath(filePath);
        
        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"[SoundManager] Sound file not found: {fullPath}");
            return null;
        }
        
        try
        {
            var sound = new Sound(tag, filePath);
            
            // Load and convert the audio file.
            using var reader = new AudioFileReader(fullPath);
            
            // Store audio properties.
            sound.SampleRate = reader.WaveFormat.SampleRate;
            sound.Channels = reader.WaveFormat.Channels;
            sound.BitsPerSample = reader.WaveFormat.BitsPerSample;
            sound.DurationMs = (int)reader.TotalTime.TotalMilliseconds;
            
            // Read all audio data into memory.
            // We store the path and reload on play for memory efficiency,
            // but for frequently used sounds, we could cache the data.
            sound.IsLoaded = true;
            
            lock (_lock)
            {
                _loadedSounds[tag] = sound;
            }
            
            Console.WriteLine($"[SoundManager] Loaded sound: {tag} ({sound.DurationMs}ms)");
            return sound;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SoundManager] Failed to load sound '{filePath}': {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Gets a previously loaded sound by tag.
    /// </summary>
    public Sound? GetSound(string tag)
    {
        lock (_lock)
        {
            return _loadedSounds.GetValueOrDefault(tag);
        }
    }
    
    // ========================================================================
    // Sound Playback
    // ========================================================================
    
    /// <summary>
    /// Plays a sound at the specified volume.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">Volume level (0 to MaxVolume).</param>
    public void Play(Sound? sound, int volume = MaxVolume)
    {
        if (sound == null || !sound.IsLoaded)
            return;
        
        if (!_isEnabled || _mixer == null)
            return;
        
        try
        {
            string fullPath = Phantasma.ResolvePath(sound.FilePath);
            
            if (!File.Exists(fullPath))
                return;
            
            // Calculate final volume (0.0 to 1.0).
            float normalizedVolume = (volume / (float)MaxVolume) * _masterVolume;
            
            // Create a reader for this playback instance.
            var reader = new AudioFileReader(fullPath);
            
            // Resample if necessary to match mixer format.
            ISampleProvider sampleProvider = reader;
            
            if (reader.WaveFormat.SampleRate != TargetSampleRate)
            {
                // Use MediaFoundationResampler for high-quality resampling.
                var resampler = new WdlResamplingSampleProvider(reader, TargetSampleRate);
                sampleProvider = resampler;
            }
            
            // Convert to stereo if mono.
            if (reader.WaveFormat.Channels == 1)
            {
                sampleProvider = new MonoToStereoSampleProvider(sampleProvider);
            }
            
            // Apply volume.
            var volumeProvider = new VolumeSampleProvider(sampleProvider)
            {
                Volume = normalizedVolume
            };
            
            // Wrap in auto-dispose provider to clean up when done.
            var autoDispose = new AutoDisposeSampleProvider(volumeProvider, reader);
            
            // Add to mixer (thread-safe).
            _mixer.AddMixerInput(autoDispose);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SoundManager] Failed to play sound '{sound.Tag}': {ex.Message}");
        }
    }
    
    /// <summary>
    /// Plays a sound at maximum volume.
    /// </summary>
    public void Play(Sound? sound)
    {
        Play(sound, MaxVolume);
    }
    
    /// <summary>
    /// Plays a sound by tag at the specified volume.
    /// </summary>
    public void Play(string tag, int volume = MaxVolume)
    {
        var sound = GetSound(tag);
        Play(sound, volume);
    }
    
    // ========================================================================
    // Sound Cleanup
    // ========================================================================
    
    /// <summary>
    /// Releases a reference to a sound.
    /// When refcount reaches zero, the sound is unloaded.
    /// </summary>
    public void ReleaseSound(Sound? sound)
    {
        if (sound == null)
            return;
        
        lock (_lock)
        {
            sound.RefCount--;
            
            if (sound.RefCount <= 0)
            {
                _loadedSounds.Remove(sound.Tag);
                sound.AudioData = null;
                sound.IsLoaded = false;
                Console.WriteLine($"[SoundManager] Unloaded sound: {sound.Tag}");
            }
        }
    }
    
    // ========================================================================
    // Shutdown
    // ========================================================================
    
    /// <summary>
    /// Shuts down the audio system.
    /// </summary>
    public void Shutdown()
    {
        if (!_isInitialized)
            return;
        
        _isEnabled = false;
        
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
        
        _mixer = null;
        
        lock (_lock)
        {
            _loadedSounds.Clear();
        }
        
        _isInitialized = false;
        Console.WriteLine("[SoundManager] Audio system shut down");
    }
    
    // ========================================================================
    // IDisposable
    // ========================================================================
    
    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }
    
    // ========================================================================
    // Utility Methods
    // ========================================================================
    
    /// <summary>
    /// Enables or disables the sound system at runtime.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled && _isInitialized;
        
        if (!_isEnabled)
        {
            _waveOut?.Stop();
        }
        else if (_waveOut != null)
        {
            _waveOut.Play();
        }
    }
}

// ============================================================================
// Helper Classes
// ============================================================================

/// <summary>
/// Sample provider that automatically disposes underlying resources when playback completes.
/// </summary>
internal class AutoDisposeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly IDisposable _disposable;
    private bool _isDisposed;
    
    public AutoDisposeSampleProvider(ISampleProvider source, IDisposable disposable)
    {
        _source = source;
        _disposable = disposable;
    }
    
    public WaveFormat WaveFormat => _source.WaveFormat;
    
    public int Read(float[] buffer, int offset, int count)
    {
        if (_isDisposed)
            return 0;
        
        int read = _source.Read(buffer, offset, count);
        
        if (read == 0)
        {
            // Playback complete - dispose resources.
            _disposable.Dispose();
            _isDisposed = true;
        }
        
        return read;
    }
}

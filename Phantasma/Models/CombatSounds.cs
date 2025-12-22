namespace Phantasma.Models;

/// <summary>
/// Manages combat-related sounds.
/// 
/// This is owned by Phantasma (game-wide configuration), not Session.
/// Session calls these play methods when combat state changes.
/// </summary>
public class CombatSounds
{
    /// <summary>
    /// Sound played when entering combat.
    /// </summary>
    public Sound? EnterSound { get; set; }
    
    /// <summary>
    /// Sound played when combat is won.
    /// </summary>
    public Sound? VictorySound { get; set; }
    
    /// <summary>
    /// Sound played when combat is lost.
    /// </summary>
    public Sound? DefeatSound { get; set; }
    
    /// <summary>
    /// Plays the combat enter sound.
    /// </summary>
    public void PlayEnterSound()
    {
        if (EnterSound != null)
        {
            SoundManager.Instance.Play(EnterSound, SoundManager.MaxVolume);
        }
    }
    
    /// <summary>
    /// Plays the victory sound.
    /// </summary>
    public void PlayVictorySound()
    {
        if (VictorySound != null)
        {
            SoundManager.Instance.Play(VictorySound, SoundManager.MaxVolume);
        }
    }
    
    /// <summary>
    /// Plays the defeat sound.
    /// </summary>
    public void PlayDefeatSound()
    {
        if (DefeatSound != null)
        {
            SoundManager.Instance.Play(DefeatSound, SoundManager.MaxVolume);
        }
    }
}

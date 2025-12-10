using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

using Phantasma.Models;

namespace Phantasma.Binders;

/// <summary>
/// ViewModel for the status window.
/// Exposes all data needed by StatusView - View never touches Models directly.
/// </summary>
public partial class StatusBinder : BinderBase
{
    private Status status;
    private Party party;
    
    [ObservableProperty]
    private string title = "Party";
    
    [ObservableProperty]
    private int selectedCharacterIndex = -1;
    
    [ObservableProperty]
    private int pageScrollY = 0;
    
    [ObservableProperty]
    private string pageText = "";
    
    [ObservableProperty]
    private bool isShowingParty = true;
    
    [ObservableProperty]
    private bool isShowingStats = false;
    
    [ObservableProperty]
    private bool isShowingPage = false;
    
    [ObservableProperty]
    private bool isSelectMode = false;
    
    public ObservableCollection<PartyMemberInfo> PartyMembers { get; } = new();
    public ObservableCollection<StatLine> StatLines { get; } = new();
    
    // Event for View to know when to re-render
    public event Action DisplayChanged;
    
    public StatusBinder()
    {
        // Default constructor for design-time
    }
    
    /// <summary>
    /// Initialize with Status model and Party.
    /// Called during setup - this is the only place Models are passed in.
    /// </summary>
    public void Initialize(Status status, Party party)
    {
        this.status = status;
        this.party = party;
        
        // Subscribe to status changes
        if (status != null)
        {
            status.StatusChanged += OnStatusChanged;
        }
        
        // Initial update
        UpdateFromModel();
    }
    
    private void OnStatusChanged()
    {
        UpdateFromModel();
        DisplayChanged?.Invoke();
    }
    
    /// <summary>
    /// Pull all data from the Model into observable properties.
    /// This is the ONLY place we read from the Model.
    /// </summary>
    private void UpdateFromModel()
    {
        if (status == null) return;
        
        // Copy state from Model to Binder properties
        Title = status.GetTitle();
        SelectedCharacterIndex = status.SelectedCharacterIndex;
        PageScrollY = status.PageScrollY;
        PageText = status.PageText ?? "";
        
        // Update visibility flags based on mode
        IsShowingParty = (status.Mode == StatusMode.ShowParty || 
                         status.Mode == StatusMode.SelectCharacter);
        IsShowingStats = (status.Mode == StatusMode.Ztats);
        IsShowingPage = (status.Mode == StatusMode.Page);
        IsSelectMode = (status.Mode == StatusMode.SelectCharacter);
        
        // Update collections based on mode
        if (IsShowingParty)
        {
            UpdatePartyMembers();
        }
        else if (IsShowingStats)
        {
            UpdateStatLines();
        }
    }
    
    private void UpdatePartyMembers()
    {
        PartyMembers.Clear();
        
        if (party == null) return;
        
        for (int i = 0; i < party.Size; i++)
        {
            var member = party.GetMemberAtIndex(i);
            if (member != null)
            {
                PartyMembers.Add(new PartyMemberInfo
                {
                    Name = member.GetName(),
                    HP = member.HP,
                    MaxHP = member.MaxHP,
                    Condition = member.IsDead ? "Dead" : "Ok",
                    IsSelected = (SelectedCharacterIndex == i)
                });
            }
        }
    }
    
    private void UpdateStatLines()
    {
        StatLines.Clear();
        
        var character = status?.GetSelectedCharacter();
        if (character == null) return;
        
        // Add stat lines based on current view
        switch (status.CurrentZtatsView)
        {
            case ZtatsView.ViewMember:
                AddMemberStats(character);
                break;
                
            case ZtatsView.ViewArmaments:
                AddArmamentsStats(character);
                break;
                
            // Other views will be implemented as we add those systems
        }
    }
    
    private void AddMemberStats(Character character)
    {
        // Level and XP
        StatLines.Add(new StatLine
        {
            Label = $"Lvl={character.Level,3}",
            Value = $"XP:{character.Experience,7}"
        });
        
        // Strength and HP
        StatLines.Add(new StatLine
        {
            Label = $"Str={character.Strength,3}",
            Value = $"HP:{character.HP,3}/{character.MaxHP,3}"
        });
        
        // Intelligence and MP
        StatLines.Add(new StatLine
        {
            Label = $"Int={character.Intelligence,3}",
            Value = $"MP:{character.MP,3}/{character.MaxMP,3}"
        });
        
        // Dexterity and AC
        StatLines.Add(new StatLine
        {
            Label = $"Dex={character.Dexterity,3}",
            Value = $"AC:{character.ArmorClass,3}"
        });
    }
    
    private void AddArmamentsStats(Character character)
    {
        // This will show equipped weapons/armor
        // For now, just a placeholder
        StatLines.Add(new StatLine
        {
            Label = "*** Arms ***",
            Value = ""
        });
    }
    
    // ===================================================================
    // DIMENSION PROPERTIES - Expose layout constants for View
    // Views should NEVER reference Dimensions directly
    // ===================================================================
    
    public int AsciiWidth => Dimensions.ASCII_W;
    public int AsciiHeight => Dimensions.ASCII_H;
    public int BorderWidth => Dimensions.BORDER_W;
    public int BorderHeight => Dimensions.BORDER_H;
    public int CharsPerLine => Dimensions.STAT_CHARS_PER_LINE;
    
    // ===================================================================
    // NESTED CLASSES - Data transfer objects for View
    // ===================================================================
    
    /// <summary>
    /// Info about a party member for display.
    /// </summary>
    public class PartyMemberInfo
    {
        public string Name { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public string Condition { get; set; }
        public bool IsSelected { get; set; }
    }
    
    /// <summary>
    /// A line of stats for display.
    /// </summary>
    public class StatLine
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Phantasma.Models;

/// <summary>
/// Status window state manager.
/// </summary>
public class Status
{
    // Current mode
    public StatusMode Mode { get; private set; }
    
    // Selected party member index
    public int SelectedCharacterIndex { get; set; }
    
    // Current view in Ztats mode
    public ZtatsView CurrentZtatsView { get; set; }
    
    // Scrolling state
    public int TopLine { get; set; }
    public int CurrentLine { get; set; }
    public int MaxLine { get; set; }
    public int NumVisibleLines { get; set; }
    
    // Page mode text
    public string PageTitle { get; set; }
    public string PageText { get; set; }
    public int PageScrollY { get; set; }
    public int PageMaxScrollY { get; set; }
    
    // List mode
    public object SelectedEntry { get; set; }
    
    // Reference to party
    private Party party;
    
    // Event fired when status needs repaint
    public event Action StatusChanged;
    
    public Status(Party party)
    {
        this.party = party;
        Mode = StatusMode.ShowParty;
        SelectedCharacterIndex = -1;
        CurrentZtatsView = ZtatsView.ViewMember;
        NumVisibleLines = 10;  // Default, will be set based on actual display size
    }
    
    /// <summary>
    /// Set the current status mode
    /// </summary>
    public void SetMode(StatusMode mode)
    {
        Mode = mode;
        
        // Reset scroll state when changing modes
        TopLine = 0;
        CurrentLine = 0;
        PageScrollY = 0;
        
        // Mode-specific initialization
        switch (mode)
        {
            case StatusMode.ShowParty:
                SelectedCharacterIndex = -1;
                break;
                
            case StatusMode.SelectCharacter:
                SelectedCharacterIndex = 0;
                break;
                
            case StatusMode.Ztats:
                CurrentZtatsView = ZtatsView.ViewMember;
                SelectedEntry = null;
                break;
                
            case StatusMode.Ready:
            case StatusMode.Use:
            case StatusMode.MixReagents:
                // These modes show inventory lists
                CalculateMaxLine();
                break;
        }
        
        StatusChanged?.Invoke();
    }
    
    /// <summary>
    /// Scroll the status window
    /// </summary>
    public void Scroll(StatusScrollDir direction)
    {
        switch (Mode)
        {
            case StatusMode.Ztats:
                ScrollZtats(direction);
                break;
                
            case StatusMode.Page:
                ScrollPage(direction);
                break;
                
            case StatusMode.SelectCharacter:
                ScrollParty(direction);
                break;
                
            case StatusMode.Ready:
            case StatusMode.Use:
            case StatusMode.MixReagents:
                ScrollList(direction);
                break;
        }
        
        StatusChanged?.Invoke();
    }
    
    private void ScrollZtats(StatusScrollDir direction)
    {
        switch (direction)
        {
            case StatusScrollDir.ScrollLeft:
                // Cycle through views
                int view = (int)CurrentZtatsView - 1;
                if (view < 0)
                    view = (int)ZtatsView.NumViews - 1;
                CurrentZtatsView = (ZtatsView)view;
                TopLine = 0;
                break;
                
            case StatusScrollDir.ScrollRight:
                // Cycle through views
                CurrentZtatsView = (ZtatsView)(((int)CurrentZtatsView + 1) % (int)ZtatsView.NumViews);
                TopLine = 0;
                break;
                
            case StatusScrollDir.ScrollUp:
                if (TopLine > 0)
                    TopLine--;
                break;
                
            case StatusScrollDir.ScrollDown:
                if (TopLine < MaxLine)
                    TopLine++;
                break;
                
            case StatusScrollDir.ScrollPageUp:
                TopLine = Math.Max(0, TopLine - NumVisibleLines);
                break;
                
            case StatusScrollDir.ScrollPageDown:
                TopLine = Math.Min(MaxLine, TopLine + NumVisibleLines);
                break;
        }
    }
    
    private void ScrollPage(StatusScrollDir direction)
    {
        switch (direction)
        {
            case StatusScrollDir.ScrollUp:
                PageScrollY = Math.Max(0, PageScrollY - 20);
                break;
                
            case StatusScrollDir.ScrollDown:
                PageScrollY = Math.Min(PageMaxScrollY, PageScrollY + 20);
                break;
                
            case StatusScrollDir.ScrollPageUp:
                PageScrollY = Math.Max(0, PageScrollY - 100);
                break;
                
            case StatusScrollDir.ScrollPageDown:
                PageScrollY = Math.Min(PageMaxScrollY, PageScrollY + 100);
                break;
        }
    }
    
    private void ScrollParty(StatusScrollDir direction)
    {
        if (party == null) return;
        
        switch (direction)
        {
            case StatusScrollDir.ScrollUp:
                if (SelectedCharacterIndex > 0)
                    SelectedCharacterIndex--;
                break;
                
            case StatusScrollDir.ScrollDown:
                if (SelectedCharacterIndex < party.Size - 1)
                    SelectedCharacterIndex++;
                break;
        }
    }
    
    private void ScrollList(StatusScrollDir direction)
    {
        switch (direction)
        {
            case StatusScrollDir.ScrollUp:
                if (CurrentLine > 0)
                {
                    CurrentLine--;
                    if (TopLine > 0 && CurrentLine < TopLine + NumVisibleLines / 2)
                        TopLine--;
                }
                break;
                
            case StatusScrollDir.ScrollDown:
                if (CurrentLine < MaxLine)
                {
                    CurrentLine++;
                    if (TopLine < MaxLine && CurrentLine >= TopLine + NumVisibleLines / 2)
                        TopLine++;
                }
                break;
                
            case StatusScrollDir.ScrollPageUp:
                for (int i = 0; i < NumVisibleLines && CurrentLine > 0; i++)
                {
                    CurrentLine--;
                    if (TopLine > 0)
                        TopLine--;
                }
                break;
                
            case StatusScrollDir.ScrollPageDown:
                for (int i = 0; i < NumVisibleLines && CurrentLine < MaxLine; i++)
                {
                    CurrentLine++;
                    if (TopLine < MaxLine)
                        TopLine++;
                }
                break;
        }
    }
    
    /// <summary>
    /// Calculate max scroll line based on content
    /// </summary>
    private void CalculateMaxLine()
    {
        // This will be implemented when we have inventory system
        // For now, just set a reasonable default
        MaxLine = Math.Max(0, 20 - NumVisibleLines);
    }
    
    /// <summary>
    /// Set page mode text
    /// </summary>
    public void SetPageText(string title, string text)
    {
        PageTitle = title;
        PageText = text;
        PageScrollY = 0;
        
        // Calculate max scroll based on text length
        // This is a rough estimate - will be refined when rendering
        int lines = text.Split('\n').Length;
        PageMaxScrollY = Math.Max(0, (lines * 20) - (NumVisibleLines * 20));
    }
    
    /// <summary>
    /// Get the currently selected character
    /// </summary>
    public Character GetSelectedCharacter()
    {
        if (party == null || SelectedCharacterIndex < 0 || SelectedCharacterIndex >= party.Size)
            return null;
            
        return party.GetMemberAtIndex(SelectedCharacterIndex);
    }
    
    /// <summary>
    /// Get status title based on current mode
    /// </summary>
    public string GetTitle()
    {
        switch (Mode)
        {
            case StatusMode.ShowParty:
                return "Party";
                
            case StatusMode.SelectCharacter:
                return "select";
                
            case StatusMode.Ztats:
                var character = GetSelectedCharacter();
                if (character != null)
                    return character.GetName();
                return GetZtatsTitle();
                
            case StatusMode.Ready:
                return GetSelectedCharacter()?.GetName() ?? "Ready";
                
            case StatusMode.Use:
                return "select";
                
            case StatusMode.Page:
                return PageTitle ?? "Page";
                
            case StatusMode.Trade:
                return "Trade";
                
            case StatusMode.MixReagents:
                return "reagents";
                
            default:
                return "Status";
        }
    }
    
    private string GetZtatsTitle()
    {
        switch (CurrentZtatsView)
        {
            case ZtatsView.ViewMember:
                return "Party Member";
            case ZtatsView.ViewArmaments:
                return "Armaments";
            case ZtatsView.ViewReagents:
                return "Reagents";
            case ZtatsView.ViewSpells:
                return "Spells";
            case ZtatsView.ViewItems:
                return "Usable Items";
            case ZtatsView.ViewMisc:
                return "Misc";
            default:
                return "Stats";
        }
    }
}

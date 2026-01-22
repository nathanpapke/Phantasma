using System;
using Avalonia.Input;

namespace Phantasma.Models;

/// <summary>
/// Key handler for quantity/amount input.
/// Used by kern-conv-get-amount and similar prompts.
/// </summary>
public class AmountKeyHandler : IKeyHandler
{
    private readonly Action<int?> onComplete;
    private readonly Session session;
    private string inputBuffer = "";
    private readonly int maxValue;
    
    public int? Result { get; private set; }
    
    public AmountKeyHandler(Session session, Action<int?> onComplete, int maxValue = -1)
    {
        this.session = session;
        this.onComplete = onComplete;
        this.maxValue = maxValue;
    }
    
    public bool HandleKey(Key key, string? keySymbol)
    {
        // Number keys.
        if (key >= Key.D0 && key <= Key.D9)
        {
            char digit = (char)('0' + (key - Key.D0));
            inputBuffer += digit;
            session.UpdateCommandInput(inputBuffer);
            return false;  // Keep handling
        }
        
        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            char digit = (char)('0' + (key - Key.NumPad0));
            inputBuffer += digit;
            session.UpdateCommandInput(inputBuffer);
            return false;
        }
        
        // Backspace.
        if (key == Key.Back && inputBuffer.Length > 0)
        {
            inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            session.UpdateCommandInput(inputBuffer);
            return false;
        }
        
        // Enter = confirm.
        if (key == Key.Enter || key == Key.Return)
        {
            if (int.TryParse(inputBuffer, out int value))
            {
                if (maxValue >= 0 && value > maxValue)
                    value = maxValue;
                Result = value;
                onComplete?.Invoke(value);
            }
            else
            {
                Result = 0;
                onComplete?.Invoke(0);
            }
            return true;  // Done
        }
        
        // Escape = cancel.
        if (key == Key.Escape)
        {
            Result = null;
            session.UpdateCommandInput("(cancelled)");
            onComplete?.Invoke(null);
            return true;
        }
        
        return false;
    }
}
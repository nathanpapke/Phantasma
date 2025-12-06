using System;

namespace Phantasma.Models;

/// <summary>
/// Dice rolling system - exact port of Nazghul's dice.c
/// Supports notation like: "1d20", "2d6+3", "1d8-1", "+5"
/// </summary>
public class Dice
{
    private static Random random = new Random();
    
    /// <summary>
    /// Roll dice using standard notation.
    /// Examples: "1d20", "2d6+3", "1d8-1", "5"
    /// </summary>
    /// <param name="diceString">Dice notation string</param>
    /// <returns>Total rolled value</returns>
    public static int Roll(string diceString)
    {
        if (string.IsNullOrEmpty(diceString))
            return 0;
            
        if (!Parse(diceString, out int num, out int faces, out int bias))
        {
            // Parse error - return 0 like Nazghul does
            Console.WriteLine($"Warning: Invalid dice string '{diceString}'");
            return 0;
        }
        
        int val = 0;
        
        // Roll each die
        for (int i = 0; i < num; i++)
        {
            val += random.Next(1, faces + 1); // Next(1, faces+1) gives [1, faces]
        }
        
        // Add bias
        val += bias;
        
        return val;
    }
    
    /// <summary>
    /// Check if a dice string is valid.
    /// </summary>
    /// <param name="diceString">String to validate</param>
    /// <returns>True if valid</returns>
    public static bool IsValid(string? diceString)
    {
        if (string.IsNullOrEmpty(diceString))
            return false;
            
        return Parse(diceString, out _, out _, out _);
    }
    
    /// <summary>
    /// Calculate the average value of a dice roll.
    /// Useful for AI to evaluate weapon effectiveness.
    /// </summary>
    /// <param name="diceString">Dice notation</param>
    /// <returns>Average value</returns>
    public static int Average(string diceString)
    {
        if (string.IsNullOrEmpty(diceString))
            return 0;
            
        if (!Parse(diceString, out int num, out int faces, out int bias))
            return 0;
        
        // Average of a die is (faces / 2) + 1
        // Example: d6 average = (6/2) + 1 = 4
        // Total average = ((faces / 2) + 1) * num + bias
        return ((faces / 2) + 1) * num + bias;
    }
    
    /// <summary>
    /// Parse dice notation string into components.
    /// Implements Nazghul's state machine parser.
    /// </summary>
    /// <param name="diceString">String like "2d6+3"</param>
    /// <param name="num">Number of dice</param>
    /// <param name="faces">Faces per die</param>
    /// <param name="bias">Modifier to add</param>
    /// <returns>True if parse succeeded</returns>
    private static bool Parse(string diceString, out int num, out int faces, out int bias)
    {
        num = 0;
        faces = 0;
        bias = 0;
        
        if (string.IsNullOrEmpty(diceString))
            return false;
        
        int state = 0;
        int val = 0;
        int sign = 1;
        int i = 0;
        
        while (i < diceString.Length)
        {
            char c = diceString[i];
            
            switch (state)
            {
                case 0: // Initial state - expecting number, +, or -
                    if (c == '+')
                    {
                        sign = 1;
                        state = 4;
                    }
                    else if (c == '-')
                    {
                        sign = -1;
                        state = 4;
                    }
                    else if (char.IsDigit(c))
                    {
                        val = c - '0';
                        if (val != 0)
                            state = 1;
                    }
                    else
                    {
                        return false; // Invalid character
                    }
                    break;
                    
                case 1: // Reading number before 'd'
                    if (c == 'd')
                    {
                        num = val * sign;
                        val = 0;
                        sign = 1;
                        state = 2;
                    }
                    else if (char.IsDigit(c))
                    {
                        val = (val * 10) + (c - '0');
                    }
                    else
                    {
                        return false;
                    }
                    break;
                    
                case 2: // After 'd', expecting faces
                    if (char.IsDigit(c))
                    {
                        val = c - '0';
                        if (val != 0)
                            state = 3;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                    
                case 3: // Reading faces
                    if (char.IsDigit(c))
                    {
                        val = (val * 10) + (c - '0');
                    }
                    else if (c == '+')
                    {
                        faces = val;
                        val = 0;
                        state = 4;
                        sign = 1;
                    }
                    else if (c == '-')
                    {
                        faces = val;
                        val = 0;
                        state = 4;
                        sign = -1;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                    
                case 4: // After + or -, expecting bias
                    if (char.IsDigit(c))
                    {
                        val = c - '0';
                        if (val != 0)
                            state = 5;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                    
                case 5: // Reading bias
                    if (char.IsDigit(c))
                    {
                        val = (val * 10) + (c - '0');
                    }
                    else
                    {
                        return false;
                    }
                    break;
                    
                default:
                    return false; // Invalid state
            }
            
            i++;
        }
        
        // Final state processing
        switch (state)
        {
            case 0:
                // Empty or just whitespace
                break;
                
            case 1:
                // Just a number (bias only, no dice)
                bias = val * sign;
                break;
                
            case 2:
                // Ended right after 'd' - error
                return false;
                
            case 3:
                // Ended after faces
                faces = val * sign;
                break;
                
            case 4:
            case 5:
                // Ended after bias
                bias = val * sign;
                break;
                
            default:
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Set random seed for testing.
    /// </summary>
    public static void SetSeed(int seed)
    {
        random = new Random(seed);
    }
}

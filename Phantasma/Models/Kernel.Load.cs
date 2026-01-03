using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IronScheme;
using IronScheme.Runtime;

namespace Phantasma.Models;

public partial class Kernel
{
    /// <summary>
    /// Load the TinyScheme compatibility layer. Call this once during initialization.
    /// </summary>
    public void LoadCompatibilityLayer()
    {
        // Get the scripts directory from configuration.
        string scriptsDir;
        if (Phantasma.Configuration.TryGetValue("include-dirname", out string dir))
            scriptsDir = dir;
        else
            scriptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        
        string compatPath = Path.Combine(scriptsDir, "tinyscheme-compat.scm");
            
        if (File.Exists(compatPath))
        {
            LoadSchemeFileInternal(compatPath);
        }
        else
        {
            // Define minimal compatibility inline.
            try
            {
                @"(define nil '())".Eval();
                @"(define NIL '())".Eval();
                @"(define t #t)".Eval();
                @"(define f #f)".Eval();
            }
            catch (SchemeException ex)
            {
                var match = Regex.Match(ex.ToString(), @"&irritants: \(([^)]+)\)");
                if (match.Success)
                {
                    Console.Error.WriteLine($"[Scheme] Undefined: {match.Groups[1].Value}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Kernel] Failed to define TinyScheme compatibility: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Load and execute a Scheme file with R5RS compatibility preprocessing.
    /// Continues past errors to evaluate all valid expressions.
    /// </summary>
    public void LoadSchemeFile(string filename)
    {
        if (!File.Exists(filename))
        {
            Console.Error.WriteLine($"[Kernel] Scheme file not found: {filename}");
            return;
        }

        Console.WriteLine($"Loading Scheme file: {filename}");
        var startTime = DateTime.Now;

        string schemeCode = File.ReadAllText(filename);
    
        // Parse, preprocess for R5RS compatibility, and evaluate.
        EvalWithPreprocessing(schemeCode, filename);

        var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
        Console.WriteLine($"{elapsedMs:F0} ms to load {filename}");
    }

    /// <summary>
    /// Load a scheme file without preprocessing (for compatibility layer itself).
    /// </summary>
    private void LoadSchemeFileInternal(string filename)
    {
        Console.WriteLine($"[LoadSchemeFileInternal] Loading: {filename}");
        string schemeCode = File.ReadAllText(filename);
        try 
        {
            EvalTopLevel(schemeCode);
            Console.WriteLine($"[LoadSchemeFileInternal] Success: {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadSchemeFileInternal] ERROR in {filename}: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Parse, preprocess, and evaluate Scheme code.
    /// Handles R5RS internal defines by transforming them to letrec.
    /// Continues past errors to evaluate all valid expressions.
    /// </summary>
    private void EvalWithPreprocessing(string code, string filename = null)
    {
        // Parse into S-expressions.
        SExpressionParser parser;
        List<object> expressions;
        
        try
        {
            parser = new SExpressionParser(code);
            expressions = parser.ParseAll();
        }
        catch (Exception ex)
        {
            // Parsing failed - can't continue.
            Console.Error.WriteLine($"[Kernel] Parse error: {ex.Message}");
            return;
        }
        
        int errorCount = 0;
        var failedFunctions = new HashSet<string>();  // Track what's failing
        
        // Transform and evaluate each expression.
        foreach (var expr in expressions)
        {
            // Skip naz.scm's load redefinition - we provide our own in tinyscheme-compat.scm
            if (SchemePreprocessor.ShouldSkipExpression(expr, filename))
            {
                Console.WriteLine($"[Preprocessor] Skipping naz.scm load redefinition");
                continue;
            }
            
            string exprStr;
            object transformed;
            string funcName = null;
        
            // Extract function name for error tracking.
            if (expr is List<object> list && list.Count > 0 && list[0] is string name)
            {
                funcName = name;
            }
            
            try
            {
                transformed = SchemePreprocessor.TransformExpression(expr);
                exprStr = SchemePreprocessor.SExpressionToString(transformed);
            }
            catch
            {
                transformed = expr;
                exprStr = SchemePreprocessor.SExpressionToString(expr);
            }
            
            if (transformed is List<object> exprList && 
                exprList.Count == 2 && 
                //exprList[0] is string funcName && 
                (funcName == "kern-include" || funcName == "kern-load" || funcName == "load"))
            {
                string nextName = exprList[1]?.ToString() ?? "";
                
                // Strip quotes.
                if (nextName.Length >= 2 && 
                    nextName.StartsWith("\"") && 
                    nextName.EndsWith("\""))
                {
                    nextName = nextName.Substring(1, nextName.Length - 2);
                }
    
                try
                {
                    if (funcName == "load")
                    {
                        // Direct file load - call our C# LoadFile.
                        LoadFile(new object[] { nextName });
                    }
                    else
                    {
                        // kern-include or kern-load - register and optionally load.
                        Include(nextName);
                        if (funcName == "kern-load")
                        {
                            LoadFile(new object[] { nextName });
                        }
                    }
                }
                catch (SchemeException ex)
                {
                    errorCount++;
                    if (funcName != null) failedFunctions.Add(funcName);
    
                    // Extract just the irritant (undefined symbol name).
                    var match = Regex.Match(ex.ToString(), @"&irritants: \(([^)]+)\)");
                    var msgMatch = Regex.Match(ex.ToString(), @"&message: ""([^""]+)""");
    
                    if (match.Success)
                    {
                        string msg = msgMatch.Success ? msgMatch.Groups[1].Value : "error";
                        Console.Error.WriteLine($"[Scheme] {msg}: {match.Groups[1].Value}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"[Scheme] {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (funcName != null)
                        failedFunctions.Add(funcName);
    
                    string preview = exprStr.Length > 60 ? exprStr.Substring(0, 60) + "..." : exprStr;
                    Console.Error.WriteLine($"[Error] {ex.Message}");
                    Console.Error.WriteLine($"  Expression: {preview}");
                }
                
                continue; // Skip Eval - we handled it.
            }
    
            // Normal Evaluation (existing code)
            try
            {
                exprStr.Eval();
            }
            catch (SchemeException ex)
            {
                var match = Regex.Match(ex.ToString(), @"&irritants: \(([^)]+)\)");
                if (match.Success)
                {
                    Console.Error.WriteLine($"[Scheme] Undefined: {match.Groups[1].Value}");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                
                if (funcName != null)
                    failedFunctions.Add(funcName);
                
                string preview = exprStr.Length > 80 
                    ? exprStr.Substring(0, 80) + "..." 
                    : exprStr;
                
                string errorDetail = ex.InnerException?.Message ?? ex.Message;
                
                Console.Error.WriteLine($"[Scheme Error] {errorDetail}");
                Console.Error.WriteLine($"  Expression: {preview}");
            }
        }
        
        if (errorCount > 0)
        {
            string fileInfo = filename != null ? Path.GetFileName(filename) : "unknown";
            string funcList = failedFunctions.Count > 0 
                ? $" ({string.Join(", ", failedFunctions.Take(5))}{(failedFunctions.Count > 5 ? "..." : "")})"
                : "";
        
            Console.Error.WriteLine($"[Kernel] {errorCount} expression(s) failed to evaluate in {fileInfo}{funcList}.");
        }
    }
    
    /// <summary>
    /// Evaluate Scheme code at the top level without preprocessing.
    /// Parses the code into individual expressions and evaluates each one.
    /// Continues past errors to evaluate all valid expressions.
    /// </summary>
    private void EvalTopLevel(string code)
    {
        // Parse into individual top-level expressions.
        SExpressionParser parser;
        List<object> expressions;
        
        try
        {
            parser = new SExpressionParser(code);
            expressions = parser.ParseAll();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Kernel] Parse error: {ex.Message}");
            return;
        }
        
        int errorCount = 0;
        
        // Evaluate each expression individually.
        foreach (var expr in expressions)
        {
            string exprStr = SchemePreprocessor.SExpressionToString(expr);
            try
            {
                exprStr.Eval();
            }
            catch (SchemeException ex)
            {
                errorCount++;
                
                var match = Regex.Match(ex.ToString(), @"&irritants: \(([^)]+)\)");
                if (match.Success)
                {
                    Console.Error.WriteLine($"[Scheme] Undefined: {match.Groups[1].Value}");
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                string preview = exprStr.Length > 80 
                    ? exprStr.Substring(0, 80) + "..." 
                    : exprStr;
    
                // Try to extract detailed error information from IronScheme exceptions
                string errorDetail = ExtractSchemeErrorDetail(ex);
    
                Console.Error.WriteLine($"[Scheme Error] {errorDetail}");
                Console.Error.WriteLine($"  Expression: {preview}");
            }
        }
        
        if (errorCount > 0)
        {
            Console.Error.WriteLine($"[Kernel] {errorCount} expression(s) failed to evaluate");
        }
    }
    
    /// <summary>
    /// Extract detailed error information from IronScheme SchemeExceptions.
    /// IronScheme exceptions often have the real error buried in nested exceptions
    /// or in special properties.
    /// </summary>
    private static string ExtractSchemeErrorDetail(Exception ex)
    {
        var sb = new StringBuilder();
        
        // Start with the base message.
        string message = ex.Message;
        
        // Check for inner exceptions (IronScheme often wraps errors)
        var current = ex;
        int depth = 0;
        while (current != null && depth < 5)
        {
            // Try to get message from the exception.
            if (!string.IsNullOrEmpty(current.Message) && 
                current.Message != "Exception of type 'IronScheme.Runtime.SchemeException' was thrown.")
            {
                message = current.Message;
                break;
            }
            
            // Try to access IronScheme-specific properties via reflection.
            var type = current.GetType();
            
            // Check for 'Who' property (IronScheme puts the function name here).
            var whoProp = type.GetProperty("Who");
            if (whoProp != null)
            {
                var who = whoProp.GetValue(current) as string;
                if (!string.IsNullOrEmpty(who))
                {
                    sb.Append($"[{who}] ");
                }
            }
            
            // Check for 'Message' property override.
            var msgProp = type.GetProperty("Message");
            if (msgProp != null)
            {
                var msg = msgProp.GetValue(current) as string;
                if (!string.IsNullOrEmpty(msg) && msg != message)
                {
                    message = msg;
                }
            }
            
            // Check for 'Irritants' property (the values that caused the error).
            var irritantsProp = type.GetProperty("Irritants");
            if (irritantsProp != null)
            {
                var irritants = irritantsProp.GetValue(current);
                if (irritants != null)
                {
                    sb.Append($"Irritants: {irritants} ");
                }
            }
            
            current = current.InnerException;
            depth++;
        }
        
        // Check exception Data dictionary for additional info.
        foreach (var key in ex.Data.Keys)
        {
            sb.Append($"[{key}: {ex.Data[key]}] ");
        }
        
        // Combine everything.
        if (sb.Length > 0)
        {
            return $"{message} - {sb}";
        }
        
        return message;
    }
}

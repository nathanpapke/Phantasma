using System;
using System.Collections.Generic;
using System.IO;
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
            Console.WriteLine($"Loading TinyScheme compatibility layer: {compatPath}");
            LoadSchemeFileInternal(compatPath);
        }
        else
        {
            // Define minimal compatibility inline.
            Console.WriteLine("Defining minimal TinyScheme compatibility...");
            try
            {
                @"(define nil '())".Eval();
                @"(define NIL '())".Eval();
                @"(define t #t)".Eval();
                @"(define f #f)".Eval();
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
        EvalWithPreprocessing(schemeCode);

        var elapsedMs = (DateTime.Now - startTime).TotalMilliseconds;
        Console.WriteLine($"{elapsedMs:F0} ms to load {filename}");
    }

    /// <summary>
    /// Load a scheme file without preprocessing (for compatibility layer itself).
    /// </summary>
    private void LoadSchemeFileInternal(string filename)
    {
        string schemeCode = File.ReadAllText(filename);
        EvalTopLevel(schemeCode);
    }
    
    /// <summary>
    /// Parse, preprocess, and evaluate Scheme code.
    /// Handles R5RS internal defines by transforming them to letrec.
    /// Continues past errors to evaluate all valid expressions.
    /// </summary>
    private void EvalWithPreprocessing(string code)
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
        
        // Transform and evaluate each expression.
        foreach (var expr in expressions)
        {
            string exprStr;
            
            // Try to transform, fall back to original on failure.
            try
            {
                var transformed = SchemePreprocessor.TransformExpression(expr);
                exprStr = SchemePreprocessor.SExpressionToString(transformed);
            }
            catch
            {
                // Transformation failed - use original expression.
                exprStr = SchemePreprocessor.SExpressionToString(expr);
            }
            
            // Evaluate the expression, log errors but continue.
            try
            {
                exprStr.Eval();
            }
            catch (Exception ex)
            {
                errorCount++;
                string preview = exprStr.Length > 80 
                    ? exprStr.Substring(0, 80) + "..." 
                    : exprStr;
                
                // Try to extract more detail from SchemeExceptions.
                string errorDetail = ex.Message;
                if (ex.InnerException != null)
                    errorDetail = ex.InnerException.Message;
                
                // Check if the exception has additional data.
                if (ex.Data.Count > 0)
                {
                    foreach (var key in ex.Data.Keys)
                    {
                        errorDetail += $" [{key}: {ex.Data[key]}]";
                    }
                }
                
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
            catch (Exception ex)
            {
                errorCount++;
                string preview = exprStr.Length > 80 
                    ? exprStr.Substring(0, 80) + "..." 
                    : exprStr;
                
                // Try to extract more detail from SchemeExceptions.
                string errorDetail = ex.Message;
                if (ex.InnerException != null)
                    errorDetail = ex.InnerException.Message;
                
                Console.Error.WriteLine($"[Scheme Error] {errorDetail}");
                Console.Error.WriteLine($"  Expression: {preview}");
            }
        }
        
        if (errorCount > 0)
        {
            Console.Error.WriteLine($"[Kernel] {errorCount} expression(s) failed to evaluate");
        }
    }
}

// SchemePreprocessor.cs - Transform R5RS internal defines to R6RS-compatible letrec.
//
// In R5RS, you can write:
//   (define (foo x)
//     (do-something)
//     (define (bar y) ...)  ; internal define after expression
//     (bar x))
//
// R6RS requires all defines at the start of a body. This preprocessor
// transforms internal defines into letrec forms which are valid anywhere.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phantasma.Models;

/// <summary>
/// Preprocesses Scheme code to transform R5RS internal defines to R6RS-compatible letrec.
/// </summary>
public class SchemePreprocessor
{
    /// <summary>
    /// Preprocess Scheme code for IronScheme/TinyScheme compatibility.
    /// - Replaces (load ...) with (kern-include ...)
    /// - Transforms internal defines to letrec
    /// </summary>
    public static string Preprocess(string code)
    {
        try
        {
            // Parse into S-expressions.
            var parser = new SExpressionParser(code);
            var expressions = parser.ParseAll();
            
            // Transform each expression (also replaces load with kern-include).
            var transformed = new List<object>();
            foreach (var expr in expressions)
            {
                transformed.Add(TransformExpression(expr));
            }
            
            // Convert back to string.
            var sb = new StringBuilder();
            foreach (var expr in transformed)
            {
                sb.AppendLine(SExpressionToString(expr));
            }
            
            return sb.ToString();
        }
        catch (Exception ex)
        {
            // If preprocessing fails, return original code.
            Console.WriteLine($"[SchemePreprocessor] Warning: {ex.Message}");
            Console.WriteLine("[SchemePreprocessor] Using original code without transformation");
            return code;
        }
    }
    
    /// <summary>
    /// Replace (load "filename") with (kern-include "filename") recursively.
    /// This ensures all file loads go through our path-resolving kern-include.
    /// </summary>
    private static object ReplaceLoadWithKernInclude(object expr)
    {
        if (expr is List<object> list && list.Count >= 1)
        {
            // Check if this is (load ...).
            if (list.Count >= 2 && list[0] is string first && first == "load")
            {
                // Replace with kern-include.
                var newList = new List<object> { "kern-include" };
                for (int i = 1; i < list.Count; i++)
                {
                    newList.Add(ReplaceLoadWithKernInclude(list[i]));
                }
                return newList;
            }
            
            // Recursively process all elements in the list.
            var result = new List<object>();
            foreach (var item in list)
            {
                result.Add(ReplaceLoadWithKernInclude(item));
            }
            return result;
        }
        
        return expr;
    }
    
    /// <summary>
    /// Transform an expression, converting internal defines to letrec where needed.
    /// Also replaces (load ...) with (kern-include ...).
    /// </summary>
    internal static object TransformExpression(object expr)
    {
        // First, replace any load calls with kern-include.
        expr = ReplaceLoadWithKernInclude(expr);
        
        if (expr is not List<object> list || list.Count == 0)
            return expr;
        
        var head = list[0];
        
        // Handle (define (name args...) body...) statement.
        if (head is string s && s == "define" && list.Count >= 3)
        {
            // Check if it's a function definition: (define (name args...) body...)
            if (list[1] is List<object> signature && signature.Count > 0)
            {
                // Transform the body.
                var newBody = TransformBody(list.Skip(2).ToList());
                var result = new List<object> { "define", list[1] };
                result.AddRange(newBody);
                return result;
            }
        }
        
        // Handle (lambda (args...) body...) statement.
        if (head is string s2 && s2 == "lambda" && list.Count >= 3)
        {
            var newBody = TransformBody(list.Skip(2).ToList());
            var result = new List<object> { "lambda", list[1] };
            result.AddRange(newBody);
            return result;
        }
        
        // Handle (let/let*/letrec ((bindings)) body...) statement.
        if (head is string s3 && (s3 == "let" || s3 == "let*" || s3 == "letrec") && list.Count >= 3)
        {
            // Named let: (let name ((bindings)) body...)
            int bodyStart = 2;
            if (list[1] is string) // named let
                bodyStart = 3;
            
            var newBody = TransformBody(list.Skip(bodyStart).ToList());
            var result = list.Take(bodyStart).ToList();
            result.AddRange(newBody);
            return result;
        }
        
        // Recursively transform all sub-expressions.
        var transformed = new List<object>();
        foreach (var item in list)
        {
            transformed.Add(TransformExpression(item));
        }
        return transformed;
    }
    
    /// <summary>
    /// Transform a procedure body, collecting internal defines into a letrec.
    /// </summary>
    private static List<object> TransformBody(List<object> body)
    {
        if (body.Count == 0)
            return body;
        
        // Collect all internal defines and their positions.
        var defines = new List<(int Index, string Name, object Args, List<object> Body)>();
        var expressions = new List<(int Index, object Expr)>();
        
        for (int i = 0; i < body.Count; i++)
        {
            if (body[i] is List<object> item && item.Count >= 3 &&
                item[0] is string head && head == "define")
            {
                // This is an internal define.
                if (item[1] is List<object> signature && signature.Count > 0)
                {
                    // Function definition: (define (name args...) body...)
                    var name = signature[0].ToString();
                    var args = signature.Skip(1).ToList();
                    var defBody = item.Skip(2).ToList();
                    defines.Add((i, name, args, defBody));
                }
                else if (item[1] is string varName)
                {
                    // Variable definition: (define name value)
                    defines.Add((i, varName, null, item.Skip(2).ToList()));
                }
            }
            else
            {
                expressions.Add((i, body[i]));
            }
        }
        
        // If no defines, or all defines are at the start, no transformation needed.
        if (defines.Count == 0)
        {
            // Still need to recursively transform sub-expressions.
            return body.Select(TransformExpression).ToList();
        }
        
        // Check if defines are already at the start (R6RS compliant).
        bool needsTransform = false;
        int lastDefineIndex = -1;
        foreach (var d in defines)
        {
            if (d.Index > lastDefineIndex + 1 && expressions.Any(e => e.Index < d.Index))
            {
                needsTransform = true;
                break;
            }
            lastDefineIndex = d.Index;
        }
        
        if (!needsTransform)
        {
            // Already compliant, just recursively transform.
            return body.Select(TransformExpression).ToList();
        }
        
        // Build letrec bindings.
        var bindings = new List<object>();
        foreach (var def in defines)
        {
            object binding;
            if (def.Args != null)
            {
                // Function: name -> (lambda (args) body)
                var lambda = new List<object> { "lambda", def.Args };
                lambda.AddRange(TransformBody(def.Body));
                binding = new List<object> { def.Name, lambda };
            }
            else
            {
                // Variable: name -> value
                var value = def.Body.Count == 1 ? TransformExpression(def.Body[0]) : 
                            new List<object> { "begin" }.Concat(def.Body.Select(TransformExpression)).ToList();
                binding = new List<object> { def.Name, value };
            }
            bindings.Add(binding);
        }
        
        // Build the letrec body from non-define expressions.
        var letrecBody = expressions.Select(e => TransformExpression(e.Expr)).ToList();
        if (letrecBody.Count == 0)
        {
            letrecBody.Add(new List<object> { "void" }); // Empty body
        }
        
        // Build final letrec.
        var letrec = new List<object> { "letrec", bindings };
        letrec.AddRange(letrecBody);
        
        return new List<object> { letrec };
    }
    
    /// <summary>
    /// Convert an S-expression back to a string.
    /// </summary>
    internal static string SExpressionToString(object expr)
    {
        if (expr == null)
            return "()";
        
        if (expr is string s)
        {
            // If already a quoted string (from ParseString), return as-is.
            if (s.StartsWith("\"") && s.EndsWith("\""))
                return s;
            
            // Check if it needs quoting (contains special characters).
            if (s.Contains(" ") || s.Contains("(") || s.Contains(")") || s.Contains("\n") || s.Contains("\t"))
                return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
            
            return s;
        }
        
        if (expr is List<object> list)
        {
            if (list.Count == 0)
                return "()";
            
            var sb = new StringBuilder();
            sb.Append('(');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(SExpressionToString(list[i]));
            }
            sb.Append(')');
            return sb.ToString();
        }
        
        return expr.ToString();
    }
}

/// <summary>
/// Simple S-expression parser.
/// </summary>
public class SExpressionParser
{
    private readonly string _input;
    private int _pos;
    
    public SExpressionParser(string input)
    {
        _input = input;
        _pos = 0;
    }
    
    public List<object> ParseAll()
    {
        var results = new List<object>();
        
        while (_pos < _input.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _input.Length)
                break;
            
            results.Add(ParseExpression());
        }
        
        return results;
    }
    
    private object ParseExpression()
    {
        SkipWhitespaceAndComments();
        
        if (_pos >= _input.Length)
            throw new Exception("Unexpected end of input");
        
        char c = _input[_pos];
        
        if (c == '(')
            return ParseList();
        
        if (c == '\'')
        {
            _pos++;
            return new List<object> { "quote", ParseExpression() };
        }
        
        if (c == '`')
        {
            _pos++;
            return new List<object> { "quasiquote", ParseExpression() };
        }
        
        if (c == ',')
        {
            _pos++;
            if (_pos < _input.Length && _input[_pos] == '@')
            {
                _pos++;
                return new List<object> { "unquote-splicing", ParseExpression() };
            }
            return new List<object> { "unquote", ParseExpression() };
        }
        
        if (c == '"')
            return ParseString();
        
        if (c == '#')
            return ParseHash();
        
        return ParseAtom();
    }
    
    private List<object> ParseList()
    {
        if (_input[_pos] != '(')
            throw new Exception("Expected '('");
        
        _pos++;
        var items = new List<object>();
        
        while (true)
        {
            SkipWhitespaceAndComments();
            
            if (_pos >= _input.Length)
                throw new Exception("Unexpected end of input in list");
            
            if (_input[_pos] == ')')
            {
                _pos++;
                return items;
            }
            
            // Handle dotted pairs.
            if (_input[_pos] == '.' && _pos + 1 < _input.Length && 
                char.IsWhiteSpace(_input[_pos + 1]))
            {
                _pos++;
                SkipWhitespaceAndComments();
                // For simplicity, just add the cdr as-is (not proper dotted pair handling).
                items.Add(ParseExpression());
                SkipWhitespaceAndComments();
                if (_input[_pos] != ')')
                    throw new Exception("Expected ')' after dotted pair");
                _pos++;
                return items;
            }
            
            items.Add(ParseExpression());
        }
    }
    
    private string ParseString()
    {
        if (_input[_pos] != '"')
            throw new Exception("Expected '\"'");
        
        _pos++;
        var sb = new StringBuilder();
        
        while (_pos < _input.Length && _input[_pos] != '"')
        {
            if (_input[_pos] == '\\' && _pos + 1 < _input.Length)
            {
                _pos++;
                char escaped = _input[_pos];
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    default: sb.Append(escaped); break;
                }
            }
            else
            {
                sb.Append(_input[_pos]);
            }
            _pos++;
        }
        
        if (_pos >= _input.Length)
            throw new Exception("Unterminated string");
        
        _pos++; // Skip closing quote
        
        // Return as a special string marker.
        return $"\"{sb}\"";
    }
    
    private string ParseHash()
    {
        _pos++; // Skip #
        
        if (_pos >= _input.Length)
            return "#";
        
        char c = _input[_pos];
        
        if (c == 't' || c == 'T')
        {
            _pos++;
            return "#t";
        }
        
        if (c == 'f' || c == 'F')
        {
            _pos++;
            return "#f";
        }
        
        if (c == '\\')
        {
            // Character Literal
            _pos++;
            if (_pos >= _input.Length)
                return "#\\";
            
            var sb = new StringBuilder("#\\");
            while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) && 
                   _input[_pos] != ')' && _input[_pos] != '(')
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
            return sb.ToString();
        }
        
        if (c == '(')
        {
            // Vector - parse as list for now
            var list = ParseList();
            return $"#({string.Join(" ", list.Select(SExpressionToString))})";
        }
        
        // Other # syntax - just return it as-is.
        var atom = new StringBuilder("#");
        while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) && 
               _input[_pos] != ')' && _input[_pos] != '(')
        {
            atom.Append(_input[_pos]);
            _pos++;
        }
        return atom.ToString();
    }
    
    private string SExpressionToString(object expr)
    {
        return SchemePreprocessor.SExpressionToString(expr);
    }
    
    private string ParseAtom()
    {
        var sb = new StringBuilder();
        
        while (_pos < _input.Length)
        {
            char c = _input[_pos];
            if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '"' || c == ';')
                break;
            sb.Append(c);
            _pos++;
        }
        
        return sb.ToString();
    }
    
    private void SkipWhitespaceAndComments()
    {
        while (_pos < _input.Length)
        {
            if (char.IsWhiteSpace(_input[_pos]))
            {
                _pos++;
            }
            else if (_input[_pos] == ';')
            {
                // Skip to end of line.
                while (_pos < _input.Length && _input[_pos] != '\n')
                    _pos++;
            }
            else
            {
                break;
            }
        }
    }
}

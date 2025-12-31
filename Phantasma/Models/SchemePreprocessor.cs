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
/// 
/// IMPORTANT: The expression tree only contains string and List&lt;object&gt; types.
/// No custom marker classes are used to maintain compatibility with all consumers.
/// Special cases like dotted pairs use string markers (". " prefix) within lists.
/// Vectors are stored as pre-formatted strings like "#(1 2 3)".
/// </summary>
public class SchemePreprocessor
{
    /// <summary>
    /// Preprocess Scheme code for IronScheme/TinyScheme compatibility.
    /// - Prepends #!fold-case for R5RS case-insensitivity
    /// - Transforms internal defines to letrec
    /// </summary>
    public static string Preprocess(string code)
    {
        try
        {
            // Parse into S-expressions.
            var parser = new SExpressionParser(code);
            var expressions = parser.ParseAll();
            
            // Transform each expression.
            var transformed = new List<object>();
            foreach (var expr in expressions)
            {
                transformed.Add(TransformExpression(expr));
            }
            
            // Convert back to string with #!fold-case prefix for R5RS compatibility.
            var sb = new StringBuilder();
            sb.AppendLine("#!fold-case");  // Enable case-insensitive symbols.
            
            foreach (var expr in transformed)
            {
                var exprStr = SExpressionToString(expr);
                sb.AppendLine(exprStr);
            }
            
            var result = sb.ToString();
            
            // Validate output.
            ValidateOutput(result, code);
            
            return result;
        }
        catch (Exception ex)
        {
            // If preprocessing fails, return original code with fold-case.
            Console.WriteLine($"[SchemePreprocessor] Warning: {ex.Message}");
            Console.WriteLine("[SchemePreprocessor] Using original code with #!fold-case prefix");
            return "#!fold-case\n" + code;
        }
    }
    
    /// <summary>
    /// Validate preprocessed output for common errors.
    /// </summary>
    private static void ValidateOutput(string output, string originalCode)
    {
        // Check for balanced parentheses.
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        
        foreach (char c in output)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }
            
            if (c == '\\' && inString)
            {
                escaped = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            
            if (!inString)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                
                if (depth < 0)
                {
                    Console.WriteLine("[SchemePreprocessor] WARNING: Unbalanced parentheses (extra close paren)");
                    break;
                }
            }
        }
        
        if (depth != 0)
        {
            Console.WriteLine($"[SchemePreprocessor] WARNING: Unbalanced parentheses (depth={depth})");
        }
    }
    
    /// <summary>
    /// Transform an expression, converting internal defines to letrec where needed.
    /// CRITICAL: Quoted forms (quote, quasiquote) are NOT transformed - their contents
    /// must remain exactly as written.
    /// </summary>
    internal static object TransformExpression(object expr)
    {
        // Non-list expressions pass through unchanged (strings, pre-formatted vectors, etc.).
        if (expr is not List<object> list || list.Count == 0)
            return expr;
        
        var head = list[0];
        
        // CRITICAL: Don't transform quoted expressions!
        // The contents of quote and quasiquote must be preserved exactly.
        if (head is string headStr)
        {
            // quote and quasiquote: preserve contents completely unchanged
            if (headStr == "quote" || headStr == "quasiquote")
            {
                // Return the entire form unchanged - do NOT recurse into quoted content
                return expr;
            }
            
            // The unquote and unquote-splicing inside quasiquote: these DO get transformed
            // because they "escape" from the quasiquote context.
            if (headStr == "unquote" || headStr == "unquote-splicing")
            {
                if (list.Count >= 2)
                {
                    return new List<object> { headStr, TransformExpression(list[1]) };
                }
                return expr;
            }
            
            // Handle (define (name args...) body...) - function definition.
            if (headStr == "define" && list.Count >= 3)
            {
                if (list[1] is List<object> signature && signature.Count > 0)
                {
                    // Function definition: transform the body
                    var newBody = TransformBody(list.Skip(2).ToList());
                    var result = new List<object> { "define", signature };
                    result.AddRange(newBody);
                    return result;
                }
                // Variable definition: transform the value expression
                if (list[1] is string varName && list.Count >= 3)
                {
                    var transformedValue = TransformExpression(list[2]);
                    return new List<object> { "define", varName, transformedValue };
                }
            }
            
            // Handle (lambda (args...) body...) statement.
            if (headStr == "lambda" && list.Count >= 3)
            {
                var newBody = TransformBody(list.Skip(2).ToList());
                var result = new List<object> { "lambda", list[1] };
                result.AddRange(newBody);
                return result;
            }
            
            // Handle (let/let*/letrec ((bindings)) body...) statement.
            if ((headStr == "let" || headStr == "let*" || headStr == "letrec") && list.Count >= 3)
            {
                // Named let: (let name ((bindings)) body...)
                int bindingsIndex = 1;
                int bodyStart = 2;
                
                if (list[1] is string) // named let
                {
                    bindingsIndex = 2;
                    bodyStart = 3;
                }
                
                // Transform bindings - each binding's value expression should be transformed.
                var newBindings = TransformBindings(list[bindingsIndex]);
                
                // Transform body.
                var newBody = TransformBody(list.Skip(bodyStart).ToList());
                
                // Rebuild the let form.
                var result = new List<object> { headStr };
                if (list[1] is string letName)
                {
                    result.Add(letName);
                }
                result.Add(newBindings);
                result.AddRange(newBody);
                return result;
            }
            
            // Handle (do ((var init step) ...) (test result ...) body...) statement.
            if (headStr == "do" && list.Count >= 3)
            {
                // Transform do form - bindings, test, and body all need transformation.
                var newBindings = TransformDoBindings(list[1]);
                var newTest = list[2] is List<object> testList 
                    ? testList.Select(TransformExpression).ToList() as object
                    : TransformExpression(list[2]);
                var newBody = list.Skip(3).Select(TransformExpression).ToList();
                
                var result = new List<object> { "do", newBindings, newTest };
                result.AddRange(newBody);
                return result;
            }
            
            // Handle (case-lambda ((args) body...) ...) statement.
            if (headStr == "case-lambda")
            {
                var result = new List<object> { "case-lambda" };
                foreach (var clause in list.Skip(1))
                {
                    if (clause is List<object> clauseList && clauseList.Count >= 2)
                    {
                        var newBody = TransformBody(clauseList.Skip(1).ToList());
                        var newClause = new List<object> { clauseList[0] };
                        newClause.AddRange(newBody);
                        result.Add(newClause);
                    }
                    else
                    {
                        result.Add(clause);
                    }
                }
                return result;
            }
        }
        
        // For all other list forms, recursively transform all sub-expressions.
        var transformed = new List<object>();
        foreach (var item in list)
        {
            transformed.Add(TransformExpression(item));
        }
        return transformed;
    }
    
    /// <summary>
    /// Transform let/let*/letrec bindings.
    /// Each binding is (name value), and the value should be transformed.
    /// </summary>
    private static object TransformBindings(object bindings)
    {
        if (bindings is not List<object> bindingsList)
            return bindings;
        
        var result = new List<object>();
        foreach (var binding in bindingsList)
        {
            if (binding is List<object> bindingPair && bindingPair.Count >= 2)
            {
                var name = bindingPair[0];
                var value = TransformExpression(bindingPair[1]);
                result.Add(new List<object> { name, value });
            }
            else
            {
                result.Add(binding);
            }
        }
        return result;
    }
    
    /// <summary>
    /// Transform do bindings: ((var init step) ...).
    /// </summary>
    private static object TransformDoBindings(object bindings)
    {
        if (bindings is not List<object> bindingsList)
            return bindings;
        
        var result = new List<object>();
        foreach (var binding in bindingsList)
        {
            if (binding is List<object> bindingList)
            {
                var transformed = bindingList.Select(TransformExpression).ToList();
                result.Add(transformed);
            }
            else
            {
                result.Add(binding);
            }
        }
        return result;
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
            if (body[i] is List<object> item && item.Count >= 2 &&
                item[0] is string head && head == "define")
            {
                // This is an internal define.
                if (item[1] is List<object> signature && signature.Count > 0)
                {
                    // Function definition: (define (name args...) body...)
                    var name = signature[0]?.ToString() ?? "";
                    var args = signature.Skip(1).ToList();
                    var defBody = item.Skip(2).ToList();
                    defines.Add((i, name, (object)args, defBody));
                }
                else if (item[1] is string varName)
                {
                    // Variable definition: (define name value) or (define name)
                    var defBody = item.Skip(2).ToList();
                    defines.Add((i, varName, null, defBody));
                }
                else
                {
                    // Unrecognized define form - treat as expression.
                    expressions.Add((i, body[i]));
                }
            }
            else
            {
                expressions.Add((i, body[i]));
            }
        }
        
        // If no defines, just recursively transform sub-expressions.
        if (defines.Count == 0)
        {
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
                object value;
                if (def.Body.Count == 0)
                {
                    // (define name) with no value - use (void) or #f
                    value = new List<object> { "void" };
                }
                else if (def.Body.Count == 1)
                {
                    value = TransformExpression(def.Body[0]);
                }
                else
                {
                    // Multiple expressions - wrap in begin.
                    var begin = new List<object> { "begin" };
                    begin.AddRange(def.Body.Select(TransformExpression));
                    value = begin;
                }
                binding = new List<object> { def.Name, value };
            }
            bindings.Add(binding);
        }
        
        // Build the letrec body from non-define expressions.
        var letrecBody = expressions.Select(e => TransformExpression(e.Expr)).ToList();
        if (letrecBody.Count == 0)
        {
            // Empty body - add (void)
            letrecBody.Add(new List<object> { "void" });
        }
        
        // Build final letrec.
        var letrec = new List<object> { "letrec", bindings };
        letrec.AddRange(letrecBody);
        
        return new List<object> { letrec };
    }
    
    /// <summary>
    /// Convert an S-expression back to a string.
    /// 
    /// The expression tree only contains strings and List&lt;object&gt;.
    /// Special cases:
    /// - Strings starting with "#(" are pre-formatted vectors
    /// - Strings starting with ". " inside lists mark dotted pair cdr
    /// </summary>
    internal static string SExpressionToString(object expr)
    {
        if (expr == null)
            return "()";
        
        if (expr is string s)
        {
            // Pre-formatted special forms (vectors, booleans, chars) - return as-is.
            if (s.StartsWith("#"))
                return s;
            
            // Dotted pair cdr marker - return as-is (includes the ". " prefix).
            if (s.StartsWith(". "))
                return s;
            
            // Quoted strings (from ParseString) - return as-is.
            if (s.StartsWith("\"") && s.EndsWith("\""))
                return s;
            
            // Regular symbols/atoms - return as-is.
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
                var item = list[i];
                
                // Check for dotted pair marker (string starting with ". ").
                if (item is string itemStr && itemStr.StartsWith(". "))
                {
                    // This is the cdr of a dotted pair - output with proper spacing.
                    sb.Append(" ");
                    sb.Append(itemStr);  // Already contains ". " prefix
                    break;  // Nothing should follow the cdr in a dotted pair.
                }
                
                if (i > 0) sb.Append(' ');
                sb.Append(SExpressionToString(item));
            }
            
            sb.Append(')');
            return sb.ToString();
        }
        
        // Fallback for any other type (shouldn't happen, but be safe).
        return expr?.ToString() ?? "()";
    }
    
    /// <summary>
    /// Skip naz.scm's load redefinition - we provide our own in tinyscheme-compat.scm.
    /// </summary>
    public static bool ShouldSkipExpression(object expr, string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;
            
        if (!filename.EndsWith("naz.scm", StringComparison.OrdinalIgnoreCase))
            return false;

        if (expr is not List<object> list || list.Count < 2)
            return false;

        var first = list[0]?.ToString();
        if (first != "define")
            return false;

        var second = list[1];

        // Skip: (define original-load load)
        if (second?.ToString() == "original-load")
            return true;

        // Skip: (define (load ...) ...)
        if (second is List<object> defList && defList.Count > 0 && defList[0]?.ToString() == "load")
            return true;

        return false;
    }
}

/// <summary>
/// Simple S-expression parser.
/// Parses Scheme code into a tree of strings and List&lt;object&gt;.
/// 
/// NOTE: We do NOT lowercase symbols here. Instead, we prepend #!fold-case
/// to the output, which tells IronScheme to handle case-insensitivity natively.
/// This is more correct and avoids issues with quoted data.
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
            
            // Handle dotted pairs: (a b . c).
            if (_input[_pos] == '.' && _pos + 1 < _input.Length && 
                (char.IsWhiteSpace(_input[_pos + 1]) || _input[_pos + 1] == '(' || _input[_pos + 1] == '"'))
            {
                _pos++;  // Skip the dot.
                SkipWhitespaceAndComments();
                
                // Parse the cdr element and create a string marker with ". " prefix.
                var cdr = ParseExpression();
                var cdrStr = SchemePreprocessor.SExpressionToString(cdr);
                items.Add(". " + cdrStr);  // String marker for dotted pair
                
                SkipWhitespaceAndComments();
                if (_pos >= _input.Length || _input[_pos] != ')')
                    throw new Exception("Expected ')' after dotted pair cdr");
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
        
        _pos++; // Skip closing quote.
        
        // Return as a string literal with quotes preserved.
        // We escape internal quotes and backslashes for proper output.
        var content = sb.ToString();
        var escaped_content = content
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
        return $"\"{escaped_content}\"";
    }
    
    private object ParseHash()
    {
        _pos++; // Skip #
        
        if (_pos >= _input.Length)
            return "#";
        
        char c = _input[_pos];
        
        // Boolean Literals
        if (c == 't' || c == 'T')
        {
            _pos++;
            // Check if it's just #t or #true symbol.
            if (_pos < _input.Length && char.IsLetter(_input[_pos]))
            {
                // Could be #true symbol.
                var sb = new StringBuilder("#t");
                while (_pos < _input.Length && char.IsLetter(_input[_pos]))
                {
                    sb.Append(_input[_pos]);
                    _pos++;
                }
                return sb.ToString();
            }
            return "#t";
        }
        
        if (c == 'f' || c == 'F')
        {
            _pos++;
            // Check if it's just #f or #false symbol.
            if (_pos < _input.Length && char.IsLetter(_input[_pos]))
            {
                var sb = new StringBuilder("#f");
                while (_pos < _input.Length && char.IsLetter(_input[_pos]))
                {
                    sb.Append(_input[_pos]);
                    _pos++;
                }
                return sb.ToString();
            }
            return "#f";
        }
        
        // Character literal: #\x or #\space etc.
        if (c == '\\')
        {
            _pos++;
            if (_pos >= _input.Length)
                return "#\\";
            
            var sb = new StringBuilder("#\\");
            // Read until delimiter.
            while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) && 
                   _input[_pos] != ')' && _input[_pos] != '(' && _input[_pos] != '"')
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
            return sb.ToString();
        }
        
        // Vector literal: #(...)
        if (c == '(')
        {
            var elements = ParseList();
            // Return as pre-formatted string - vectors are stored as strings.
            if (elements.Count == 0)
                return "#()";
            var elementsStr = string.Join(" ", elements.Select(SchemePreprocessor.SExpressionToString));
            return $"#({elementsStr})";
        }
        
        // Reader directives: #!fold-case, #!r6rs, etc.
        if (c == '!')
        {
            _pos++;
            var sb = new StringBuilder("#!");
            while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) &&
                   _input[_pos] != ')' && _input[_pos] != '(')
            {
                sb.Append(_input[_pos]);
                _pos++;
            }
            return sb.ToString();
        }
        
        // Other # syntax - read as atom.
        var atom = new StringBuilder("#");
        while (_pos < _input.Length && !char.IsWhiteSpace(_input[_pos]) && 
               _input[_pos] != ')' && _input[_pos] != '(' && _input[_pos] != '"')
        {
            atom.Append(_input[_pos]);
            _pos++;
        }
        return atom.ToString();
    }
    
    private string ParseAtom()
    {
        var sb = new StringBuilder();
    
        while (_pos < _input.Length)
        {
            char c = _input[_pos];
            if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '"' || c == ';' || c == '\'' || c == '`' || c == ',')
                break;
            sb.Append(c);
            _pos++;
        }
        
        // Lowercase all symbols for R5RS case-insensitivity.
        // This is REQUIRED for C# interop because:
        // 1. C# dictionaries (object registries) are case-sensitive
        // 2. Scheme code may reference t_A as t_a or T_A
        // 3. Both the definition and reference must use the same case
        // 
        // The #!fold-case directive handles IronScheme's internal symbol matching,
        // but C# lookups still need consistent casing.
        return sb.ToString().ToLowerInvariant();
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
                // Line comment - skip to end of line.
                while (_pos < _input.Length && _input[_pos] != '\n')
                    _pos++;
            }
            else if (_pos + 1 < _input.Length && _input[_pos] == '#' && _input[_pos + 1] == '|')
            {
                // Block comment #| ... |# - handle nesting.
                _pos += 2;
                int depth = 1;
                while (_pos + 1 < _input.Length && depth > 0)
                {
                    if (_input[_pos] == '#' && _input[_pos + 1] == '|')
                    {
                        depth++;
                        _pos += 2;
                    }
                    else if (_input[_pos] == '|' && _input[_pos + 1] == '#')
                    {
                        depth--;
                        _pos += 2;
                    }
                    else
                    {
                        _pos++;
                    }
                }
            }
            else
            {
                break;
            }
        }
    }
}

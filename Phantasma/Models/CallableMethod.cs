using System;
using System.Reflection;
using IronScheme.Runtime;

namespace Phantasma;

/// <summary>
/// A Callable that wraps a C# static method.
/// This makes the method fully compatible with IronScheme's apply.
/// </summary>
public class CallableMethod : Callable
{
    private readonly MethodInfo _method;
    private readonly string _name;
    private readonly int _arity;

    public CallableMethod(MethodInfo method, string schemeName)
    {
        _method = method;
        _name = schemeName;
        _arity = method.GetParameters().Length;
    }

    public override object Arity => _arity;
        
    public override object Form => _name;
    
    public override object Call() 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(0 args) invoked");
        return Call(Array.Empty<object>());
    }
    
    public override object Call(object a) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(1 arg) invoked");
        return Call(new[] { a });
    }
    
    public override object Call(object a, object b) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(2 args) invoked");
        return Call(new[] { a, b });
    }
    
    public override object Call(object a, object b, object c) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(3 args) invoked");
        return Call(new[] { a, b, c });
    }
    
    public override object Call(object a, object b, object c, object d) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(4 args) invoked");
        return Call(new[] { a, b, c, d });
    }
    
    public override object Call(object a, object b, object c, object d, object e) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(5 args) invoked");
        return Call(new[] { a, b, c, d, e });
    }
    
    public override object Call(object a, object b, object c, object d, object e, object f) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(6 args) invoked");
        return Call(new[] { a, b, c, d, e, f });
    }
    
    public override object Call(object a, object b, object c, object d, object e, object f, object g) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(7 args) invoked");
        return Call(new[] { a, b, c, d, e, f, g });
    }
    
    public override object Call(object a, object b, object c, object d, object e, object f, object g, object h) 
    {
        Console.WriteLine($"[CallableMethod] {_name}: Call(8 args) invoked");
        return Call(new[] { a, b, c, d, e, f, g, h });
    }
    
    public override object Call(object[] args)
    {
        var parameters = _method.GetParameters();
        
        Console.WriteLine($"[CallableMethod] {_name}: Call(object[]) invoked, args.Length={args.Length}");
        
        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(object[]))
        {
            Console.WriteLine($"[CallableMethod] {_name}: Using OLD-STYLE (wrapping args)");
            return _method.Invoke(null, new object[] { args });
        }
        else
        {
            Console.WriteLine($"[CallableMethod] {_name}: Using NEW-STYLE (direct args), params.Length={parameters.Length}");
            return _method.Invoke(null, args);
        }
    }
    
    public override string ToString() => $"#<procedure {_name}>";
}

namespace Horus.Modules.Core.Application.Common.Helpers;

public static class StringExtensions
{
    public static string ToLowerFirstChar(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
    
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    // public static string Pascalize(this string str)
    // {
    //     if (string.IsNullOrEmpty(str)) return str;
    //     return char.ToUpperInvariant(str[0]) + str[1..];
    // }
}


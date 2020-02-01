using System.Reflection;
using System.Text.RegularExpressions;

public static bool IsConfigured(this string value) => !string.IsNullOrWhiteSpace(value);

public static string Redact(this string value) => value != null ? "****" : value;

public static Dictionary<string, string> ToEnvVars(this Builder build) =>
    _buildEnvVars ??= build.ToTokens()
        .Where(entry => BuildEnvVarsRegex.IsMatch(entry.Key))
        .ToDictionary(entry => entry.Key.Replace(".", "_").ToUpper(), entry => entry.Value.ToString());

public static Dictionary<string, object> ToTokens(this Builder build) =>
    _buildTokens ??= build.ToTokens("Build")
        .ToDictionary(entry => entry.Key, entry => entry.Value);

public static IEnumerable<KeyValuePair<string, object>> ToTokens(this object obj, string prefix = null)
{
    static IEnumerable<IEnumerable<KeyValuePair<string, object>>> GetTokens(string key, object value)
    {
        if (value == null || IsLeafType(value.GetType()) ||
            (value is Array emptyArray && emptyArray.Length == 0) ||
            (value is List<string> emptyStringList && emptyStringList.Count == 0) ||
            (value is Dictionary<string, string> emptyStringDict && emptyStringDict.Count == 0))
        {
            yield return Enumerable.Repeat(new KeyValuePair<string, object>(key, value), 1);
        }
        else if (value is Array array) // unroll array tokens
        {
            for (var i = 0; i < array.Length; i++)
            {
                yield return GetTokens($"{key}[{i}]", array.GetValue(i)).SelectMany(tokens => tokens);
            }
        }
        else if (value is List<string> stringList) // unroll string list tokens
        {
            for (var i = 0; i < stringList.Count; i++)
            {
                yield return GetTokens($"{key}[{i}]", stringList[i]).SelectMany(tokens => tokens);
            }
        }
        else if (value is Dictionary<string, string> stringDict) // unroll string dictionary tokens
        {
            foreach (var entry in stringDict)
            {
                yield return GetTokens($"{key}['{entry.Key}']", entry.Value).SelectMany(tokens => tokens);
            }
        }
        else
        {
            yield return value.ToTokens(key);
        }
    }

    object GetValue(PropertyInfo property)
    {
        try
        {
            return property.GetValue(obj);
        }
        catch (TargetInvocationException) // when (ex.InnerException is ArgumentException || ex.InnerException is FormatException || ex.InnerException is TypeInitializationException)
        {
            return null; // ignore
        }
    }

    static bool IsLeafType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type == typeof(byte[]) || // extended primitives
            type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(Uri) ||
            type == typeof(DirectoryPath) || type == typeof(FilePath) || // cake primitives
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) && // list
                type.GetGenericArguments()[0] != typeof(string)) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) && // dictionary
                (type.GetGenericArguments()[0] != typeof(string) || type.GetGenericArguments()[1] != typeof(string)));
    }

    return obj?.GetType()
        .GetProperties()
        .Where(property => property.GetIndexParameters().Length == 0) // filter indexed properties
        .SelectMany(property => // flatten nested properties
        {
            var key = prefix != null ? $"{prefix}.{property.Name}" : property.Name;
            return GetTokens(key, GetValue(property)).SelectMany(tokens => tokens); // flatten nested tokens
        });
}

public static string ToValueString(this object value)
{
    var type = value?.GetType();
    return value switch
    {
        null => "(null)",
        bool boolean => boolean.ToString().ToLower(),
        string { Length: 0 } => @"""""",
        Array { Length: 0 } => "[]",
        List<string> { Count: 0 } => "[]",
        Dictionary<string, string> { Count: 0 } => "{}",
        _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) => "[...]", // roll-up list
        _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) => "{...}", // roll-up dictionary
        _ => value.ToString()
    };
}

public static string TrimTrailingWhitespace(this string value) => TrailingWhitespaceRegex.Replace(value, "$1");

private static readonly Regex BuildEnvVarsRegex = new Regex(@"^Build\.(?:(?:Parameters\.(?:Configuration|Title))|Version\.)", RegexOptions.Compiled);
private static readonly Regex TrailingWhitespaceRegex = new Regex(@"[ \t]+(\r?\n|$)", RegexOptions.Compiled | RegexOptions.Multiline);

private static Dictionary<string, string> _buildEnvVars;
private static Dictionary<string, object> _buildTokens;

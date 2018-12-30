using Cake.Core.Text;
using System.Reflection;

public static bool IsConfigured(this string value) => !string.IsNullOrWhiteSpace(value);

public static IEnumerable<KeyValuePair<string, object>> ToTokens(this object obj, string prefix = null)
{
    object GetValue(PropertyInfo property)
    {
        try
        {
            return property.GetValue(obj);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException || ex.InnerException is FormatException)
        {
            return null; // ignore
        }
    }

    bool IsLeafType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || (type == typeof(decimal)) || (type == typeof(string)) || (type == typeof(byte[])) || // extended primitives
            (type == typeof(DateTime)) || (type == typeof(DateTimeOffset)) || (type == typeof(TimeSpan)) || (type == typeof(Guid)) || (type == typeof(Uri)) ||
            (type == typeof(DirectoryPath)) || (type == typeof(FilePath)) || // cake primitives
            type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) || // arrays and enumerables
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)); // dictionaries
    }

    return obj?.GetType()
        .GetProperties()
        .Where(property => property.GetIndexParameters().Length == 0) // filter indexed properties
        .SelectMany(property => // flatten nested properties
        {
            var value = GetValue(property);
            return value == null || IsLeafType(value.GetType())
                ? Enumerable.Repeat(new KeyValuePair<string, object>(string.Concat(prefix, property.Name), value), 1)
                : value.ToTokens(string.Concat(prefix, property.Name, ".")); // recursive
        });
}

public static string ToTokenString(this object value)
{
    switch (value)
    {
        case bool boolean:
            return boolean.ToString().ToLower();
        case string str when str.Length == 0: // empty string
            return @"""";
        case string[] strings when strings.Length == 0: // empty string array
            return "[]";
        case string[] strings: // flatten string array
            return string.Concat("[ ", string.Join(", ", strings), " ]");
        case IDictionary<string, string> dict when dict.Count == 0: // empty string dictionary
            return "{}";
        case IDictionary<string, string> dict: // flatten string dictionary
            return string.Concat("{ ", string.Join(", ", dict.Select(x => string.Concat(x.Key, ": ", x.Value))), " }");
        case null:
            return "(null)";
        default:
            var type = value.GetType();
            return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ? "[...]" // roll-up
                : type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ? "{...}"
                : value.ToString(); // default
    }
}

public static TextTransformation<TTemplate> WithTokens<TTemplate>(
    this TextTransformation<TTemplate> transformation, IEnumerable<KeyValuePair<string, object>> tokens)
    where TTemplate : class, ITextTransformationTemplate
{
    if (transformation != null && tokens != null)
    {
        foreach (var token in tokens)
        {
            if (token.Value != null)
            {
                transformation.Template.Register(token.Key, token.Value);
            }
        }
    }
    return transformation;
}

public static void WithVerbosity(this ICakeLog log, Verbosity verbosity, Action action)
{
    var lastVerbosity = log.Verbosity;
    try
    {
        log.Verbosity = verbosity;
        action();
    }
    finally
    {
        log.Verbosity = lastVerbosity;
    }
}

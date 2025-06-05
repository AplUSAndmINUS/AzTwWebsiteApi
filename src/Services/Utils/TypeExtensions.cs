using System;

namespace AzTwWebsiteApi.Services.Utils;

public static class TypeExtensions
{
    public static object? GetDefault(this Type type)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}

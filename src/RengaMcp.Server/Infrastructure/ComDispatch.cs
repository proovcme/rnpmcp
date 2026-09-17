using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RengaMcp.Infrastructure;

internal static class ComDispatch
{
    private const BindingFlags InstancePublic = BindingFlags.Instance | BindingFlags.Public;

    public static object? Get(object target, string propertyName) =>
        target.GetType().InvokeMember(
            propertyName,
            InstancePublic | BindingFlags.GetProperty,
            binder: null,
            target,
            args: null,
            CultureInfo.InvariantCulture);

    public static T Get<T>(object target, string propertyName)
    {
        var value = Get(target, propertyName);
        return ConvertValue<T>(value, propertyName);
    }

    public static T GetOrDefault<T>(object target, string propertyName, T fallback)
    {
        try
        {
            var value = Get(target, propertyName);
            return value is null ? fallback : ConvertValue<T>(value, propertyName);
        }
        catch (COMException exception) when ((uint)exception.HResult == 0x80020006)
        {
            return fallback;
        }
    }

    public static void Set(object target, string propertyName, object? value) =>
        target.GetType().InvokeMember(
            propertyName,
            InstancePublic | BindingFlags.SetProperty,
            binder: null,
            target,
            [value],
            CultureInfo.InvariantCulture);

    public static object? Call(object target, string methodName, params object?[] arguments) =>
        target.GetType().InvokeMember(
            methodName,
            InstancePublic | BindingFlags.InvokeMethod,
            binder: null,
            target,
            arguments,
            CultureInfo.InvariantCulture);

    public static T Call<T>(object target, string methodName, params object?[] arguments)
    {
        var value = Call(target, methodName, arguments);
        return ConvertValue<T>(value, methodName);
    }

    public static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }

    private static T ConvertValue<T>(object? value, string memberName)
    {
        if (value is T typed)
        {
            return typed;
        }

        if (value is null)
        {
            throw new InvalidOperationException($"Renga COM member '{memberName}' returned null.");
        }

        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
    }
}

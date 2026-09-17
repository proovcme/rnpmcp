using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using RengaMcp.Models;

namespace RengaMcp.Infrastructure;

public static class RunningObjectTable
{
    private const string HumanReadablePrefix = "!Renga Application";

    public static IReadOnlyList<RengaInstanceInfo> ListRengaInstances()
    {
        return EnumerateDisplayNames()
            .Where(displayName => displayName.StartsWith(HumanReadablePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(Parse)
            .OrderBy(entry => entry.ProcessId)
            .ToArray();
    }

    public static object GetRengaApplication(int? processId)
    {
        IRunningObjectTable? table = null;
        IEnumMoniker? enumerator = null;

        try
        {
            Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out table));
            table.EnumRunning(out enumerator);
            enumerator.Reset();

            var monikers = new IMoniker[1];
            while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
            {
                var moniker = monikers[0];
                var displayName = GetDisplayName(moniker);
                if (!displayName.StartsWith(HumanReadablePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    ComDispatch.Release(moniker);
                    continue;
                }

                var info = Parse(displayName);
                if (processId is not null && info.ProcessId != processId)
                {
                    ComDispatch.Release(moniker);
                    continue;
                }

                table.GetObject(moniker, out var application);
                ComDispatch.Release(moniker);
                return application;
            }
        }
        finally
        {
            ComDispatch.Release(enumerator);
            ComDispatch.Release(table);
        }

        var suffix = processId is null ? string.Empty : $" with PID {processId}";
        throw new InvalidOperationException($"No running Renga instance{suffix} was found.");
    }

    private static List<string> EnumerateDisplayNames()
    {
        var result = new List<string>();
        IRunningObjectTable? table = null;
        IEnumMoniker? enumerator = null;

        try
        {
            Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out table));
            table.EnumRunning(out enumerator);
            enumerator.Reset();

            var monikers = new IMoniker[1];
            while (enumerator.Next(1, monikers, IntPtr.Zero) == 0)
            {
                var moniker = monikers[0];
                result.Add(GetDisplayName(moniker));
                ComDispatch.Release(moniker);
            }

            return result;
        }
        finally
        {
            ComDispatch.Release(enumerator);
            ComDispatch.Release(table);
        }
    }

    private static string GetDisplayName(IMoniker moniker)
    {
        IBindCtx? context = null;
        try
        {
            Marshal.ThrowExceptionForHR(CreateBindCtx(0, out context));
            moniker.GetDisplayName(context, null, out var displayName);
            return displayName;
        }
        finally
        {
            ComDispatch.Release(context);
        }
    }

    public static RengaInstanceInfo Parse(string moniker)
    {
        string apiVersion = string.Empty;
        int? processId = null;

        foreach (var part in moniker.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("ver:", StringComparison.OrdinalIgnoreCase))
            {
                apiVersion = trimmed[4..].Trim();
            }
            else if (trimmed.StartsWith("pid:", StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(trimmed[4..].Trim(), out var parsedPid))
            {
                processId = parsedPid;
            }
        }

        return new RengaInstanceInfo(processId, apiVersion, moniker);
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(
        int reserved,
        [MarshalAs(UnmanagedType.Interface)] out IRunningObjectTable runningObjectTable);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(
        int reserved,
        [MarshalAs(UnmanagedType.Interface)] out IBindCtx bindContext);
}

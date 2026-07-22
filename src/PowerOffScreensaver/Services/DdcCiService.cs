using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PowerOffScreensaver.Services;

/// <summary>
/// Real DDC/CI (VESA MCCS) power control via dxva2.dll. Each physical monitor is
/// addressed directly through VCP code 0xD6 ("Power Mode"), so power-off works
/// per panel regardless of how the GPU driver handles global DPMS. This is what
/// makes the second/third monitor actually turn off on NVIDIA/AMD rigs.
/// </summary>
public sealed class DdcCiService : IDdcCiService
{
    private const byte VCP_POWER = 0xD6;
    private const uint POWER_ON = 1;   // 0x01 On
    // 0x02 Standby (not 0x04 Off): Standby blanks the panel but keeps the DDC/CI
    // bus alive, so the monitor can be woken again by a DDC/CI "On" or DPMS. Hard
    // Off (0x04/0x05) can kill the DDC controller and strand the monitor until it
    // is physically power-cycled.
    private const uint POWER_OFF = 2;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint dwArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(
        IntPtr hMonitor, byte bVCPCode, out uint pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    /// <summary>
    /// Enumerate physical monitors together with their screen rectangle, so the test
    /// can place windows on the right monitor and number them. Index matches Probe()
    /// / PowerOne().
    /// </summary>
    public IReadOnlyList<MonitorInfo> Inventory()
    {
        var result = new List<MonitorInfo>();
        List<(IntPtr hmon, Rectangle bounds)> monitors;
        try { monitors = EnumerateHMonitorsWithBounds(); }
        catch { return result; }

        int index = 0;
        foreach (var (hmon, bounds) in monitors)
        {
            PHYSICAL_MONITOR[]? arr = null;
            uint n = 0;
            try
            {
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hmon, out n) || n == 0)
                    continue;
                arr = new PHYSICAL_MONITOR[n];
                if (!GetPhysicalMonitorsFromHMONITOR(hmon, n, arr)) { arr = null; continue; }

                foreach (var pm in arr)
                {
                    bool supports;
                    DdcPowerState state = DdcPowerState.Unknown;
                    try
                    {
                        supports = GetVCPFeatureAndVCPFeatureReply(pm.hPhysicalMonitor, VCP_POWER, out _, out uint cur, out _);
                        if (supports)
                            state = cur switch { 1 => DdcPowerState.On, 2 or 3 or 4 or 5 => DdcPowerState.Off, _ => DdcPowerState.Other };
                    }
                    catch { supports = false; }

                    result.Add(new MonitorInfo(index, Describe(pm.szPhysicalMonitorDescription, index + 1), bounds, supports, state));
                    index++;
                }
            }
            catch { }
            finally
            {
                if (arr != null) { try { DestroyPhysicalMonitors(n, arr); } catch { } }
            }
        }
        return result;
    }

    private List<(IntPtr, Rectangle)> EnumerateHMonitorsWithBounds()
    {
        var list = new List<(IntPtr, Rectangle)>();
        MonitorEnumProc proc = (h, _, lprc, _) =>
        {
            var r = Marshal.PtrToStructure<RECT>(lprc);
            list.Add((h, Rectangle.FromLTRB(r.left, r.top, r.right, r.bottom)));
            return true;
        };
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        GC.KeepAlive(proc);
        return list;
    }

    public DdcResult PowerAll(bool on) => SetPower(on, onlyIndex: null);

    /// <summary>Set power on a single physical monitor, addressed by its Probe() index.</summary>
    public DdcResult PowerOne(int index, bool on) => SetPower(on, onlyIndex: index);

    private DdcResult SetPower(bool on, int? onlyIndex)
    {
        uint value = on ? POWER_ON : POWER_OFF;
        return ForEachPhysical((pm, _) =>
        {
            try { return SetVCPFeature(pm.hPhysicalMonitor, VCP_POWER, value); }
            catch { return false; }
        }, onlyIndex);
    }

    public IReadOnlyList<MonitorProbe> Probe()
    {
        var probes = new List<MonitorProbe>();
        ForEachPhysical((pm, idx) =>
        {
            bool supports;
            DdcPowerState state = DdcPowerState.Unknown;
            try
            {
                supports = GetVCPFeatureAndVCPFeatureReply(
                    pm.hPhysicalMonitor, VCP_POWER, out _, out uint cur, out _);
                if (supports)
                {
                    state = cur switch
                    {
                        1 => DdcPowerState.On,
                        2 or 3 or 4 or 5 => DdcPowerState.Off, // standby/suspend/off = not on
                        _ => DdcPowerState.Other
                    };
                }
            }
            catch { supports = false; }

            probes.Add(new MonitorProbe(idx, Describe(pm.szPhysicalMonitorDescription, idx + 1), supports, state));
            return supports;
        }, null);
        return probes;
    }

    /// <summary>Probe a single physical monitor by its Probe() index.</summary>
    public MonitorProbe? ProbeOne(int index)
    {
        foreach (var p in Probe())
            if (p.Index == index) return p;
        return null;
    }

    private static string Describe(string? raw, int index) =>
        string.IsNullOrWhiteSpace(raw) ? $"Monitor {index}" : raw.Trim();

    /// <summary>
    /// Enumerates every physical monitor, runs <paramref name="action"/> on each,
    /// and always releases the handles. Any native failure is swallowed so one bad
    /// monitor never aborts the sweep.
    /// </summary>
    private DdcResult ForEachPhysical(Func<PHYSICAL_MONITOR, int, bool> action, int? onlyIndex)
    {
        int total = 0, ok = 0, index = 0;
        List<IntPtr> hmonitors;
        try
        {
            hmonitors = EnumerateHMonitors();
        }
        catch
        {
            return new DdcResult(0, 0);
        }

        foreach (var hmon in hmonitors)
        {
            PHYSICAL_MONITOR[]? arr = null;
            uint n = 0;
            try
            {
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hmon, out n) || n == 0)
                    continue;
                arr = new PHYSICAL_MONITOR[n];
                if (!GetPhysicalMonitorsFromHMONITOR(hmon, n, arr))
                {
                    arr = null;
                    continue;
                }

                foreach (var pm in arr)
                {
                    int current = index++;
                    if (onlyIndex.HasValue && onlyIndex.Value != current) continue;
                    total++;
                    if (action(pm, current)) ok++;
                }
            }
            catch
            {
                // ignore this monitor
            }
            finally
            {
                if (arr != null)
                {
                    try { DestroyPhysicalMonitors(n, arr); } catch { }
                }
            }
        }

        return new DdcResult(total, ok);
    }

    private List<IntPtr> EnumerateHMonitors()
    {
        var list = new List<IntPtr>();
        MonitorEnumProc proc = (h, _, _, _) => { list.Add(h); return true; };
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
        GC.KeepAlive(proc);
        return list;
    }
}

/// <summary>No-op DDC/CI service for environments without display hardware.</summary>
public sealed class NullDdcCiService : IDdcCiService
{
    public IReadOnlyList<MonitorProbe> Probe() => Array.Empty<MonitorProbe>();
    public IReadOnlyList<MonitorInfo> Inventory() => Array.Empty<MonitorInfo>();
    public MonitorProbe? ProbeOne(int index) => null;
    public DdcResult PowerAll(bool on) => new(0, 0);
    public DdcResult PowerOne(int index, bool on) => new(0, 0);
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ZoyaRadio.MediaProbe;

internal sealed record AudioSessionEnumeration(
    string EndpointTag,
    IReadOnlyList<AudioSessionDescriptor> Sessions);

internal static class WindowsAudioSessionCatalog
{
    private const uint ClsCtxAll = 23;
    private static readonly Guid MmDeviceEnumeratorClassId = new("BCDE0395-E52F-467C-8E3D-C4579291692E");

    public static AudioSessionEnumeration Enumerate(IAnnouncementLog log)
    {
        object? deviceEnumeratorObject = null;
        IMMDevice? device = null;
        object? sessionManagerObject = null;
        IAudioSessionEnumerator? sessionEnumerator = null;

        try
        {
            Type enumeratorType = Type.GetTypeFromCLSID(MmDeviceEnumeratorClassId, throwOnError: true)!;
            deviceEnumeratorObject = Activator.CreateInstance(enumeratorType)
                ?? throw new InvalidOperationException("Windows did not create the audio-device enumerator.");
            var deviceEnumerator = (IMMDeviceEnumerator)deviceEnumeratorObject;

            CheckHResult(
                deviceEnumerator.GetDefaultAudioEndpoint(
                    EDataFlow.Render,
                    ERole.Multimedia,
                    out device),
                "get the default multimedia render device");

            string endpointId = ReadAllocatedString(device.GetId, "read the default audio endpoint identifier");
            string endpointTag = CreateSafeTag(endpointId);

            Guid managerInterfaceId = typeof(IAudioSessionManager2).GUID;
            CheckHResult(
                device.Activate(
                    ref managerInterfaceId,
                    ClsCtxAll,
                    IntPtr.Zero,
                    out sessionManagerObject),
                "activate the Windows audio-session manager");

            var sessionManager = (IAudioSessionManager2)sessionManagerObject;
            CheckHResult(
                sessionManager.GetSessionEnumerator(out sessionEnumerator),
                "enumerate Windows audio sessions");
            CheckHResult(sessionEnumerator.GetCount(out int count), "count Windows audio sessions");

            var sessions = new List<AudioSessionDescriptor>(count);

            for (int index = 0; index < count; index++)
            {
                IAudioSessionControl? baseControl = null;

                try
                {
                    CheckHResult(
                        sessionEnumerator.GetSession(index, out baseControl),
                        $"open audio session {index + 1}");

                    var control = (IAudioSessionControl2)baseControl;
                    ISimpleAudioVolume? volume = baseControl as ISimpleAudioVolume;

                    CheckHResult(control.GetProcessId(out uint processIdValue), "read an audio-session process ID");
                    int? processId = processIdValue == 0 || processIdValue > int.MaxValue
                        ? null
                        : (int)processIdValue;
                    string? processName = ResolveProcessName(processId);

                    string sessionIdentifier = TryReadAllocatedString(
                        control.GetSessionIdentifier,
                        fallback: $"session-{index}");
                    string sessionInstanceIdentifier = TryReadAllocatedString(
                        control.GetSessionInstanceIdentifier,
                        fallback: sessionIdentifier);
                    string stableTag = CreateSafeTag(sessionInstanceIdentifier);

                    var target = new WindowsAudioSessionVolumeTarget(
                        baseControl,
                        control,
                        volume,
                        stableTag,
                        processId,
                        processName);
                    baseControl = null;
                    sessions.Add(new AudioSessionDescriptor(target, target.ReadState()));
                }
                catch (Exception exception)
                {
                    log.Warning($"Could not inspect Windows audio session {index + 1}; continuing", exception);
                    ReleaseComObject(baseControl);
                }
            }

            return new AudioSessionEnumeration(endpointTag, sessions);
        }
        finally
        {
            ReleaseComObject(sessionEnumerator);
            ReleaseComObject(sessionManagerObject);
            ReleaseComObject(device);
            ReleaseComObject(deviceEnumeratorObject);
        }
    }

    private static string? ResolveProcessName(int? processId)
    {
        if (processId is null)
        {
            return null;
        }

        try
        {
            using Process process = Process.GetProcessById(processId.Value);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    private static string ReadAllocatedString(
        AllocatedStringMethod method,
        string operation)
    {
        IntPtr pointer = IntPtr.Zero;

        try
        {
            CheckHResult(method(out pointer), operation);
            return Marshal.PtrToStringUni(pointer) ?? string.Empty;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }
    }

    private static string TryReadAllocatedString(
        AllocatedStringMethod method,
        string fallback)
    {
        try
        {
            string value = ReadAllocatedString(method, "read an audio-session identifier");
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private static string CreateSafeTag(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 6));
    }

    private static void CheckHResult(int hresult, string operation)
    {
        if (hresult < 0)
        {
            throw new COMException($"Windows could not {operation}.", hresult);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is null || !Marshal.IsComObject(value))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(value);
        }
        catch
        {
            // The process is short-lived; a failed best-effort COM release is not a reason to skip restoration.
        }
    }

    private delegate int AllocatedStringMethod(out IntPtr value);
}

internal sealed class WindowsAudioSessionVolumeTarget : IAudioSessionVolumeTarget
{
    private readonly IAudioSessionControl _baseControl;
    private readonly IAudioSessionControl2 _control;
    private readonly ISimpleAudioVolume? _volume;
    private bool _disposed;

    public WindowsAudioSessionVolumeTarget(
        IAudioSessionControl baseControl,
        IAudioSessionControl2 control,
        ISimpleAudioVolume? volume,
        string stableSessionTag,
        int? processId,
        string? processName)
    {
        _baseControl = baseControl;
        _control = control;
        _volume = volume;
        StableSessionTag = stableSessionTag;
        ProcessId = processId;
        ProcessName = processName;
    }

    public string StableSessionTag { get; }

    public int? ProcessId { get; }

    public string? ProcessName { get; }

    public AudioSessionVolumeState ReadState()
    {
        if (_disposed)
        {
            return new AudioSessionVolumeState(
                false,
                AudioSessionActivity.Unknown,
                0f,
                false,
                "The original audio-session handle has already been released.");
        }

        try
        {
            WindowsAudioSessionCatalogInterop.CheckHResult(
                _control.GetState(out AudioSessionActivity activity),
                "read the audio-session state");

            if (_volume is null)
            {
                return new AudioSessionVolumeState(
                    false,
                    activity,
                    0f,
                    false,
                    "Windows did not expose ISimpleAudioVolume for this session.");
            }

            WindowsAudioSessionCatalogInterop.CheckHResult(
                _volume.GetMasterVolume(out float volumeScalar),
                "read the per-session volume");
            WindowsAudioSessionCatalogInterop.CheckHResult(
                _volume.GetMute(out bool muted),
                "read the per-session mute state");

            return new AudioSessionVolumeState(true, activity, volumeScalar, muted);
        }
        catch (Exception exception)
        {
            return new AudioSessionVolumeState(
                false,
                AudioSessionActivity.Unknown,
                0f,
                false,
                exception.Message);
        }
    }

    public void SetVolume(float volumeScalar, Guid eventContext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ISimpleAudioVolume volume = _volume
            ?? throw new InvalidOperationException("Windows did not expose per-session volume control.");
        WindowsAudioSessionCatalogInterop.CheckHResult(
            volume.SetMasterVolume(Math.Clamp(volumeScalar, 0f, 1f), ref eventContext),
            "set the selected audio-session volume");
    }

    public void SetMute(bool muted, Guid eventContext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ISimpleAudioVolume volume = _volume
            ?? throw new InvalidOperationException("Windows did not expose per-session mute control.");
        WindowsAudioSessionCatalogInterop.CheckHResult(
            volume.SetMute(muted, ref eventContext),
            "restore the selected audio-session mute state");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Marshal.IsComObject(_baseControl))
        {
            try
            {
                Marshal.FinalReleaseComObject(_baseControl);
            }
            catch
            {
                // The console process is ending and no replacement session may be selected during cleanup.
            }
        }
    }
}

internal static class WindowsAudioSessionCatalogInterop
{
    public static void CheckHResult(int hresult, string operation)
    {
        if (hresult < 0)
        {
            throw new COMException($"Windows could not {operation}.", hresult);
        }
    }
}

internal enum EDataFlow
{
    Render = 0,
    Capture = 1,
    All = 2,
}

internal enum ERole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr client);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    [PreserveSig]
    int Activate(ref Guid interfaceId, uint classContext, IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);

    [PreserveSig]
    int OpenPropertyStore(uint accessMode, out IntPtr properties);

    [PreserveSig]
    int GetId(out IntPtr id);

    [PreserveSig]
    int GetState(out uint state);
}

[ComImport]
[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionManager2
{
    [PreserveSig]
    int GetAudioSessionControl(ref Guid sessionId, uint streamFlags, out IntPtr sessionControl);

    [PreserveSig]
    int GetSimpleAudioVolume(ref Guid sessionId, uint streamFlags, out IntPtr audioVolume);

    [PreserveSig]
    int GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);

    [PreserveSig]
    int RegisterSessionNotification(IntPtr sessionNotification);

    [PreserveSig]
    int UnregisterSessionNotification(IntPtr sessionNotification);

    [PreserveSig]
    int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionId, IntPtr duckNotification);

    [PreserveSig]
    int UnregisterDuckNotification(IntPtr duckNotification);
}

[ComImport]
[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionEnumerator
{
    [PreserveSig]
    int GetCount(out int sessionCount);

    [PreserveSig]
    int GetSession(int sessionIndex, out IAudioSessionControl sessionControl);
}

[ComImport]
[Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl
{
    [PreserveSig]
    int GetState(out AudioSessionActivity state);

    [PreserveSig]
    int GetDisplayName(out IntPtr displayName);

    [PreserveSig]
    int SetDisplayName(IntPtr value, ref Guid eventContext);

    [PreserveSig]
    int GetIconPath(out IntPtr iconPath);

    [PreserveSig]
    int SetIconPath(IntPtr value, ref Guid eventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid groupingId);

    [PreserveSig]
    int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

    [PreserveSig]
    int RegisterAudioSessionNotification(IntPtr client);

    [PreserveSig]
    int UnregisterAudioSessionNotification(IntPtr client);
}

[ComImport]
[Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioSessionControl2
{
    [PreserveSig]
    int GetState(out AudioSessionActivity state);

    [PreserveSig]
    int GetDisplayName(out IntPtr displayName);

    [PreserveSig]
    int SetDisplayName(IntPtr value, ref Guid eventContext);

    [PreserveSig]
    int GetIconPath(out IntPtr iconPath);

    [PreserveSig]
    int SetIconPath(IntPtr value, ref Guid eventContext);

    [PreserveSig]
    int GetGroupingParam(out Guid groupingId);

    [PreserveSig]
    int SetGroupingParam(ref Guid groupingId, ref Guid eventContext);

    [PreserveSig]
    int RegisterAudioSessionNotification(IntPtr client);

    [PreserveSig]
    int UnregisterAudioSessionNotification(IntPtr client);

    [PreserveSig]
    int GetSessionIdentifier(out IntPtr sessionIdentifier);

    [PreserveSig]
    int GetSessionInstanceIdentifier(out IntPtr sessionInstanceIdentifier);

    [PreserveSig]
    int GetProcessId(out uint processId);

    [PreserveSig]
    int IsSystemSoundsSession();

    [PreserveSig]
    int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
}

[ComImport]
[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISimpleAudioVolume
{
    [PreserveSig]
    int SetMasterVolume(float level, ref Guid eventContext);

    [PreserveSig]
    int GetMasterVolume(out float level);

    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid eventContext);

    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Qtl.Keylogging.HotKeys;

public sealed class HotKeyTask : BackgroundMessagePumpTask
{
    private const uint WM_ADD_HOT_KEY = Native.WM_USER + 1;
    private const uint WM_REMOVE_HOT_KEY = Native.WM_USER + 2;

    private static void GetHotKeyFromMessage(nint l, out HotKeyModifiers modifiers, out int virtualKey)
    {
        modifiers = (HotKeyModifiers)(l & ushort.MaxValue);
        virtualKey = (int)(l >> 16 & ushort.MaxValue);
    }

    public static HotKeyTask StartNew()
    {
        var hotKeyTask = new HotKeyTask();
        hotKeyTask.Start();
        return hotKeyTask;
    }

    private readonly ConcurrentQueue<RegisteredHotKey> _toBeRegisterdHotKeys;
    private readonly ConcurrentQueue<HotKey> _toBeUnRegisterdHotKeys;
    private readonly List<RegisteredHotKey> _registeredHotKeys;

    private int _id;

    private HotKeyTask()
    {
        _toBeRegisterdHotKeys = new();
        _toBeUnRegisterdHotKeys = new();
        _registeredHotKeys = new();
    }

    private void InvokeEventsForHotKey(HotKeyModifiers modifiers, int virtualKey)
    {
        for (int i = 0; i < _registeredHotKeys.Count; i++)
        {
            var hotkey = _registeredHotKeys[i];

            if ((hotkey.Modifiers & ~HotKeyModifiers.NoRepeat) == modifiers && hotkey.VirtualKey == virtualKey)
            {
                try
                {
                    hotkey.OnHotKey(hotkey.HotKey);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }

    private void InvokeEventForMessage(nint l)
    {
        GetHotKeyFromMessage(l, out var modifiers, out var virtualKey);
        InvokeEventsForHotKey(modifiers, virtualKey);
    }

    private void RegisterQueuedHotKeys(nint windowHandle)
    {
        while (_toBeRegisterdHotKeys is { IsEmpty: false })
        {
            if (_toBeRegisterdHotKeys.TryDequeue(out var registeredHotKey))
            {
                var result = Native.RegisterHotKey(
                    (HWND)windowHandle,
                    registeredHotKey.Id,
                    (HOT_KEY_MODIFIERS)registeredHotKey.Modifiers,
                    (uint)registeredHotKey.VirtualKey
                );

                if (!result) { throw new LastPInvokeException(); }

                _registeredHotKeys.Add(registeredHotKey);
            }
        }
    }

    private void UnregisterQueuedHotKeys(nint windowHandle)
    {
        while (_toBeUnRegisterdHotKeys is { IsEmpty: false })
        {
            if (_toBeUnRegisterdHotKeys.TryDequeue(out var hotKey))
            {
                if (_registeredHotKeys.Find(registeredHotKey => registeredHotKey.Id == hotKey.Id) is RegisteredHotKey registeredHotKey)
                {
                    _registeredHotKeys.Remove(registeredHotKey);

                    var result = Native.UnregisterHotKey(
                        (HWND)windowHandle,
                        registeredHotKey.Id
                    );

                    if (!result) { throw new LastPInvokeException(); }
                }
            }
        }
    }

    protected override void OnWindowMessage(nint windowHandle, uint message, nuint w, nint l)
    {
        switch (message)
        {
            case Native.WM_HOTKEY:
                InvokeEventForMessage(l);
                break;
            case WM_ADD_HOT_KEY:
                RegisterQueuedHotKeys(windowHandle);
                break;
            case WM_REMOVE_HOT_KEY:
                UnregisterQueuedHotKeys(windowHandle);
                break;
            default:
                break;
        }
    }

    protected override void OnWindowCreated(nint windowHandle) { }

    public void AddHotKey(HotKeyModifiers modifiers, int virtualKey, Action<HotKey> onHotKey)
    {
        ArgumentNullException.ThrowIfNull(onHotKey);

        var id = _id++;
        var hotkey = new RegisteredHotKey(modifiers, virtualKey, id, onHotKey);
        _toBeRegisterdHotKeys.Enqueue(hotkey);
        InvokeMessageAsync(WM_ADD_HOT_KEY);
    }

    public void RemoveHotKey(HotKey hotKey)
    {
        ArgumentNullException.ThrowIfNull(hotKey);

        _toBeUnRegisterdHotKeys.Enqueue(hotKey);
        InvokeMessageAsync(WM_REMOVE_HOT_KEY);
    }
}

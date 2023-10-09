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
    private readonly List<RegisteredHotKey> _registeredHotKeys;

    private int _id;

    private HotKeyTask()
    {
        _toBeRegisterdHotKeys = new();
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
                    hotkey.HotKeyEventHandler(hotkey.HotKeyEventArgs);
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

    protected override void OnWindowMessage(nint windowHandle, uint message, nuint w, nint l)
    {
        if (message is Native.WM_HOTKEY)
        {
            InvokeEventForMessage(l);
        }
        else if (message is WM_ADD_HOT_KEY)
        {
            RegisterQueuedHotKeys(windowHandle);
        }
    }

    protected override void OnWindowCreated(nint windowHandle) { }

    public void AddHotKey(HotKeyModifiers modifiers, int virtualKey, HotKeyEventHandler onHotKeyEventHandler)
    {
        ArgumentNullException.ThrowIfNull(onHotKeyEventHandler, nameof(onHotKeyEventHandler));

        var id = _id++;
        var hotkey = new RegisteredHotKey(modifiers, virtualKey, id, onHotKeyEventHandler);
        _toBeRegisterdHotKeys.Enqueue(hotkey);
        _ = Native.PostMessage((HWND)WindowHandle, WM_ADD_HOT_KEY, 0, 0);
    }
}

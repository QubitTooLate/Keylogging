using Qtl.Keylogging.Windows;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Qtl.Keylogging;

public abstract class BackgroundMessagePumpTask : IDisposable
{
    private readonly AutoResetEvent _resetEvent;

    private HWND _windowHandle;
    private Task? _backgroundTask;
    private bool _started;
    private bool _isDisposed;

    protected BackgroundMessagePumpTask() 
    {
        _resetEvent = new AutoResetEvent(false);
    }

    private LRESULT OnMessageEventHandler(Win32MessageOnlyWindow window, uint message, WPARAM w, LPARAM l)
    {
        try
        {
            OnWindowMessage(window.WindowHandle, message, w, l);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }

        return window.DefaultEventHandler(message, w, l);
    }

    private void BackgroundTaskAction()
    {
        using var messageOnlyWindow = new Win32MessageOnlyWindow();
        _windowHandle = messageOnlyWindow.Create(OnMessageEventHandler);
        _resetEvent.Set();
        OnWindowCreated(_windowHandle);
        messageOnlyWindow.PumpMessages();
    }

    protected void Start()
    {
        if (_started) { throw new InvalidOperationException(); }
        _resetEvent.Reset();
        _backgroundTask = Task.Factory.StartNew(BackgroundTaskAction, TaskCreationOptions.LongRunning);
        _resetEvent.WaitOne();
        _started = true;
    }

    protected abstract void OnWindowCreated(IntPtr windowHandle);

    protected abstract void OnWindowMessage(IntPtr windowHandle, uint message, nuint w, nint l);

    protected void InvokeMessageAsync(uint message, nuint w = 0, nint l = 0) => _ = Native.PostMessage(_windowHandle, message, w, l);

    protected IntPtr WindowHandle => _windowHandle;

    public async Task StopAsync()
    {
        if (!_started) { return; }
        _started = false;

        if (_windowHandle != HWND.Null) { InvokeMessageAsync(Native.WM_QUIT); }
        await _backgroundTask!;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) { return; }
        _isDisposed = true;

        if (disposing)
        {
            _resetEvent?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BackgroundMessagePumpTask()
    {
        Dispose(false);
    }
}

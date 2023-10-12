using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Qtl.Keylogging.Windows;

internal sealed unsafe class Win32MessageOnlyWindow : IDisposable
{
	public delegate LRESULT OnMessageEventHandler(Win32MessageOnlyWindow window, uint message, WPARAM w, LPARAM l);

	[UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvStdcall) })]
	private static LRESULT WindowProcedure(HWND windowHandle, uint message, WPARAM w, LPARAM l)
	{
		var handle = Native.GetWindowLongPtr(windowHandle, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
		if (handle == IntPtr.Zero) { return Native.DefWindowProc(windowHandle, message, w, l); }
		var window = GCHandle.FromIntPtr(handle).Target as Win32MessageOnlyWindow ?? throw new UnreachableException();
		return window._messageEventHandler!.Invoke(window, message, w, l);
	}

	private static char* GetRandomUnmanagedString()
	{
		var randomString = Guid.NewGuid().ToString();
		var unmanagedRandomString = (char*)NativeMemory.Alloc((nuint)randomString.Length + 1, sizeof(char));
		randomString.CopyTo(new Span<char>(unmanagedRandomString, randomString.Length));
		unmanagedRandomString[randomString.Length] = '\0';
		return unmanagedRandomString;
	}

	private OnMessageEventHandler? _messageEventHandler;
	private GCHandle _handle;

	public HWND Create(OnMessageEventHandler messageEventHandler)
	{
		_messageEventHandler = messageEventHandler;

		var unmanagedWindowName = GetRandomUnmanagedString();
		var unmanagedClassName = GetRandomUnmanagedString();

		var windowClass = new WNDCLASSEXW
		{
			cbSize = (uint)sizeof(WNDCLASSEXW),
			lpszClassName = unmanagedClassName,
			lpfnWndProc = &WindowProcedure
		};

		var atom = Native.RegisterClassEx(&windowClass);
		if (atom is 0)
		{
			NativeMemory.Free(unmanagedWindowName);
			NativeMemory.Free(unmanagedClassName);
			throw new LastPInvokeException();
		}

		_handle = GCHandle.Alloc(this, GCHandleType.Normal);

		WindowHandle = Native.CreateWindowEx(
			default,
			unmanagedClassName,
			unmanagedWindowName,
			default,
			default,
			default,
			default,
			default,
			HWND.HWND_MESSAGE,
			HMENU.Null,
			HINSTANCE.Null,
			null
		);

		NativeMemory.Free(unmanagedWindowName);
		NativeMemory.Free(unmanagedClassName);

		if (WindowHandle == HWND.Null) { throw new LastPInvokeException(); }

		_ = Native.SetWindowLongPtr(WindowHandle, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, GCHandle.ToIntPtr(_handle));

		return WindowHandle;
	}

	public LRESULT DefaultEventHandler(uint message, WPARAM w, LPARAM l) => Native.DefWindowProc(WindowHandle, message, w, l);

	public void PumpMessages()
	{
		MSG msg;
		while (Native.GetMessage(&msg, WindowHandle, 0, 0).Value != 0)
		{
			_ = Native.DispatchMessage(&msg);
		}
	}

	public HWND WindowHandle { get; private set; }

	private void Dispose(bool disposing)
	{
		_ = disposing;

		if (_handle.IsAllocated)
		{
			_handle.Free();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~Win32MessageOnlyWindow()
	{
		Dispose(false);
	}
}

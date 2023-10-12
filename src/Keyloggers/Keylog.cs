using System;
using Windows.Win32.UI.Input;

namespace Qtl.Keylogging;

public record Keylog(IntPtr DeviceHandle, LogType Type, uint ExtraInformation, ScanCodeInfo ScanCodeInfo, uint ScanCode, uint Message, uint VirtualKey)
{
	internal unsafe Keylog(RAWINPUT* rawInput) : this(
		rawInput->header.hDevice,
		(LogType)rawInput->header.dwType,
		rawInput->data.keyboard.ExtraInformation,
		(ScanCodeInfo)rawInput->data.keyboard.Flags,
		rawInput->data.keyboard.MakeCode,
		rawInput->data.keyboard.Message,
		rawInput->data.keyboard.VKey
	)
	{

	}
}

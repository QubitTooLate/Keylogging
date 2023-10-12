using System.Diagnostics;
using Qtl.Keylogging.HotKeys;
using Qtl.Keylogging.Keyloggers;

var keylogger = KeyloggerTask.StartNew(keylog =>
{
	Console.WriteLine($"Keylogger event: {keylog}");
});

var hotKeys = HotKeyTask.StartNew();

var hotKeyTriggers = 0;
hotKeys.AddHotKey(HotKeyModifiers.NoRepeat | HotKeyModifiers.Shift, 'H', e =>
{
	Console.WriteLine($"HotKey event: {e}");

	using var _ = Process.Start("notepad.exe");

	if (++hotKeyTriggers is 3)
	{
		hotKeys.RemoveHotKey(e);
	}
});

Console.WriteLine("Press enter to quit...");
Console.ReadKey();

await keylogger.StopAsync();
await hotKeys.StopAsync();

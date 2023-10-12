using Qtl.Keylogging.HotKeys;
using Qtl.Keylogging.Keyloggers;
using System.Diagnostics;

var keylogger = KeyloggerTask.StartNew(keylog =>
{
    Console.WriteLine($"Keylogger event: {keylog}");
});

var hotkeys = HotKeyTask.StartNew();

hotkeys.AddHotKey(HotKeyModifiers.NoRepeat | HotKeyModifiers.Shift, 'H', e =>
{
    Console.WriteLine($"HotKey event: {e}");

    using var _ = Process.Start("notepad.exe");
});

Console.WriteLine("Press enter to quit...");
Console.ReadKey();

await keylogger.StopAsync();
await hotkeys.StopAsync();

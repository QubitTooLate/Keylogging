using Qtl.Keylogging.HotKeys;
using Qtl.Keylogging.Keyloggers;
using System.Diagnostics;

var keylogger = KeyloggerTask.StartNew(keylog =>
{
    Console.WriteLine($"Keylogger event: {keylog}");
});

var hotkeys = HotKeyTask.StartNew();

hotkeys.AddHotKey(HotKeyModifiers.NoRepeat | HotKeyModifiers.Shift, 'H', async e =>
{
    Console.WriteLine($"HotKey event: {e}");

    await Process.Start("notepad.exe").WaitForExitAsync();
});

Console.WriteLine("Press enter to quit...");
Console.ReadKey();

await keylogger.StopAsync();
await hotkeys.StopAsync();

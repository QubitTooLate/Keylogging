namespace Qtl.Keylogging.HotKeys;

public record RegisteredHotKey(HotKeyModifiers Modifiers, int VirtualKey, int Id, HotKeyEventHandler HotKeyEventHandler)
{
    public HotKeyEventArg HotKeyEventArgs => new(Modifiers, VirtualKey, Id);
}

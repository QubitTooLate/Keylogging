using System;

namespace Qtl.Keylogging.HotKeys;

public record RegisteredHotKey(HotKeyModifiers Modifiers, int VirtualKey, int Id, Action<HotKey> OnHotKey)
{
	public HotKey HotKey => new(Modifiers, VirtualKey, Id);
}

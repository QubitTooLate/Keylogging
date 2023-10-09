using System;

namespace Qtl.Keylogging.HotKeys;

[Flags]
public enum HotKeyModifiers : uint
{
    Alt = 0x00000001,
    Control = 0x00000002,
    NoRepeat = 0x00004000,
    Shift = 0x00000004,
    Win = 0x00000008,
}

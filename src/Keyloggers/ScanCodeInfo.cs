using System;

namespace Qtl.Keylogging;

[Flags]
public enum ScanCodeInfo : uint
{
	Down = 0,
	Up = 1,
	E0Prefix = 2,
	E1Prefix = 4,
}

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Qtl.Keylogging;

internal class LastPInvokeException : Win32Exception
{
	public LastPInvokeException() : base(Marshal.GetLastPInvokeError(), Marshal.GetLastPInvokeErrorMessage())
	{

	}
}

namespace ILib.Contents
{
	using System;

	[Flags]
	internal enum TransLockFlag
	{
		Boot = 1,
		RunOrSuspend = 2,
		EnableOrDisable = 3,
		Shutdown = 4,
	}
}

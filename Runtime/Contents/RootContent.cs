using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ILib.Contents
{

	internal class RootContent : Content<BootParam>
	{
		protected override async Task OnRun()
		{
			if (Param.ParallelBoot)
			{
				await Task.WhenAll(Param.BootContents.Select((x) => Append(x)));
			}
			else
			{
				foreach (var prm in Param.BootContents)
				{
					await Append(prm);
				}
			}
		}
	}

}

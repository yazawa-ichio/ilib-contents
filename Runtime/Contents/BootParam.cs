using System;
using System.Collections.Generic;

namespace ILib.Contents
{
	public class BootParam : IContentParam
	{
		public bool ParallelBoot { get; set; }
		public List<IContentParam> BootContents { get; set; } = new List<IContentParam>();

		public void Add<T>(object prm = null) where T : Content
		{
			BootContents.Add(SimpleParam.Create<T>(prm));
		}

		Type IContentParam.GetContentType()
		{
			return typeof(RootContent);
		}
	}

}

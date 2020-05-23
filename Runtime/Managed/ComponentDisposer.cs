﻿using UnityEngine;
using System;

namespace ILib.Managed
{
	public class ComponentDisposer : IDisposable
	{
		Component m_Component;

		public ComponentDisposer(Component component) => m_Component = component;

		public void Dispose()
		{
			if (m_Component == null) return;
			Component.Destroy(m_Component);
			m_Component = null;
		}
	}
}
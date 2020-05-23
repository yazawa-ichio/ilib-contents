﻿using UnityEngine;
using System;

namespace ILib.Managed
{
	public class GameObjectDisposer : IDisposable
	{
		GameObject m_GameObject;

		public GameObjectDisposer(GameObject component) => m_GameObject = component;

		public void Dispose()
		{
			if (m_GameObject == null) return;
			GameObject.Destroy(m_GameObject);
			m_GameObject = null;
		}
	}
}
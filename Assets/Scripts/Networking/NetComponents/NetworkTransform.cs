﻿using UnityEngine;

internal struct TransformUpdate
{
	public Vector3 position;
	public Quaternion rotation;
	public Vector3 scale;
}

[FrequentUpdate]
public class NetworkTransform : NetworkBehaviour
{
	[NetVar(nameof(OnRecvTransform))] internal TransformUpdate m_transform;

	/// <summary>
	/// Allows the client to display the object with a different transformation
	/// than what the owner says (good for client-predicted stuff like picking up an item).
	/// </summary>
	public bool overrideTransform = false;

	private void Update()
	{
		if (IsOwner && TickManager.ShouldTick())
		{
			m_transform.position = transform.position;
			m_transform.rotation = transform.rotation;
			m_transform.scale = transform.localScale;
		}
	}

	void OnRecvTransform()
	{
		if (overrideTransform) return;

		transform.SetPositionAndRotation(m_transform.position, m_transform.rotation);
		transform.localScale = m_transform.scale;
	}
}
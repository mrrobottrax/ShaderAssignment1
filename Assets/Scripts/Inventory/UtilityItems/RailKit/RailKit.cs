using UnityEngine;
using static UnityEditor.FilePathAttribute;
using UnityEngine.UIElements;

public class RailKit : UseableItem
{
	[Header("Rail Kit")]
	[SerializeField] float m_maxMetresStack = 50;
	[SerializeField] float m_metresLeft = 50;

	[Header("Rail Placing")]
	[SerializeField] float m_maxRaycastDist = 4;
	[SerializeField] LayerMask m_trackBlockLayers = ~0;
	[SerializeField] float m_maxAngle = 50;
	[SerializeField] float m_tangentCheckDist = 0.3f;
	[SerializeField] float m_tangentCheckAngle = 0.3f;
	[SerializeField] float m_minDistAway = 1f;
	[SerializeField] RailGenerator m_generator;
	[SerializeField] GameObject m_railNodePrefab;
	[SerializeField] LayerMask m_snappingLayer;

	[Header("Preview")]
	[SerializeField] Material m_previewMaterial;
	[SerializeField] Color m_goodPreviewColour;
	[SerializeField] Color m_badPreviewColour;

	// Starts from the raycast
	RailNode m_startNode; // Used ONLY when connecting two nodes
	Vector3 m_startPos;
	Quaternion m_startRot;

	// Ends at any track we're extending
	bool m_bExtendingTrack = false;
	RailNode m_endNode;

	bool m_bCanMakeRail;
	RailGenerator.Curve m_curve;

	static MeshFilter s_previewMeshFilter1;
	static MeshRenderer s_previewMeshRenderer1;
	static MeshFilter s_previewMeshFilter2;
	static MeshRenderer s_previewMeshRenderer2;

	#region Interaction

	new void Awake()
	{
		interactions = new Interaction[1] {
			new() {
				prompt = GetPromptText(),
				sprite = itemSprite,
				interact = PickUp
			}
		};
	}

	string GetPromptText()
	{
		return $"Pick up {itemName} ({(int)m_metresLeft}m)";
	}

	public override string GetCustomStackText()
	{
		return $"{(int)m_metresLeft}m";
	}

	public override Interaction[] GetInteractions()
	{
		// Show metres in prompt
		interactions[0].prompt = GetPromptText();

		return base.GetInteractions();
	}

	static bool IsNotFull(Item item)
	{
		if (item is not RailKit) return false;

		RailKit kit = item as RailKit;

		return kit.m_metresLeft < kit.m_maxMetresStack;
	}

	protected override void PickUp(PlayerInteraction interactor)
	{
		PlayerInventory inventory = GetInventoryComponent(interactor);

		// Check if we have an empty hand
		if (inventory.GetActiveSlot().items.Count != 0)
		{
			// Check for any non-full rail kits
			if (inventory.FindItemWhere(IsNotFull) != null)
			{
				// Absord metres into existing kits
				RailKit kit;
				while (kit = inventory.FindItemWhere(IsNotFull) as RailKit)
				{
					TransportMetresTo(kit);
				}

				interactor.ForceRefresh();
				return;
			}
		}

		// Normal pickup
		CreatePreview();
		base.PickUp(interactor);
	}

	public override void OnDrop()
	{
		base.OnDrop();
		DestroyPreview();
	}

	public override void OnEquip()
	{
		base.OnEquip();
		CreatePreview();
	}

	public override void OnUnEquip()
	{
		base.OnUnEquip();
		m_bExtendingTrack = false;

		if (s_previewMeshRenderer1 != null)
			s_previewMeshRenderer1.enabled = false;

		if (s_previewMeshRenderer2 != null)
			s_previewMeshRenderer2.enabled = false;
	}

	// Move metres to another kit
	void TransportMetresTo(RailKit kit)
	{
		float maxAdd = kit.m_maxMetresStack - kit.m_metresLeft;
		float maxRemove = m_metresLeft;

		float remove = Mathf.Min(maxAdd, maxRemove);

		m_metresLeft -= remove;

		kit.m_metresLeft += remove;

		// Destroy when empty
		if (m_metresLeft <= 0)
		{
			Destroy(gameObject);
		}

		kit.ownerSlot.ItemUpdate?.Invoke();
	}

	#endregion

	#region Rail Building

	// Create the gameobject for the preview hologram
	void CreatePreview()
	{
		if (s_previewMeshFilter1 == null)
		{
			s_previewMeshFilter1 = new GameObject("Preview",
				new System.Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).GetComponent<MeshFilter>();
			s_previewMeshRenderer1 = s_previewMeshFilter1.GetComponent<MeshRenderer>();

			s_previewMeshRenderer1.enabled = false;
			s_previewMeshRenderer1.material = m_previewMaterial;
		}

		if (s_previewMeshFilter2 == null)
		{
			s_previewMeshFilter2 = new GameObject("Preview 2",
			new System.Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).GetComponent<MeshFilter>();
			s_previewMeshRenderer2 = s_previewMeshFilter2.GetComponent<MeshRenderer>();

			s_previewMeshRenderer2.enabled = false;
			s_previewMeshRenderer2.material = m_previewMaterial;
		}
	}

	void DestroyPreview()
	{
		if (s_previewMeshFilter1 != null)
			Destroy(s_previewMeshFilter1.gameObject);

		if (s_previewMeshFilter2 != null)
			Destroy(s_previewMeshFilter2.gameObject);
	}

	private void Update()
	{
		if (ownerInventory == null) return;

		UpdatePreview();
	}

	void UpdatePreview()
	{
		s_previewMeshRenderer1.enabled = false;
		s_previewMeshRenderer2.enabled = false;
		m_bCanMakeRail = false;

		// Extending track has been destroyed
		if (m_bExtendingTrack && m_endNode == null)
		{
			m_bExtendingTrack = false;
		}

		if (!RaycastFromView(out m_startPos, out m_startRot, out m_startNode, out bool canShowStartPreview))
		{
			if (m_bExtendingTrack && canShowStartPreview)
			{
				ShowFailedStartAndEnd();
				return;
			}
			else if (m_bExtendingTrack)
			{
				ShowFailedEnd();
				return;
			}

			//else if (canShowStartPreview)
			//	return ShowFailedStart;

			return;
		}

		m_curve = GenerateCurve();
		m_curve = SnapUpToGround(m_curve);

		for (int i = 0; i < m_curve.m_points.Length; ++i)
		{
			Debug.DrawRay(m_curve.m_points[i], m_curve.m_orientations[i] * (Vector3.forward + Vector3.up), Color.blue);
		}

		if (m_curve.m_length > m_metresLeft)
		{
			ShowFailedCurve();
			return;
		}

		if (m_bExtendingTrack && !CheckCurveTightness(m_curve))
		{
			ShowFailedStartAndEnd();
			return;
		}

		if (!CheckCollision(m_curve))
		{
			ShowFailedCurve();
			return;
		}

		m_bCanMakeRail = true;
		ShowCurve();
	}

	bool TryPlaceRail()
	{
		if (!m_bExtendingTrack)
		{
			// Start extending a node
			if (m_startNode != null)
			{
				m_bExtendingTrack = true;
				m_endNode = m_startNode;
				return false;
			}

			// Place a new rail
			RailNode node1 = Instantiate(m_railNodePrefab).GetComponent<RailNode>();
			RailNode node2 = Instantiate(m_railNodePrefab).GetComponent<RailNode>();

			m_curve = GetDefaultCurve();

			node1.transform.SetPositionAndRotation(m_startPos, m_startRot);
			node2.transform.SetPositionAndRotation(m_curve.m_points[1], m_curve.m_orientations[1]);

			node1.next = node2;
			node2.prev = node1;

			node1.GenerateMesh(m_generator, m_curve);

			return true;
		}

		m_bExtendingTrack = false;

		// Extend a track to a new spot
		if (m_startNode == null)
		{
			// Some error has happened
			if (m_endNode == null)
			{
				Debug.LogWarning("Rail invalid");
				return false;
			}

			bool connectFront = m_endNode.next == null && m_endNode.prev != null;
			bool connectBack = m_endNode.prev == null && m_endNode.next != null;

			if (connectFront == connectBack)
			{
				Debug.LogWarning("Rail invalid");
				return false;
			}

			RailNode node1 = Instantiate(m_railNodePrefab).GetComponent<RailNode>();

			node1.transform.SetPositionAndRotation(m_startPos, m_startRot);
			node1.next = m_endNode;

			if (connectFront)
			{
				// Connect to front
				m_endNode.next = node1;
				m_endNode.GenerateMesh(m_generator, m_curve);
			}
			else if (connectBack)
			{
				// Connect to back
				m_endNode.prev = node1;
				node1.GenerateMesh(m_generator, m_curve);
			}

			return true;
		}

		// Connect two nodes
		if (m_startNode == null || m_endNode == null)
		{
			Debug.LogWarning("Rail invalid");
			return false;
		}

		bool startConnectFront = m_startNode.next == null && m_startNode.prev != null;
		bool startConnectBack = m_startNode.next != null && m_startNode.prev == null;

		bool endConnectFront = m_endNode.next == null && m_endNode.prev != null;
		bool endConnectBack = m_endNode.next != null && m_endNode.prev == null;

		if (startConnectFront == startConnectBack || endConnectFront == endConnectBack)
		{
			Debug.LogWarning("Rail invalid");
			return false;
		}

		// Connect them
		if (startConnectFront)
		{
			m_startNode.next = m_endNode;
		}
		else if (startConnectBack)
		{
			m_startNode.prev = m_endNode;
		}

		if (endConnectFront)
		{
			m_endNode.next = m_startNode;
		}
		else if (endConnectBack)
		{
			m_endNode.prev = m_startNode;
		}

		// We're connecting two same ends, do some special stuff
		if (startConnectFront == endConnectFront || startConnectBack == endConnectBack)
		{
			m_startNode.GenerateMesh2(m_generator, m_curve);
		}
		else
		{
			// Generate mesh
			if (startConnectFront)
			{
				m_startNode.GenerateMesh(m_generator, m_curve);
			}
			else if (endConnectFront)
			{
				m_endNode.GenerateMesh(m_generator, m_curve);
			}
		}

		return true;
	}

	public override void OnFire1Pressed()
	{
		base.OnFire1Pressed();

		UpdatePreview();

		if (!m_bCanMakeRail) return;

		if (TryPlaceRail())
		{
			m_metresLeft -= m_curve.m_length;

			if ((int)m_metresLeft <= 0)
			{
				Destroy(gameObject);
			}

			ownerSlot.ItemUpdate?.Invoke();
		}
	}

	public override void OnFire2Pressed()
	{
		base.OnFire2Pressed();

		m_bExtendingTrack = false;
	}

	void ShowFailedStart()
	{
		s_previewMeshRenderer1.material.color = m_badPreviewColour;

		s_previewMeshRenderer1.transform.SetPositionAndRotation(m_startPos, m_startRot);
		s_previewMeshRenderer1.enabled = true;

		s_previewMeshFilter1.mesh = GetDefaultRailMesh();
	}

	void ShowFailedEnd()
	{
		if (!m_bExtendingTrack)
		{
			throw new System.ApplicationException("This is not supposed to happen");
		}

		s_previewMeshRenderer2.material.color = m_badPreviewColour;

		// Swap rotation when extending back
		Quaternion endRot = m_endNode.transform.rotation;
		if (m_endNode.prev == null && m_endNode.next != null)
		{
			endRot = Quaternion.AngleAxis(180, endRot * Vector3.up) * endRot;
		}

		s_previewMeshRenderer2.transform.SetPositionAndRotation(m_endNode.transform.position, endRot);
		s_previewMeshRenderer2.enabled = true;

		s_previewMeshFilter2.mesh = GetDefaultRailMesh();
	}

	void ShowFailedStartAndEnd()
	{
		ShowFailedStart();
		ShowFailedEnd();
	}

	void ShowFailedCurve()
	{
		ShowCurve(false);
	}

	void ShowCurve(bool canPlace = true)
	{
		if (canPlace)
		{
			s_previewMeshRenderer1.material.color = m_goodPreviewColour;
		}
		else
		{
			s_previewMeshRenderer1.material.color = m_badPreviewColour;
		}

		s_previewMeshRenderer1.transform.SetPositionAndRotation(m_startPos, m_startRot);
		s_previewMeshRenderer1.enabled = true;

		s_previewMeshFilter1.mesh = m_generator.GenerateMesh(m_curve, m_startPos, m_startRot);
	}

	RailGenerator.Curve GetDefaultCurve()
	{
		RailGenerator.Curve curve = new()
		{
			m_points = new Vector3[] { m_startPos, m_startPos + m_startRot * Vector3.forward * m_generator.GetRailExtents().z },
			m_orientations = new Quaternion[] { m_startRot, m_startRot },
		};

		curve.m_length = Vector3.Distance(curve.m_points[0], curve.m_points[1]);

		return curve;
	}

	Mesh GetDefaultRailMesh()
	{
		RailGenerator.Curve curve = GetDefaultCurve();

		return m_generator.GenerateMesh(curve, m_startPos, m_startRot);
	}

	RailGenerator.Curve GenerateCurve()
	{
		if (m_bExtendingTrack)
		{
			// Swap direction when connecting to front
			Quaternion endRot = m_endNode.transform.rotation;
			if (m_endNode.next == null && m_endNode.prev != null)
			{
				endRot = Quaternion.AngleAxis(180, endRot * Vector3.up) * endRot;
			}
			return m_generator.GenerateCurve(m_startPos, m_startRot, m_endNode.transform.position, endRot);
		}

		return GetDefaultCurve();
	}

	bool RaycastFromView(out Vector3 hitPos, out Quaternion hitRot, out RailNode hitNode, out bool canShowStartPreview)
	{
		// Check for nodes
		if (Physics.Raycast(
			ownerInventory.GetCamera().CameraTransform.position,
			ownerInventory.GetCamera().CameraTransform.forward,
			out RaycastHit hitInfo2,
			m_maxRaycastDist,
			m_snappingLayer,
			QueryTriggerInteraction.Collide)

			&&

			hitInfo2.collider.TryGetComponent(out hitNode))
		{
			hitPos = hitNode.transform.position;
			hitRot = hitNode.transform.rotation;

			// Flip direction when connecting to the back
			if (hitNode.next != null && hitNode.prev == null)
			{
				hitRot = Quaternion.AngleAxis(180, hitRot * Vector3.up) * hitRot;
			}

			if (m_bExtendingTrack && hitNode == m_endNode)
			{
				canShowStartPreview = false;
				return false;
			}

			// Make sure this isn't mid track
			if (hitNode.next == null != (hitNode.prev == null))
			{
				canShowStartPreview = true;
				return true;
			}
		}

		// Raycast for ground
		if (!Physics.Raycast(
			ownerInventory.GetCamera().CameraTransform.position,
			ownerInventory.GetCamera().CameraTransform.forward,
			out RaycastHit hitInfo,
			m_maxRaycastDist,
			m_trackBlockLayers,
			QueryTriggerInteraction.Collide))
		{
			hitPos = hitInfo.point;
			hitRot = Quaternion.identity;
			hitNode = null;
			canShowStartPreview = true;

			return false;
		}

		hitPos = hitInfo.point;
		hitNode = null;

		// Get rotation
		Vector3 forward = ownerInventory.GetCamera().CameraTransform.forward;
		forward = Vector3.ProjectOnPlane(forward, hitInfo.normal);

		hitRot = Quaternion.LookRotation(forward, hitInfo.normal);

		// Make rotation match rail direction when extending
		if (m_bExtendingTrack)
		{
			hitRot = Quaternion.LookRotation(m_endNode.transform.position - hitPos, hitRot * Vector3.up);
		}

		// Check ground normal
		if (hitInfo.normal.y < Mathf.Cos(m_maxAngle * Mathf.Deg2Rad))
		{
			canShowStartPreview = true;
			return false;
		}

		canShowStartPreview = true;
		return true;
	}

	// Make sure the curve isn't too tight at the start and end
	bool CheckCurveTightness(RailGenerator.Curve curve)
	{
		// Check that start is in front of end
		if (Vector3.Dot(curve.m_points[^1] - curve.m_points[0], curve.m_orientations[^1] * Vector3.forward) < m_minDistAway)
		{
			Debug.DrawRay(curve.m_points[0], curve.m_orientations[0] * Vector3.forward, Color.red);
			Debug.DrawRay(curve.m_points[^1], curve.m_orientations[^1] * Vector3.forward, Color.magenta);
			return false;
		}

		// Check start angle
		Vector3 startForward = curve.m_orientations[0] * Vector3.forward;

		int startIndex2 = Mathf.RoundToInt(m_tangentCheckDist / curve.m_length * curve.m_points.Length);
		startIndex2 = Mathf.Min(startIndex2, curve.m_points.Length - 1);

		Vector3 startForward2 = curve.m_orientations[startIndex2] * Vector3.forward;

		if (Vector3.Dot(startForward, startForward2) < Mathf.Cos(m_tangentCheckAngle * Mathf.Deg2Rad))
		{
			Debug.DrawRay(curve.m_points[startIndex2], curve.m_orientations[startIndex2] * new Vector3(0, 1, 1), Color.green);
			return false;
		}

		// Check end angle
		Vector3 endForward = curve.m_orientations[^1] * Vector3.forward;

		int endIndex2 = curve.m_points.Length - 1 - startIndex2;

		Vector3 endForward2 = curve.m_orientations[endIndex2] * Vector3.forward;

		if (Vector3.Dot(endForward, endForward2) < Mathf.Cos(m_tangentCheckAngle * Mathf.Deg2Rad))
		{
			Debug.DrawRay(curve.m_points[endIndex2], curve.m_orientations[endIndex2] * new Vector3(0, 1, 1), Color.green);
			return false;
		}

		return true;
	}

	// TODO: Check collision along a rail
	bool CheckCollision(RailGenerator.Curve curve)
	{
		_ = curve;
		return true;
	}

	// TODO: Snap all segments up to the ground
	RailGenerator.Curve SnapUpToGround(RailGenerator.Curve curve)
	{
		return curve;
	}

	#endregion
}

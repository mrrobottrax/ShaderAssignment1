// Sent from client to server to request picking up an item
using System.Runtime.InteropServices;
using UnityEngine;

public partial class Item
{
	// Client requests to pick up an item

	[StructLayout(LayoutKind.Sequential)]
	class PickUpItemRequestMessage : ComponentUpdateMessage<ItemNetworkBehaviour>
	{
		public override EMessageFilter Filter => EMessageFilter.HostOnly;

		readonly int m_Slot;

		public PickUpItemRequestMessage(Item component, int slot) : base(component.NetworkComponent)
		{
			m_Slot = slot;
		}

		public override void ReceiveOnComponent(ItemNetworkBehaviour component, Peer sender)
		{
			Item item = component.Item;

			if (!item.interactionEnabled)
			{
				Debug.Log("Player can't pick up item because it's not interactable");
				return;
			}

			if (item.ownerInventory != null || item.ownerSlot != null)
			{
				Debug.Log("Player can't pick up item because it's already owned");
				return;
			}

			PlayerInventory inv = sender.Player.GetComponent<PlayerInventory>();
			if (inv == null || !inv.HasSpaceForItem(item, out _))
			{
				Debug.Log("Player cannot pick up item because inventory is full.");
				return;
			}

			// Success
			inv.AddItemToSlot(item, inv.Slots[m_Slot]);
			NetworkManager.BroadcastMessage(new PickUpItemSuccessMessage(item, sender.Player, m_Slot));
		}
	}

	// Host gives client permission to pick up item/tells other clients they have picked up the item

	[StructLayout(LayoutKind.Sequential)]
	public class PickUpItemSuccessMessage : ComponentUpdateMessage<ItemNetworkBehaviour>
	{
		readonly int m_PlayerID;
		readonly int m_Slot;

		public override EMessageFilter Filter => EMessageFilter.ClientOnly;

		public PickUpItemSuccessMessage(Item component, NetworkObject player, int slot) : base(component.NetworkComponent)
		{
			m_PlayerID = player.NetID;
			m_Slot = slot;
		}

		public override void ReceiveOnComponent(ItemNetworkBehaviour component, Peer sender)
		{
			Item item = component.Item;

			NetworkObject player = NetworkObjectManager.GetNetworkObject(m_PlayerID);

			if (player == null)
			{
				Debug.LogError("player is null?");
				return;
			}

			if (!player.TryGetComponent<PlayerInventory>(out var inv))
			{
				Debug.LogError("player has no inventory?");
				return;
			}

			inv.AddItemToSlot(item, inv.Slots[m_Slot]);
		}
	}

	// Client requests to drop item

	[StructLayout(LayoutKind.Sequential)]
	public class DropItemRequestMessage : ComponentUpdateMessage<PlayerInventory>
	{
		internal int m_ItemIndex;

		internal readonly Vector3 m_DropPos;
		internal readonly Quaternion m_DropRot;

		internal readonly Vector3 m_DropVel;

		public override EMessageFilter Filter => EMessageFilter.HostOnly;

		public DropItemRequestMessage(PlayerInventory component, Vector3 dropPoint, Vector3 dropVel, Quaternion dropRot) : base(component)
		{
			m_ItemIndex = component.GetActiveSlotIndex();

			m_DropPos = dropPoint;
			m_DropRot = dropRot;
			m_DropVel = dropVel;
		}

		public override void ReceiveOnComponent(PlayerInventory component, Peer sender)
		{
			InventorySlot slot = component.Slots[m_ItemIndex];
			if (slot == null || slot.items == null || slot.items.Count == 0)
			{
				Debug.Log("Player cannot drop an item because they aren't holding one in the slot they think they are.");
				return;
			}

			component.DropFromSlot(slot, m_DropPos, m_DropVel, m_DropRot);
			NetworkManager.BroadcastMessage(new DropItemSuccessMessage(component, m_DropPos, m_DropVel, m_DropRot, m_ItemIndex));
		}
	}

	// Successfull drop

	[StructLayout(LayoutKind.Sequential)]
	public class DropItemSuccessMessage : DropItemRequestMessage
	{
		public override EMessageFilter Filter => EMessageFilter.ClientOnly;

		public DropItemSuccessMessage(PlayerInventory component, Vector3 dropPoint, Vector3 dropVel, Quaternion dropRot, int slot)
			: base(component, dropPoint, dropVel, dropRot)
		{
			m_ItemIndex = slot;
		}

		public override void ReceiveOnComponent(PlayerInventory component, Peer sender)
		{
			InventorySlot slot = component.Slots[m_ItemIndex];
			component.DropFromSlot(slot, m_DropPos, m_DropVel, m_DropRot);
		}
	}
}
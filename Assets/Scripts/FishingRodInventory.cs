using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks purchased fishing rods without duplicating shop or wallet responsibilities.</summary>
public sealed class FishingRodInventory : MonoBehaviour
{
    [SerializeField] private List<string> ownedRodIds = new();
    [SerializeField] private string equippedRodId = "";

    public int OwnedRodCount => ownedRodIds.Count;
    public string EquippedRodId => equippedRodId;
    public event Action InventoryChanged;

    public bool Owns(string rodId)
    {
        return !string.IsNullOrWhiteSpace(rodId) && ownedRodIds.Contains(rodId);
    }

    public bool TryAddAndEquip(string rodId)
    {
        if (string.IsNullOrWhiteSpace(rodId) || Owns(rodId)) return false;
        ownedRodIds.Add(rodId);
        equippedRodId = rodId;
        InventoryChanged?.Invoke();
        return true;
    }
}

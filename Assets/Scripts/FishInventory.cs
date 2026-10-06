using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks caught fish counts. Selling removes fish through this single owner.</summary>
public sealed class FishInventory : MonoBehaviour
{
    [Serializable]
    private sealed class FishStack
    {
        public string FishId;
        public int Count;
    }

    [SerializeField] private List<FishStack> fishStacks = new();

    public event Action InventoryChanged;

    public int GetCount(string fishId)
    {
        FishStack stack = fishStacks.Find(candidate => candidate.FishId == fishId);
        return stack != null ? stack.Count : 0;
    }

    public void Add(string fishId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(fishId) || amount <= 0) return;
        FishStack stack = fishStacks.Find(candidate => candidate.FishId == fishId);
        if (stack == null)
        {
            stack = new FishStack { FishId = fishId, Count = 0 };
            fishStacks.Add(stack);
        }

        stack.Count += amount;
        InventoryChanged?.Invoke();
    }

    public int RemoveAll(string fishId)
    {
        FishStack stack = fishStacks.Find(candidate => candidate.FishId == fishId);
        if (stack == null || stack.Count <= 0) return 0;

        int removed = stack.Count;
        stack.Count = 0;
        InventoryChanged?.Invoke();
        return removed;
    }

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (FishStack stack in fishStacks) total += Mathf.Max(0, stack.Count);
            return total;
        }
    }
}

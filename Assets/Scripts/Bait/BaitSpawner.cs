using System.Collections.Generic;
using UnityEngine;

public class BaitSpawner : MonoBehaviour
{
    [SerializeField] private Bait baitPrefab;
    [Tooltip("Parent for the spawned baits, keeps the Hierarchy clean.")]
    [SerializeField] private Transform baitContainer;
    [SerializeField] private float spawnInterval = 5f;
    [Tooltip("The brief allows at most 5 active baits at the same time.")]
    [SerializeField, Range(1, 5)] private int maxActiveBaits = 3;

    private readonly List<Bait> activeBaits = new List<Bait>();

    public float SpawnInterval => spawnInterval;

    public void TrySpawn(Vector3 position)
    {
        activeBaits.RemoveAll(bait => bait == null);
        if (baitPrefab == null || activeBaits.Count >= maxActiveBaits) return;

        activeBaits.Add(Instantiate(baitPrefab, position, Quaternion.identity, baitContainer));
    }
}

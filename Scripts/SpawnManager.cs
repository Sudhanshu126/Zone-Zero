using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance {get; private set;}

    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public Transform GetSpawnPoint(int index)
    {
        return spawnPoints[index];
    }
}

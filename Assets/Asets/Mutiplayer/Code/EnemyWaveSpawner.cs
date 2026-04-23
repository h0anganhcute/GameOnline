using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class EnemyWaveSpawner : NetworkBehaviour
{
    [Header("Enemy")]
    [Tooltip("Thứ tự quái ở đây phải khớp với thứ tự điểm Spawn bên dưới")]
    [SerializeField] private NetworkPrefabRef[] enemyPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Thứ tự: Phần tử 0 -> Quái 0, Phần tử 1 -> Quái 1...")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Patrol Manager")]
    [SerializeField] private PatrolPointManager patrolManager;

    [Header("Wave Settings")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private float waveDelay = 5f;
    [Tooltip("Nếu số này lớn hơn số điểm Spawn, nó sẽ quay lại điểm đầu tiên")]
    [SerializeField] private int enemiesPerWave = 5;

    private int currentWave = 0;

    public override void Spawned()
    {
        if (!Runner.IsServer) return;

        if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("❌ Mày quên kéo Prefab quái hoặc điểm Spawn rồi!");
            return;
        }

        Debug.Log("🛡️ Spawner started - Chế độ Spawn theo thứ tự.");
        StartCoroutine(SpawnWaveLoop());
    }

    IEnumerator SpawnWaveLoop()
    {
        while (true)
        {
            currentWave++;
            Debug.Log("🌊 Wave: " + currentWave);

            yield return SpawnWave();
            yield return new WaitForSeconds(waveDelay);
        }
    }

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < enemiesPerWave; i++)
        {
            // Truyền chỉ số i vào để xác định vị trí và loại quái
            SpawnEnemySequential(i);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnEnemySequential(int index)
    {
        // 1. CHỌN QUÁI THEO THỨ TỰ
        // Dùng toán tử % (chia lấy dư) để nếu index vượt quá số lượng Prefab thì nó quay lại con đầu tiên
        int prefabIndex = index % enemyPrefabs.Length;
        NetworkPrefabRef selectedEnemy = enemyPrefabs[prefabIndex];

        // 2. CHỌN ĐIỂM SPAWN THEO THỨ TỰ
        int spawnIndex = index % spawnPoints.Length;
        Transform selectedPoint = spawnPoints[spawnIndex];

        Vector3 basePos = selectedPoint.position;

        // 3. Random nhẹ để quái không bị chồng khít lên nhau tại điểm đó
        Vector3 randomPos = basePos + Random.insideUnitSphere * 1.5f;
        randomPos.y = basePos.y;

        // 4. Kiểm tra NavMesh
        NavMeshHit hit;
        Vector3 finalPos = basePos;
        if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas))
        {
            finalPos = hit.position;
        }

        // 5. Spawn
        Runner.Spawn(selectedEnemy, finalPos, Quaternion.identity);

        Debug.Log($"👾 [Thứ tự {index}] Spawn {prefabIndex} tại điểm {spawnIndex} ({selectedPoint.name})");
    }
}
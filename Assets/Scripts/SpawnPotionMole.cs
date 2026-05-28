using UnityEngine;

public class SpawnPotionMole : MonoBehaviour
{
    [SerializeField] private GameObject potionMolePrefab;
    [SerializeField] private GameObject pressureGauge;
    public float spawnDistance = 2f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameObject prefab = potionMolePrefab;

            if (prefab == null)
            {
                Debug.LogError("PotionMole prefab not found.");
                return;
            }

            Camera cam = Camera.main;
            Vector3 spawnPosition = cam.transform.position + cam.transform.forward * spawnDistance;
            Quaternion spawnRotation = Quaternion.LookRotation(cam.transform.forward);

            Instantiate(prefab, spawnPosition, spawnRotation);
        }
    }
}
using System.Collections.Generic;
using Tools;
using UnityEngine;
using Object = UnityEngine.Object;

// TODO: Combine with WoodCart into single parent
public class StoneCart : MonoBehaviour {
    [SerializeField] private int stoneCount = 0;

    public int StoneCount {
        get => stoneCount;
        set {
            if (value <= 0) value = 0;
            stoneCount = value;
            UpdateStone();
        }
    }

    public GameObject stoneObj;

    private List<GameObject> currentObjs;

    private readonly List<Vector3> positions = new List<Vector3>() {
        new Vector3(0.0022f, -0.0011f, 0.0022f),
        new Vector3(0.0002f, 0.0011f, 0.0022f),
        new Vector3(-0.0018f, -0.0011f, 0.0022f),
        new Vector3(-0.0038f, 0.0011f, 0.0022f),
        new Vector3(0.0012f, -0.0011f, 0.00292f),
        new Vector3(-0.0008f, 0.0011f, 0.00292f),
        new Vector3(-0.0028f, -0.0011f, 0.00292f),
        new Vector3(-0.0018f, -0.0011f, 0.00364f),
    };

    public StoneCart() {
        currentObjs = new List<GameObject>();
    }

    private void UpdateStone() {
        while (currentObjs.Count > StoneCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < StoneCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Object.Instantiate(stoneObj, this.transform);
            spawned.transform.localScale = new Vector3(0.005f, 0.005f, 0.004f);
            spawned.transform.localRotation = Quaternion.Euler(-90f, 0f, -180f);
            spawned.transform.localPosition = positions[currentObjs.Count];

            currentObjs.Add(spawned);
        }
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out VacuumManager vacuumManager)) {
            if (vacuumManager.inventoryItem == 2) {
                if (vacuumManager.inventoryCount != 0) {
                    StoneCount += vacuumManager.inventoryCount;
                    vacuumManager.inventoryCount = 0;
                    vacuumManager.inventoryItem = 0;
                }
            }
        }
    }
}
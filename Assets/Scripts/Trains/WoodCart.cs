using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using Object = UnityEngine.Object;


public class WoodCart : MonoBehaviour {
    [SerializeField] private int woodCount = 0;

    public int WoodCount {
        get => woodCount;
        set {
            if (value <= 0) value = 0;
            woodCount = value;
            UpdateWood();
        }
    }

    public GameObject woodObj;

    private List<GameObject> currentObjs;

    private readonly List<Vector3> positions = new List<Vector3>() {
        new Vector3(0.0022f, 0, 0.0022f),
        new Vector3(0.0002f, 0, 0.0022f),
        new Vector3(-0.0018f, 0, 0.0022f),
        new Vector3(-0.0038f, 0, 0.0022f),
        new Vector3(0.00061f, 0f, 0.00364f),
        new Vector3(-0.00139f, 0f, 0.00364f),
        new Vector3(-0.00339f, 0f, 0.00364f),
        new Vector3(0.0002f, 0, 0.00508f),
        new Vector3(-0.0018f, 0, 0.00508f),
        new Vector3(-0.00139f, 0f, 0.00652f),
    };

    private readonly List<Quaternion> rotations = new List<Quaternion>() {
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -135f),
    };

    public WoodCart() {
        currentObjs = new List<GameObject>();
    }

    private void UpdateWood() {
        while (currentObjs.Count > WoodCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < WoodCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Object.Instantiate(woodObj, this.transform);
            spawned.transform.localScale = new Vector3(0.01f, 0.01f, 0.007f);
            spawned.transform.localRotation = rotations[currentObjs.Count];
            spawned.transform.localPosition = positions[currentObjs.Count];

            currentObjs.Add(spawned);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out VacuumManager vacuumManager)) {
            if (vacuumManager.inventoryItem == 1) {
                if (vacuumManager.inventoryCount != 0) {
                    WoodCount += vacuumManager.inventoryCount;
                    vacuumManager.inventoryCount = 0;
                    vacuumManager.inventoryItem = 0;
                }
            }
        }
    }
}
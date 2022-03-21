using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using Object = UnityEngine.Object;


public class TrackCart : MonoBehaviour {
    [SerializeField] private int trackCount = 0;

    public int TrackCount {
        get => trackCount;
        set {
            if (value <= 0) value = 0;
            trackCount = value;
            UpdateTrack();
        }
    }

    public GameObject woodObj;

    public WoodCart woodCart;
    public StoneCart stoneCart;

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

    public TrackCart() {
        currentObjs = new List<GameObject>();
    }

    private void Start() {
        InvokeRepeating("BuildTrack", 0f, 5f);
    }

    private void UpdateTrack() {
        while (currentObjs.Count > TrackCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < TrackCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Object.Instantiate(woodObj, this.transform);
            spawned.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            spawned.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            spawned.transform.localPosition = positions[currentObjs.Count];

            currentObjs.Add(spawned);
        }
    }

    private void BuildTrack() {
        if (woodCart.WoodCount > 1 && stoneCart.StoneCount > 0) {
            woodCart.WoodCount -= 2;
            stoneCart.StoneCount -= 1;
            TrackCount += 2;
        }
    }

    private Collider triggerLocked;

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out VacuumManager vacuumManager)) {
            if (triggerLocked == null) {
                triggerLocked = other;
                InvokeRepeating("MoveTrackToVacuumManager", 0f, 1f);
            }
        }
    }

    private void MoveTrackToVacuumManager() {
        if (trackCount <= 0) return;
        VacuumManager vacuumManager = triggerLocked.GetComponent<VacuumManager>();
        vacuumManager.inventoryCount += 1;
        vacuumManager.inventoryItem = 3;
        TrackCount -= 1;
    }

    private void OnTriggerExit(Collider other) {
        if (other == triggerLocked) {
            triggerLocked = null;
            CancelInvoke("MoveTrackToVacuumManager");
        }
    }
}
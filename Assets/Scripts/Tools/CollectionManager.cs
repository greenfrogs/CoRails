using System;
using System.Collections;
using System.Collections.Generic;
using Networking;
using ResourceDrops;
using Tools;
using UnityEngine;

public class CollectionManager : MonoBehaviour {
    private GameObject parent;
    private Scoring.ScoringEvent onScoreEvent;
    private WorldManager worldManager;


    public SuctionManager suctionManager;

    private void Start() {
        parent = transform.parent.gameObject;
        onScoreEvent = GameObject.Find("Scoring").GetComponent<Scoring>().OnScoreEvent;
    }

    private void OnTriggerEnter(Collider collision) {
        // only collect ResourceDrops
        if (!collision.TryGetComponent(out ResourceDropManager resourceDropManager)) return;
        
        bool result = false;
        if (resourceDropManager.type == "wood") {
            result = parent.GetComponent<VacuumManager>().AddItem("log");
            if (result) {
                onScoreEvent.Invoke(ScoreEventType.WoodPickUp);
            }
        }
        else if (resourceDropManager.type == "stone") {
            result = parent.GetComponent<VacuumManager>().AddItem("rock");
            if (result) {
                onScoreEvent.Invoke(ScoreEventType.StonePickUp);
            }
        }
        else if (resourceDropManager.type == "rail") {
            result = parent.GetComponent<VacuumManager>().AddItem("rail");
        }

        // stop sucking the object
        suctionManager.RemoveObj(new Tuple<GameObject, ResourceDropManager>(collision.gameObject, resourceDropManager));

        // if it was successfully added to inventory, destroy the object
        if (result) {
            // Destroy(collision.gameObject);
            if (worldManager == null)
            {
                worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            }
            worldManager.OnWorldUpdate.Invoke(collision.gameObject, null);  // destroy and don't spawn anything
        }
    }
}
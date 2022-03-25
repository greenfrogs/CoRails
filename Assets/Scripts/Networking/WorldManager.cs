using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ResourceDrops;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;
using Terrain = Terrain_Generation.Terrain;

namespace Networking {
    public class WorldManager : MonoBehaviour, INetworkComponent, INetworkObject {
        private readonly Queue<string> moveQueue = new Queue<string>();

        private readonly Dictionary<string, Tuple<string, bool>> removedObjects =
            new Dictionary<string, Tuple<string, bool>>();

        private readonly Dictionary<string, Vector3> resDropPos = new Dictionary<string, Vector3>();

        private readonly Queue<Tuple<string, GameObject, bool>> wmEventQueue =
            new Queue<Tuple<string, GameObject, bool>>();

        private NetworkContext netContext;

        public WorldUpdateEvent OnWorldUpdate;
        private int peersJoined = 0;
        private bool ready = true; // ready to start processing network events (or just in single-player)


        private RoomClient roomClient;
        private Scoring.ScoringEvent onScoreEvent;

        private Terrain terrainGenerationComponent;
        private int worldInitState;

        private void Awake() {
            roomClient = GetComponentInParent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendWorldStateToNewPeer);
            roomClient.OnJoinedRoom.AddListener(UpdateInitState);

            terrainGenerationComponent = GameObject.Find("Generation").GetComponent<Terrain>();
        }


        // Start is called before the first frame update
        private void Start() {
            OnWorldUpdate = new WorldUpdateEvent();
            OnWorldUpdate.AddListener(ProcessWorldUpdate);
            netContext = NetworkScene.Register(this);
            onScoreEvent = GameObject.Find("Scoring").GetComponent<Scoring>().OnScoreEvent;
            Debug.Log("[WorldManager] Hello world!");
        }

        private void Update() {
            ProcessQueueEvent();
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var wmm = message.FromJson<WorldManagerMessage>();

            if (wmm.WorldInitState > 0 && !ready) {
                // if world already ready, ignore message
                Debug.LogWarning($"Processing initial world sync message, initState: {wmm.WorldInitState}");
                RegenerateTerrain(wmm.WorldInitState);
                ready = true;
            }

            Debug.Log($">>>> destroying \'{wmm.NewRemovedObject}\'");

            GameObject toDestroy = GameObject.Find(wmm.NewRemovedObject);
            if (toDestroy == null) {
                Debug.LogWarning($"Couldn't find gameobject {wmm.NewRemovedObject}");
                return;
            }

            QueueWorldUpdate(false, wmm.NewRemovedObject); // queue the actual destroy
            Vector3 position = toDestroy.transform.position;

            if (wmm.NewSpawnedObject.IsNullOrEmpty()) return;
            removedObjects[wmm.NewRemovedObject] = new Tuple<string, bool>(wmm.NewSpawnedObject, false);

            Debug.Log($"Enqueuing new object to be moved '{wmm.NewSpawnedObject}'");
            moveQueue.Enqueue(wmm.NewSpawnedObject);
            resDropPos[wmm.NewSpawnedObject] = position;
        }

        NetworkId INetworkObject.Id => new NetworkId(2815);


        private IEnumerator WorldManagerSync() {
            Debug.LogWarning("[ONJOINEDROOM] Joined room, waiting for peers...");
            ready = false;
            const int timeoutMax = 5; // give 500ms for initializing world sync
            bool roomHasPeers = false;
            for (int timeoutTicker = 0; timeoutTicker < timeoutMax; timeoutTicker++) {
                if (roomClient.Peers.Any()) // waiting for peers to join within timeout period
                {
                    roomHasPeers = true;
                    Debug.Log("Room has peer(s)");
                    break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            if (roomHasPeers) {
                Debug.LogWarning("[ONJOINEDROOM] room has peers, waiting for sync message");
                yield break; // don't destroy terrain, peer(s) exist so wait for initState to be sent by someone else
            }

            Debug.LogWarning("[ONJOINEDROOM] no peers in room after timeout elapsed, setting up new world");
            if (FindObjectsOfType(typeof(GameObject)) is GameObject[] gameObjects) // clear out network spawned objects
                foreach (GameObject go in gameObjects)
                    if (go.name.StartsWith("SpawnedObject-"))
                        Destroy(go);
            removedObjects.Clear();
            resDropPos.Clear();
            RegenerateTerrain(terrainGenerationComponent.initState);
            ready = true; // we just joined (created) an empty room, we get to set the room's seed.
        }

        private void RegenerateTerrain(int initState) {
            worldInitState = terrainGenerationComponent.Generate(initState);
            wmEventQueue.Clear();
        }

        // private void HandlePeerAdded(IPeer newPeer)
        // {
        //     
        // }

        private void SendWorldStateToNewPeer(IPeer newPeer) {
            Debug.Log($"New peer joined: {newPeer.UUID}");
            Debug.LogWarning(removedObjects);
            int mySuffix = roomClient.Me.UUID.Last();

            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);
            Debug.LogWarning("SendWorldStateToNewPeer");
            Debug.LogWarning(doSend);
            Debug.LogWarning(ready);
            Debug.LogWarning("---------");

            if (!doSend || !ready) return;

            var m = new WorldManagerMessage {
                WorldInitState = worldInitState
            };
            netContext.SendJson(m);

            foreach (WorldManagerMessage message in removedObjects.Select(removedObject => new WorldManagerMessage {
                NewRemovedObject = removedObject.Key,
                WorldInitState = worldInitState,
                NewSpawnedObject = removedObject.Value.Item1
            })) {
                Debug.Log($"Sending world sync message: '{message.NewRemovedObject}', '{message.NewSpawnedObject}'");
                netContext.SendJson(message);
            }
        }

        private void UpdateInitState(IRoom newRoom) {
            // ready = false;
            // if (roomClient.Peers.Any()) {
            //     Debug.LogWarning("[ONJOINEDROOM] room has peers, waiting for sync message");
            //     return; // don't destroy terrain, peer(s) exist so wait for initState to be sent by someone else
            // }
            //
            // Debug.LogWarning("[ONJOINEDROOM] no peers in room, setting up new world");
            //
            // // reworking this
            // // RegenerateTerrain(terrainGenerationComponent.initState);
            // // ready = true; // we just joined (created) an empty room, we get to set the room's seed.]
            StartCoroutine(WorldManagerSync());
        }

        private string AddRemovedObject(string toRemove, GameObject toSpawn = null) {
            GameObject toRemoveGameObject = GameObject.Find(toRemove);
            if (toRemove == null || removedObjects.ContainsKey(toRemove) && removedObjects[toRemove].Item2) return null;


            string newSpawnedObjectName = null;
            if (toSpawn != null) // only local instance has toSpawn set
            {
                GameObject spawnedObject = NetworkSpawner.SpawnPersistent(this, toSpawn);
                newSpawnedObjectName = $"SpawnedObject-{((INetworkObject)spawnedObject.GetSpawnableInChildren()).Id}";
                Vector3 position = toRemoveGameObject.transform.position;
                resDropPos[newSpawnedObjectName] = position;
                moveQueue.Enqueue(newSpawnedObjectName);
            }

            if (toRemoveGameObject.TryGetComponent(out ResourceDropManager resourceDropManager))
                onScoreEvent.Invoke(resourceDropManager.type == "wood"
                    ? ScoreEventType.WoodPickUp
                    : ScoreEventType.StonePickUp);

            Destroy(toRemoveGameObject);

            return newSpawnedObjectName;
        }

        private void DoDestroy() {
            // make a DestroyLocal and DestroyRemote
            if (!ready || wmEventQueue.Count == 0) return;
            (string toDestroy, GameObject toSpawn, bool isLocal) = wmEventQueue.Dequeue();
            Debug.Log($"Processing {(isLocal ? "local" : "remote")} world event");
            Debug.Log(toDestroy);
            string newSpawnedObjectName = AddRemovedObject(toDestroy, toSpawn);
            // if (newSpawnedObjectName == null || !isLocal) return;

            if (!isLocal) return;
            removedObjects[toDestroy] =
                new Tuple<string, bool>(newSpawnedObjectName,
                    true); // update this only if local, if remote, newSpawnedObjectName is empty/null
            var msg = new WorldManagerMessage {
                NewRemovedObject = toDestroy,
                NewSpawnedObject = newSpawnedObjectName
            };
            if (toDestroy.StartsWith("SpawnedObject-"))
                roomClient.Room[toDestroy] = null; // deletes the key, <ubiq>/Runtime/Dictionaries/Types.cs:67
            netContext.SendJson(msg); // sends network message if event origin is local
        }

        private void DoMove() {
            if (moveQueue.Count == 0) return;
            // Debug.LogWarning($"MOVE QUEUE: {moveQueue.Count}");
            string objectToMove = moveQueue.Dequeue();
            // Debug.Log($"Trying to move '{objectToMove}'");
            GameObject toMove = GameObject.Find(objectToMove);
            if (toMove == null) {
                // Debug.Log($"Couldn't find '{objectToMove}'");
                moveQueue.Enqueue(objectToMove); // object hasn't spawned yet
                return;
            }

            Vector3 targetPosition = resDropPos[objectToMove];
            Debug.Log($"{objectToMove} moved to target position {targetPosition}");
            toMove.transform.position = targetPosition;
        }

        private void ProcessQueueEvent() // process one queue event
        {
            DoMove();
            DoDestroy();
        }

        // toSpawn can be a GameObject because it's local (not from network stack) and won't get null-ed for whatever reason
        private void QueueWorldUpdate(bool isLocal, string toRemove, GameObject toSpawn = null) {
            Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            Debug.Log(toRemove);
            Tuple<string, GameObject, bool> a = new Tuple<string, GameObject, bool>(toRemove, toSpawn, isLocal);
            wmEventQueue.Enqueue(a);
            Debug.Log(a);
        }

        private void ProcessWorldUpdate(GameObject updatedGameObject, GameObject dropPrefab) {
            Debug.LogWarning("ONWORLDUPDATE EVENT CALLBACK");
            Debug.LogWarning($"local: destroy '{updatedGameObject.name}'");
            QueueWorldUpdate(true, updatedGameObject.name, dropPrefab);
        }

        public void MoveDrop(string dropObjName, Vector3 targetPosition) {
            if (!resDropPos.ContainsKey(dropObjName)) return;
            resDropPos[dropObjName] = targetPosition;
        }

        public class WorldUpdateEvent : UnityEvent<GameObject, GameObject> { }


        private struct WorldManagerMessage {
            public string NewRemovedObject;
            public string NewSpawnedObject;
            public int WorldInitState;
        }
    }
}
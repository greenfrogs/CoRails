using System;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples;
using UnityEngine;
using UnityEngine.Events;
using Terrain = Terrain_Generation.Terrain;

namespace Networking
{
    public class WorldManager : MonoBehaviour, INetworkComponent, INetworkObject
    {
        NetworkId INetworkObject.Id => new NetworkId(2815);
        private NetworkContext netContext;
        public class WorldUpdateEvent : UnityEvent<GameObject, GameObject>
        { }
        public WorldUpdateEvent OnWorldUpdate;
        private readonly Dictionary<string, string> removedObjects = new Dictionary<string, string>();
        private readonly Dictionary<string, Vector3> resDropPos = new Dictionary<string, Vector3>();

        private Terrain terrainGenerationComponent;
        private int worldInitState;
        private bool ready = true;  // ready to start processing network events (or just in single-player)


        private struct WorldManagerMessage
        {
            public string NewRemovedObject;
            public string NewSpawnedObject;
            public int WorldInitState;
        }
        private readonly Queue<Tuple<GameObject, GameObject, bool>> wmEventQueue = new Queue<Tuple<GameObject, GameObject, bool>>();
        private readonly Queue<string> moveQueue = new Queue<string>();


        private RoomClient roomClient;

        private void Awake()
        {
            roomClient = GetComponentInParent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendWorldStateToNewPeer);
            roomClient.OnJoinedRoom.AddListener(UpdateInitState);
            
            terrainGenerationComponent = GameObject.Find("Generation").GetComponent<Terrain>();
        }

        private void RegenerateTerrain(int initState)
        {
            worldInitState = terrainGenerationComponent.Generate(initState);
            wmEventQueue.Clear();
        }

        private void UpdateInitState(IRoom newRoom)
        {
            ready = false;
            if (roomClient.Peers.Any())
            {
                return;  // don't destroy terrain, peer(s) exist so wait for initState to be sent by someone else
            }
            
            RegenerateTerrain(terrainGenerationComponent.initState); 
            ready = true;  // we just joined (created) an empty room, we get to set the room's seed.
        }

        private void SendWorldStateToNewPeer(IPeer newPeer)
        {
            Debug.Log($"New peer joined: {newPeer.UUID}");
            int mySuffix = roomClient.Me.UUID.Last();
        
            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            var doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last()).All(peerSuffix => peerSuffix > mySuffix);

            if (!doSend) return;

            var m = new WorldManagerMessage
            {
                WorldInitState = worldInitState,
            };
            netContext.SendJson(m);
            
            foreach (var message in removedObjects.Select(removedObject => new WorldManagerMessage
                     {
                         NewRemovedObject = removedObject.Key,
                         WorldInitState = worldInitState,
                         NewSpawnedObject = removedObject.Value,
                     }))
            {
                netContext.SendJson(message);
            }
            

        }

        // Start is called before the first frame update
        private void Start()
        {
            OnWorldUpdate = new WorldUpdateEvent();
            OnWorldUpdate.AddListener(ProcessWorldUpdate);
            netContext = NetworkScene.Register(this);
            Debug.Log("[WorldManager] Hello world!");
        }

        private string AddRemovedObject(GameObject toRemove, GameObject toSpawn = null)
        {
            // if (toRemove == null || !removedObjects.Add(toRemove.name)) return null;  // removedObjects used to be a HashSet
            if (toRemove == null || removedObjects.ContainsKey(toRemove.name)) return null;
            
            // if (toSpawn != null)  Instantiate(toSpawn, toRemove.transform.position, Quaternion.identity);
            string newSpawnedObjectName = null;
            if (toSpawn != null)  // only local instance has toSpawn set
            {
                var spawnedObject = NetworkSpawner.SpawnPersistent(this, toSpawn);
                newSpawnedObjectName = $"SpawnedObject-{((INetworkObject) spawnedObject.GetSpawnableInChildren()).Id}";
                var position = toRemove.transform.position;
                resDropPos[newSpawnedObjectName] = position;
                moveQueue.Enqueue(newSpawnedObjectName);
            }

            // removedObjects[toRemove.name] = newSpawnedObjectName;
            Destroy(toRemove);

            return newSpawnedObjectName;
        }

        private void DoDestroy()
        {
            if (!ready || wmEventQueue.Count == 0) return;
            
            var (toDestroy, toSpawn, isLocal ) = wmEventQueue.Dequeue();
            Debug.Log($"Processing {(isLocal ? "local": "remote")} world event");
            var newSpawnedObjectName = AddRemovedObject(toDestroy, toSpawn);
            if (newSpawnedObjectName == null || !isLocal) return;
            var msg = new WorldManagerMessage
            {
                NewRemovedObject = toDestroy.name,
                NewSpawnedObject = newSpawnedObjectName
            };
            removedObjects[toDestroy.name] = newSpawnedObjectName;
            if (toDestroy.name.StartsWith("SpawnedObject-"))  roomClient.Room[toDestroy.name] = null;  // deletes the key, <ubiq>/Runtime/Dictionaries/Types.cs:67
            netContext.SendJson(msg);  // sends network message if event origin is local
        }

        private void DoMove()
        {
            if (moveQueue.Count == 0) return;
            var objectToMove = moveQueue.Dequeue();
            var toMove = GameObject.Find(objectToMove);
            if (toMove == null)
            {
                moveQueue.Enqueue(objectToMove);  // object hasn't spawned yet
                return;
            }
            var targetPosition = resDropPos[objectToMove];
            Debug.Log($"{objectToMove} moved to target position {targetPosition}");
            toMove.transform.position = targetPosition;
        }

        private void ProcessQueueEvent()  // process one queue event
        {
            DoMove();
            DoDestroy();
        }
        
        private void QueueWorldUpdate(bool isLocal, GameObject toRemove, GameObject toSpawn = null)
        {
            wmEventQueue.Enqueue(new Tuple<GameObject, GameObject, bool>(toRemove, toSpawn, isLocal));
        }

        private void ProcessWorldUpdate(GameObject updatedGameObject, GameObject dropPrefab)  // could work on these var names
        {
            // Debug.Log("Got world update");
            // Debug.Log(updatedGameObject);
            // Debug.Log(dropPrefab);
            // Debug.Log("----");
            QueueWorldUpdate(true, updatedGameObject, dropPrefab);
        }
        
        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var wmm = message.FromJson<WorldManagerMessage>();

            if (wmm.WorldInitState > 0 && !ready)
            {
                Debug.Log($"Processing initial world sync message, initState: {wmm.WorldInitState}");
                RegenerateTerrain(wmm.WorldInitState);
                ready = true;
            }

            var toDestroy = GameObject.Find(wmm.NewRemovedObject);
            if (toDestroy == null)  return;
            QueueWorldUpdate(false, toDestroy);
            var position = toDestroy.transform.position;
            removedObjects[toDestroy.name] = wmm.NewSpawnedObject;
            moveQueue.Enqueue(wmm.NewSpawnedObject);
            resDropPos[wmm.NewSpawnedObject] = position;
        }

        public void MoveDrop(string dropObjName, Vector3 newPosition)
        {
            if (!resDropPos.ContainsKey(dropObjName)) return;
            resDropPos[dropObjName] = newPosition;
            // moveQueue.Enqueue(dropObjName);  // should already be handled locally, no need to queue move
        }

        private void Update()
        {
            ProcessQueueEvent();
        }
    }
}

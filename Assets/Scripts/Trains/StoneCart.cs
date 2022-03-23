using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Networking;
using ResourceDrops;
using Tools;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

// TODO: Combine with WoodCart into single parent
public class StoneCart : MonoBehaviour, INetworkComponent, INetworkObject {
    [SerializeField] private int stoneCount;
    private WorldManager worldManager;

    public GameObject stoneObj;

    public NetworkScene networkScene;

    public bool ready;

    private readonly List<Vector3> positions = new List<Vector3> {
        new Vector3(0.0022f, -0.0011f, 0.0022f),
        new Vector3(0.0002f, 0.0011f, 0.0022f),
        new Vector3(-0.0018f, -0.0011f, 0.0022f),
        new Vector3(-0.0038f, 0.0011f, 0.0022f),
        new Vector3(0.0012f, -0.0011f, 0.00292f),
        new Vector3(-0.0008f, 0.0011f, 0.00292f),
        new Vector3(-0.0028f, -0.0011f, 0.00292f),
        new Vector3(-0.0018f, -0.0011f, 0.00364f)
    };

    private readonly List<GameObject> currentObjs;
    private NetworkContext netContext;

    private RoomClient roomClient;

    public StoneCart() {
        currentObjs = new List<GameObject>();
    }

    public int StoneCount {
        get => stoneCount;
        set {
            if (value <= 0) value = 0;
            stoneCount = value;
            UpdateStone();
        }
    }

    private void Awake() {
        networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
        roomClient = networkScene.GetComponent<RoomClient>();
        roomClient.OnPeerAdded.AddListener(SendTrainState);
        roomClient.OnJoinedRoom.AddListener(InitState);
    }

    private void Start() {
        netContext = NetworkScene.Register(this);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out VacuumManager vacuumManager))
        {
            if (vacuumManager.inventoryItem == 2)
                if (vacuumManager.inventoryCount != 0) {
                    StoneCount += vacuumManager.inventoryCount;
                    vacuumManager.inventoryCount = 0;
                    vacuumManager.inventoryItem = 0;
                    netContext.SendJson(new Message(stoneCount, false));
                }
        }
        else
        {
            if (!other.TryGetComponent(out ResourceDropManager resourceDropManager)) return;
            if (resourceDropManager.type == "stone")
            {
                if (worldManager == null) worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
                worldManager.OnWorldUpdate.Invoke(other.gameObject, null); // destroy and don't spawn anything
                StoneCount += 1;
                netContext.SendJson(new Message(StoneCount, false));
            }
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
        var msg = message.FromJson<Message>();
        if (ready && msg.Joining) return;
        stoneCount = msg.StoneCount;
        UpdateStone();
        ready = true;
    }

    NetworkId INetworkObject.Id => new NetworkId(633013);

    private void UpdateStone() {
        while (currentObjs.Count > StoneCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < StoneCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Instantiate(stoneObj, transform);
            spawned.transform.localScale = new Vector3(0.005f, 0.005f, 0.004f);
            spawned.transform.localRotation = Quaternion.Euler(-90f, 0f, -180f);
            spawned.transform.localPosition = positions[currentObjs.Count];

            currentObjs.Add(spawned);
        }
    }

    private void SendTrainState(IPeer newPeer) {
        int mySuffix = roomClient.Me.UUID.Last();

        // use last character of UUID as integer, lowest integer in room sends new updates to new peer
        bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
            .All(peerSuffix => peerSuffix > mySuffix);


        if (!doSend) return;
        netContext.SendJson(new Message(stoneCount, true));
    }

    private IEnumerator SelectHost() {
        ready = false;
        const int timeoutMax = 5; // give 500ms for initializing world sync
        bool roomHasPeers = false;
        for (int timeoutTicker = 0; timeoutTicker < timeoutMax; timeoutTicker++) {
            if (roomClient.Peers.Any()) // waiting for peers to join within timeout period
            {
                roomHasPeers = true;
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (roomHasPeers)
            yield break; // don't destroy terrain, peer(s) exist so wait for initState to be sent by someone else
        ready = true; // we just joined (created) an empty room, we get to set the room's seed.
    }

    private void InitState(IRoom newRoom) {
        StartCoroutine(SelectHost());
    }

    private struct Message {
        // ReSharper disable all FieldCanBeMadeReadOnly.Local
        public int StoneCount;
        public bool Joining;

        public Message(int stoneCount, bool joining) {
            StoneCount = stoneCount;
            Joining = joining;
        }
    }
}
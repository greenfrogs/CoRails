using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Networking;
using ResourceDrops;
using Tools;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

public class WoodCart : MonoBehaviour, INetworkComponent, INetworkObject {
    [SerializeField] private int woodCount;

    public GameObject woodObj;

    public NetworkScene networkScene;

    public bool ready;
    private WorldManager worldManager;


    private readonly List<Vector3> positions = new List<Vector3> {
        new Vector3(0.0022f, 0, 0.0022f),
        new Vector3(0.0002f, 0, 0.0022f),
        new Vector3(-0.0018f, 0, 0.0022f),
        new Vector3(-0.0038f, 0, 0.0022f),
        new Vector3(0.00061f, 0f, 0.00364f),
        new Vector3(-0.00139f, 0f, 0.00364f),
        new Vector3(-0.00339f, 0f, 0.00364f),
        new Vector3(0.0002f, 0, 0.00508f),
        new Vector3(-0.0018f, 0, 0.00508f),
        new Vector3(-0.00139f, 0f, 0.00652f)
    };

    private readonly List<Quaternion> rotations = new List<Quaternion> {
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -135f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -180f),
        Quaternion.Euler(-90f, 0f, -135f)
    };

    private readonly List<GameObject> currentObjs;
    private NetworkContext netContext;

    private RoomClient roomClient;

    public WoodCart() {
        currentObjs = new List<GameObject>();
    }

    public int WoodCount {
        get => woodCount;
        set {
            if (value <= 0) value = 0;
            woodCount = value;
            UpdateWood();
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
            if (vacuumManager.inventoryItem == 1)
                if (vacuumManager.inventoryCount != 0) {
                    WoodCount += vacuumManager.inventoryCount;
                    vacuumManager.inventoryCount = 0;
                    vacuumManager.inventoryItem = 0;
                    netContext.SendJson(new Message(woodCount, false));
                }
        }
        else
        {
            if (!other.TryGetComponent(out ResourceDropManager resourceDropManager)) return;
            if (resourceDropManager.type == "wood")
            {
                if (worldManager == null) worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
                worldManager.OnWorldUpdate.Invoke(other.gameObject, null); // destroy and don't spawn anything
                WoodCount += 1;
                netContext.SendJson(new Message(WoodCount, false));
            }
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
        var msg = message.FromJson<Message>();
        if (ready && msg.Joining) return;
        woodCount = msg.WoodCount;
        UpdateWood();
        ready = true;
    }

    NetworkId INetworkObject.Id => new NetworkId(603010);

    private void UpdateWood() {
        while (currentObjs.Count > WoodCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < WoodCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Instantiate(woodObj, transform);
            spawned.transform.localScale = new Vector3(0.01f, 0.01f, 0.007f);
            spawned.transform.localRotation = rotations[currentObjs.Count];
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
        netContext.SendJson(new Message(woodCount, true));
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
        woodCount = 0;
        UpdateWood();
        ready = true; // we just joined (created) an empty room, we get to set the room's seed.
    }

    private void InitState(IRoom newRoom) {
        StartCoroutine(SelectHost());
    }

    private struct Message {
        // ReSharper disable all FieldCanBeMadeReadOnly.Local

        public int WoodCount;
        public bool Joining;

        public Message(int woodCount, bool joining) {
            WoodCount = woodCount;
            Joining = joining;
        }
    }
}
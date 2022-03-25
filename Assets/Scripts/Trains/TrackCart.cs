using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Tools;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

public class TrackCart : MonoBehaviour, INetworkComponent, INetworkObject {
    [SerializeField] private int trackCount;

    public GameObject woodObj;

    public WoodCart woodCart;
    public StoneCart stoneCart;

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

    private Collider triggerLocked;

    public TrackCart() {
        currentObjs = new List<GameObject>();
    }

    public int TrackCount {
        get => trackCount;
        set {
            if (value <= 0) value = 0;
            trackCount = value;
            UpdateTrack();
        }
    }

    private void Awake() {
        networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
        roomClient = networkScene.GetComponent<RoomClient>();
        roomClient.OnPeerAdded.AddListener(SendTrainState);
        roomClient.OnJoinedRoom.AddListener(InitState);
    }

    private void Start() {
        InvokeRepeating("BuildTrack", 0f, 5f);
        netContext = NetworkScene.Register(this);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out VacuumManager vacuumManager))
            if (triggerLocked == null) {
                triggerLocked = other;
                InvokeRepeating("MoveTrackToVacuumManager", 0f, 1f);
            }
    }

    private void OnTriggerExit(Collider other) {
        if (other == triggerLocked) {
            triggerLocked = null;
            CancelInvoke("MoveTrackToVacuumManager");
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
        var msg = message.FromJson<Message>();
        if (ready && msg.Joining) return;
        trackCount = msg.TrackCount;
        UpdateTrack();
        ready = true;
    }

    NetworkId INetworkObject.Id => new NetworkId(333016);

    private void UpdateTrack() {
        while (currentObjs.Count > TrackCount) {
            Destroy(currentObjs[currentObjs.Count - 1]);
            currentObjs.RemoveAt(currentObjs.Count - 1);
        }

        while (currentObjs.Count < TrackCount && currentObjs.Count < positions.Count) {
            GameObject spawned = Instantiate(woodObj, transform);
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
            netContext.SendJson(new Message(trackCount, false));
        }
    }

    private void MoveTrackToVacuumManager() {
        if (trackCount <= 0) return;
        var vacuumManager = triggerLocked.GetComponent<VacuumManager>();
        vacuumManager.inventoryCount += 1;
        vacuumManager.inventoryItem = 3;
        TrackCount -= 1;
        netContext.SendJson(new Message(trackCount, false));
    }


    private void SendTrainState(IPeer newPeer) {
        int mySuffix = roomClient.Me.UUID.Last();

        // use last character of UUID as integer, lowest integer in room sends new updates to new peer
        bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
            .All(peerSuffix => peerSuffix > mySuffix);


        if (!doSend) return;
        netContext.SendJson(new Message(trackCount, true));
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
        
        trackCount = 0;
        UpdateTrack();
        ready = true; // we just joined (created) an empty room, we get to set the room's seed.
    }

    private void InitState(IRoom newRoom) {
        StartCoroutine(SelectHost());
    }

    private struct Message {
        // ReSharper disable all FieldCanBeMadeReadOnly.Local
        public int TrackCount;
        public bool Joining;

        public Message(int trackCount, bool joining) {
            TrackCount = trackCount;
            Joining = joining;
        }
    }
}
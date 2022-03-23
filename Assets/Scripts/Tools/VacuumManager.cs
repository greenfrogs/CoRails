using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using Trains;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.XR;
using UnityEngine;

namespace Tools {
    public class VacuumManager : Tool, INetworkComponent, INetworkObject, IGraspable, IUseable {
        public GameObject suctionZone;
        public int inventoryCount;
        public int inventoryItem; // 0 = None, 1 = log, 2 = rock, 3 = rail
        public TextMeshPro inventoryDisplay;
        public TrackManager trackManager;
        public bool placeCoolDown; // false = able to place, true = on cooldown from previously sucking

        private NetworkContext ctx;
        public NetworkScene networkScene;
        public bool ready;
        private RoomClient roomClient;

        // Start is called before the first frame update
        private void Start() {
            useCustomNetworking = true;
            ctx = NetworkScene.Register(this);
            StartShared(new NetworkId()); // ignore this network id
        }
        private void Awake() {
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendGunState);
            roomClient.OnJoinedRoom.AddListener(InitState);
        }

        // Update is called once per frame
        private void Update() {
            UpdateDisplay();
            SharedUpdate(new Vector3(270, 0, 0), -65, 180, 0,
                Vector3.forward * -0.1f + Vector3.left * -0.04f + Vector3.up * 0.04f);
            if (!owner) return;
            if (follow == null) return;
            ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount, inventoryItem, false));
        }

        public new void Grasp(Hand controller) {
            GraspShared(controller);
            ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount,
                inventoryItem, false)); // broadcast player claim
        }

        public new void Release(Hand controller) {
            ReleaseShared(controller);
            ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount,
                inventoryItem, false)); // broadcast player unclaim
        }

        // move object to received position and inventory, if it has been unclaimed start the respawn timer
        public new void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<VacuumMessage>();

            if (msg.Joining)
            {
                if (ready) return;
                Transform transform1 = transform;
                transform1.localPosition = msg.Transform.position;
                transform1.localRotation = msg.Transform.rotation;
                claimed = msg.Claim;

                if (claimed) {
                    StopCoroutine(ResetFollow());
                    followRespawn = false;
                }
                else {
                    StartCoroutine(ResetFollow());
                }

                inventoryCount = msg.Count;
                inventoryItem = msg.Item;
            }
            else
            {
                Transform transform1 = transform;
                transform1.localPosition = msg.Transform.position;
                transform1.localRotation = msg.Transform.rotation;
                claimed = msg.Claim;

                if (claimed) {
                    StopCoroutine(ResetFollow());
                    followRespawn = false;
                }
                else {
                    StartCoroutine(ResetFollow());
                }

                inventoryCount = msg.Count;
                inventoryItem = msg.Item;
            }
        }

        public new NetworkId Id { get; } = new NetworkId(1002);

        public void Use(Hand controller) {
            SuctionMode(true); // enable suction/rail placement
        }

        public void UnUse(Hand controller) {
            // if it has sucked an object this trigger pull, enable rail placement next trigger pull
            if (placeCoolDown) {
                placeCoolDown = false;
                SuctionMode(false);
                return;
            }

            // if it can place rails, attempt to place one at the latest colliding ground tile
            if (inventoryItem == 3) {
                var suckObject = suctionZone.GetComponent<SuctionManager>();
                if (suckObject.currentlySucking.Count == 0 && suckObject.ground.Count != 0) {
                    int tail = suckObject.ground.Count - 1;

                    // if it is unable to display a holo, then its overlapping with a tree/stone/rail and should not place a rail
                    if (suckObject.currentHolo == null)
                    {
                        SuctionMode(false);
                        return;
                    }

                    // clear above ground debris
                    foreach (Transform child in suckObject.ground[tail].transform) Destroy(child.gameObject);

                    // add rail to the trackManager for placement
                    trackManager.Add((int) suckObject.ground[tail].transform.position.x,
                        (int) suckObject.ground[tail].transform.position.z);
                    trackManager.BroadcastAdd((int) suckObject.ground[tail].transform.position.x,
                        (int) suckObject.ground[tail].transform.position.z);
                    inventoryCount -= 1;
                    if (inventoryCount == 0) inventoryItem = 0;
                    ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount,
                        inventoryItem, false)); // broadcast new inventory
                }

                suckObject.DestroyCurrentHolo(); // destroy projection once rail is placed
            }
            SuctionMode(false);
        }

        // attempt to add an object to the inventory, if it is incompatible return false
        public bool AddItem(string item) {
            int code = item switch {
                "log" => 1,
                "rock" => 2,
                "rail" => 3,
                _ => 0
            };

            placeCoolDown = true;

            if (inventoryItem != 0 && inventoryItem != code) return false;
            inventoryItem = code;
            inventoryCount += 1;
            ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount, inventoryItem, false));
            return true;
        }

        // set suction and collection active or inactive
        private void SuctionMode(bool activate) {
            suctionZone.SetActive(activate);
        }

        // show what is currently in the inventory above the gun
        private void UpdateDisplay() {
            string str = inventoryItem switch {
                1 => "log",
                2 => "rock",
                3 => "rail",
                _ => ""
            };
            str += "\n";
            str = str + "  " + inventoryCount;
            inventoryDisplay.text = str;
        }

        // broadcast internal inventory, position, and if it has been claimed by a player
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private struct VacuumMessage {
            public TransformMessage Transform;
            public bool Claim;
            public int Count;
            public int Item;
            public bool Joining;

            public VacuumMessage(Transform transform, bool claim, int count, int item, bool joining) {
                Transform = new TransformMessage(transform);
                Claim = claim;
                Count = count;
                Item = item;
                Joining = joining;
            }
        }
        private void SendGunState(IPeer newPeer) {
            int mySuffix = roomClient.Me.UUID.Last();

            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);


            if (!doSend) return;
            ctx.SendJson(new VacuumMessage(transform, claimed, inventoryCount, inventoryItem, true));
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
    }
}
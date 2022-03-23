using System.Collections;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Trains {
    public class CarManager : MonoBehaviour, INetworkComponent, INetworkObject {
        public TrainManager trainManager;
        public TrackManager trackManager;

        public TrackPiece currentTrack;
        public uint presetID;
        public NetworkScene networkScene;
        public bool ready;
        private NetworkContext netContext;

        private RoomClient roomClient;

        private void Awake() {
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendCarState);
            roomClient.OnJoinedRoom.AddListener(InitTrain);
        }


        private void Start() {
            currentTrack = null;
            netContext = NetworkScene.Register(this);
        }

        private void Update() {
            Vector3 position = transform.position;
            if (currentTrack?.gameObject == null) currentTrack = null;
            currentTrack ??= trainManager.trackManager.Closest(position.x, position.z + 0.1f, currentTrack);

            if (Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y)) < 0.01f) {
                Vector3 currentDirection =
                    (Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, 0.1f) - position) * 2;


                currentTrack = trainManager.trackManager.Closest(position.x + currentDirection.x,
                    position.z + currentDirection.z,
                    currentTrack);
            }

            if (trainManager.stop) return;

            float step = trainManager.GetSpeed() * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, step);

            float rotationStep = trainManager.speed * 10 * Time.deltaTime;
            Quaternion lookAt =
                Quaternion.LookRotation((currentTrack.gameObject.transform.position - position).normalized);
            lookAt *= Quaternion.Euler(-90, 270, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, rotationStep);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<Message>();
            if (ready) return;

            transform.localPosition = msg.TrainTransform.position;
            transform.localRotation = msg.TrainTransform.rotation;
            currentTrack = null;
            ready = true;
        }

        NetworkId INetworkObject.Id => new NetworkId(presetID);

        private void SendCarState(IPeer newPeer) {
            int mySuffix = roomClient.Me.UUID.Last();

            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);

            if (!doSend) return;
            netContext.SendJson(new Message(transform));
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

        private void InitTrain(IRoom newRoom) {
            StartCoroutine(SelectHost());
        }

        private struct Message {
            // ReSharper disable all FieldCanBeMadeReadOnly.Local
            public TransformMessage TrainTransform;

            public Message(Transform transform) {
                TrainTransform = new TransformMessage(transform);
            }
        }
    }
}
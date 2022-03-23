using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Trains {
    public class TrainManager : MonoBehaviour, INetworkComponent, INetworkObject {
        public TrackManager trackManager;

        public float speed = 0.2f;
        public float timeStart = 0f;

        public TrackPiece currentTrack;
        public float distance;

        public bool _stop;

        public NetworkScene networkScene;

        public bool ready;
        private NetworkContext netContext;
        private List<TrackPiece> repeatTrack;

        private RoomClient roomClient;

        public bool stop {
            get => _stop;
            set {
                _stop = value;
                if (TryGetComponent(out ParticleSystem particleSystem)) {
                    if (_stop)
                        particleSystem.Stop();
                    else {
                        particleSystem.Play();
                        timeStart = Time.time;
                    }
                }
            }
        }

        private void Awake() {
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendTrainState);
            roomClient.OnJoinedRoom.AddListener(InitCar);
        }

        private void Start() {
            currentTrack = null;
            repeatTrack = new List<TrackPiece>();
            netContext = NetworkScene.Register(this);
        }

        private void Update() {
            Vector3 position = transform.position;
            if (currentTrack?.gameObject == null) currentTrack = null;
            currentTrack ??= trackManager.Closest(position.x, position.z + 0.1f, currentTrack);

            if (Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y)) < 0.01f) {
                Vector3 currentDirection =
                    (Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, 0.1f) - position) * 2;

                repeatTrack.Add(currentTrack);

                currentTrack = trackManager.Closest(position.x + currentDirection.x, position.z + currentDirection.z,
                    currentTrack);

                if (repeatTrack.Contains(currentTrack)) {
                    Debug.LogError("FAILED");
                    stop = true;
                }
            }

            distance = Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y));


            if (stop) return;
            
            float step = GetSpeed() * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, step);

            float rotationStep = speed * 10 * Time.deltaTime;
            Quaternion lookAt =
                Quaternion.LookRotation((currentTrack.gameObject.transform.position - position).normalized);
            lookAt *= Quaternion.Euler(0, 270, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, rotationStep);
        }

        public float GetSpeed() {
            return speed + (Time.time - timeStart) * 0.0003f;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            if (ready) return;
            var msg = message.FromJson<Message>();

            transform.localPosition = msg.TrainTransform.position;
            transform.localRotation = msg.TrainTransform.rotation;

            speed = msg.Speed;
            timeStart = msg.StartTime;
            currentTrack = null;
            repeatTrack = msg.RepeatTrack;
            distance = msg.Distance;
            stop = msg.Stop;
            ready = true;
        }

        NetworkId INetworkObject.Id => new NetworkId(600000);


        private void SendTrainState(IPeer newPeer) {
            int mySuffix = roomClient.Me.UUID.Last();

            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);


            if (!doSend) return;
            netContext.SendJson(new Message(transform, speed, timeStart, repeatTrack, distance, stop));
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

        private void InitCar(IRoom newRoom) {
            StartCoroutine(SelectHost());
        }

        private struct Message {
            // ReSharper disable all FieldCanBeMadeReadOnly.Local

            public TransformMessage TrainTransform;
            public float Speed;
            public float StartTime;
            public List<TrackPiece> RepeatTrack;
            public float Distance;
            public bool Stop;

            public Message(Transform transform, float speed, float startTime, List<TrackPiece> repeatTrack, float distance, bool stop) {
                TrainTransform = new TransformMessage(transform);
                Speed = speed;
                StartTime = startTime;
                RepeatTrack = repeatTrack;
                Distance = distance;
                Stop = stop;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Trains {
    public class TrainManager : MonoBehaviour, INetworkComponent, INetworkObject {
        public TrackManagerSnake trackManager;
        public int trackIndex;
        private int startingTrackIndex;

        public float speed = 0.2f;
        public float timeStart;
        public AnimationCurve rotateCurve;

        public TrackPieceSnake currentTrack;
        public TrackPieceSnake nextTrack;
        public TrackPieceSnake nextNextTrack;
        public float distance;

        public bool _stop;
        public bool won;
        public bool failed;

        public NetworkScene networkScene;

        public bool ready;
        private NetworkContext netContext;
        private List<TrackPiece> repeatTrack;

        private RoomClient roomClient;

        public ParticleSystem smokeParticles;
        public ParticleSystem explosionParticles;
        public ParticleSystem fireworkParticles;

        public bool stop {
            get => _stop;
            set {
                _stop = value;
                if (_stop)
                    smokeParticles.Stop(false);
                else {
                    smokeParticles.Play(false);
                    timeStart = Time.time;
                }
            }
        }

        private void Awake() {
            startingTrackIndex = trackIndex;
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendTrainState);
            roomClient.OnJoinedRoom.AddListener(InitCar);
        }

        public void Reset() {
            trackIndex = startingTrackIndex;
            speed = 0.2f;
            timeStart = 0f;

            currentTrack = null;
            distance = 0f;

            _stop = true;
            won = false;
            failed = false;


            ready = false;
            repeatTrack = new List<TrackPiece>();
            Start();
        }

        private void Start() {
            currentTrack = null;
            repeatTrack = new List<TrackPiece>();
            netContext = NetworkScene.Register(this);
            transform.position = trackManager.tracks[trackIndex].gameObject.transform.position;
        }

        private void UpdateTrack() {
            currentTrack = trackManager.tracks.Count <= trackIndex ? null : trackManager.tracks[trackIndex];
            nextTrack = trackManager.tracks.Count <= trackIndex + 1 ? null : trackManager.tracks[trackIndex + 1];
            nextNextTrack = trackManager.tracks.Count <= trackIndex + 2 ? null : trackManager.tracks[trackIndex + 2];

            if (currentTrack == null) {
                stop = true;
                if (!failed) {
                    Debug.LogError("FAILED");
                    explosionParticles.Play(false);
                }
                failed = true;
            }
            else {
                if (currentTrack.y > 56) {
                    stop = true;
                    won = true;
                    fireworkParticles.Play();
                }
            }
        }

        private void Update() {
            if (won) return;
            Vector3 position = transform.position;
            if (currentTrack?.gameObject == null) currentTrack = null;
            if (currentTrack == null) UpdateTrack();
            if (currentTrack == null) return;

            if (GetSpeed() * Time.deltaTime > 0f && Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y)) >= 0.5f) {
                trackIndex += 1;
                UpdateTrack();
                
            }
            if (nextTrack == null) return;
            if (currentTrack == null) return;
            if (stop) return;

            
            Vector3 nextTrackLocation = nextTrack.gameObject.transform.position;
            // calculates offset of position of the entrypoint of the next rail (from the next rail's center)
            Vector3 offset = Quaternion.Euler(0, ((int)nextTrack.connections[0] + 0) % 4 * 90f, 0) * Vector3.forward * 0.5f;
            // Debug.Log($"Original target: {nextTrackLocation}, offset: {offset}, newTarget: {nextTrackLocation + offset}");
            nextTrackLocation += offset;

            float step = GetSpeed() * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, nextTrackLocation, step);
            
            if (currentTrack.connections.Sum(x=> (int)x) % 2 == 0) return;  // track is straight
            bool isNorthSouth = (int)currentTrack.connections[1] % 2 == 0;  // exit of turn is north or south

            Vector3 localTrackOffset = currentTrack.gameObject.transform.position - position;
            double rotationDegrees = -Math.Atan2(localTrackOffset.x * (isNorthSouth? -1 : 1), localTrackOffset.z) * 180 / Math.PI;
            rotationDegrees *= isNorthSouth ? -1 : 1;
            rotationDegrees += isNorthSouth ? 180 : 0;
            rotationDegrees %= 360;
            int rotationQuadrant = (int) rotationDegrees / 90;
            float rotationOffset = rotateCurve.Evaluate((float)(Math.Abs(rotationDegrees) % 90));

            transform.rotation = Quaternion.Euler(0, rotationQuadrant * 90 + Math.Sign(rotationDegrees) * rotationOffset, 0) * Quaternion.Euler(0, 270, 0);
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
            won = msg.Won;
            if (won) {
                fireworkParticles.Play();
            }
            failed = msg.Failed;
            ready = true;
        }

        NetworkId INetworkObject.Id => new NetworkId(600000);


        private void SendTrainState(IPeer newPeer) {
            int mySuffix = roomClient.Me.UUID.Last();

            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);


            if (!doSend) return;
            netContext.SendJson(new Message(transform, speed, timeStart, repeatTrack, distance, stop, won, failed));
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

            Reset();
            ready = true; // we just joined (created) an empty room, we get to set the room's seed.
        }

        private void InitCar(IRoom newRoom) {
            won = false;
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
            public bool Won;
            public bool Failed;

            public Message(Transform transform, float speed, float startTime, List<TrackPiece> repeatTrack, float distance, bool stop, bool won, bool failed) {
                TrainTransform = new TransformMessage(transform);
                Speed = speed;
                StartTime = startTime;
                RepeatTrack = repeatTrack;
                Distance = distance;
                Stop = stop;
                Won = won;
                Failed = failed;
            }
        }
    }
}
using System;
using System.Collections;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Trains {
    public class CarManager : MonoBehaviour, INetworkComponent, INetworkObject {
        public TrainManager trainManager;
        public TrackManagerSnake trackManager;
        public int trackIndex;
        private int startingTrackIndex;

        
        public TrackPieceSnake currentTrack;
        public TrackPieceSnake nextTrack;
        public TrackPieceSnake nextNextTrack;
        public float distance;
        public uint presetID;
        public NetworkScene networkScene;
        public bool ready;
        private NetworkContext netContext;

        private RoomClient roomClient;
        

        private void Awake() {
            startingTrackIndex = trackIndex;
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendCarState);
            roomClient.OnJoinedRoom.AddListener(InitTrain);
        }
        
        public void Reset() {
            currentTrack = null;
            ready = false;
            Start();
        }


        private void Start() {
            trackIndex = startingTrackIndex;
            currentTrack = null;
            netContext = NetworkScene.Register(this);
            transform.position = trackManager.tracks[trackIndex].gameObject.transform.position;
        }
        
        private void UpdateTrack() {
            currentTrack = trackManager.tracks.Count <= trackIndex ? null : trackManager.tracks[trackIndex];
            nextTrack = trackManager.tracks.Count <= trackIndex + 1 ? null : trackManager.tracks[trackIndex + 1];
            nextNextTrack = trackManager.tracks.Count <= trackIndex + 2 ? null : trackManager.tracks[trackIndex + 2];
        }

        private void Update() {
            Vector3 position = transform.position;
            if (currentTrack?.gameObject == null) currentTrack = null;
            if (currentTrack == null) UpdateTrack();
            
            if (currentTrack == null) return;
            if (trainManager.stop) return;

            if (Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y)) >= 0.5f) {
                trackIndex += 1;
                UpdateTrack();
            }
            
            if (nextTrack == null) return;
            if (currentTrack == null) return;

            
            Vector3 nextTrackLocation = nextTrack.gameObject.transform.position;
            // calculates offset of position of the entrypoint of the next rail (from the next rail's center)
            Vector3 offset = Quaternion.Euler(0, ((int)nextTrack.connections[0] + 0) % 4 * 90f, 0) * Vector3.forward * 0.5f;
            // Debug.Log($"Original target: {nextTrackLocation}, offset: {offset}, newTarget: {nextTrackLocation + offset}");
            nextTrackLocation += offset;
            

            float step = trainManager.GetSpeed() * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, nextTrackLocation, step);

            if (currentTrack.connections.Sum(x=> (int)x) % 2 == 0) return;  // track is straight
            bool isNorthSouth = (int)currentTrack.connections[1] % 2 == 0;  // exit of turn is north or south

            Vector3 localTrackOffset = currentTrack.gameObject.transform.position - position;
            double rotationDegrees = -Math.Atan2(localTrackOffset.x * (isNorthSouth? -1 : 1), localTrackOffset.z) * 180 / Math.PI;
            rotationDegrees *= isNorthSouth ? -1 : 1;
            rotationDegrees += isNorthSouth ? 180 : 0;
            rotationDegrees %= 360;
            int rotationQuadrant = (int) rotationDegrees / 90;
            float rotationOffset = trainManager.rotateCurve.Evaluate((float)(Math.Abs(rotationDegrees) % 90));

            transform.rotation = Quaternion.Euler(0, rotationQuadrant * 90 + Math.Sign(rotationDegrees) * rotationOffset, 0) * Quaternion.Euler(-90, 270, 0);
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
            Reset();
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
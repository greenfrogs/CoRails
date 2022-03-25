using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Trains {
    public class TrackManagerSnake : MonoBehaviour, INetworkComponent, INetworkObject {
        public GameObject objectStraightTrack;
        public GameObject objectCurvedTrack;
        public GameObject objectEndPost;

        public List<TrackPieceSnake> tracks;

        public UnityEvent<int, int> spawnTrack;

        public NetworkScene networkScene;

        public bool ready;
        private NetworkContext netContext;

        private RoomClient roomClient;

        private GameObject endLocation;

        public TrackManagerSnake() {
            tracks = new List<TrackPieceSnake>();
        }

        private void Awake() {
            networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
            roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SendTrackList);
            roomClient.OnJoinedRoom.AddListener(InitState);
        }

        private void Start() {
            netContext = NetworkScene.Register(this);
            if (spawnTrack == null) {
                spawnTrack = new UnityEvent<int, int>();
                spawnTrack.AddListener(Add);
            }
        }

        public void GenerateStart() {
            // Starting Track
            for (int y = -15; y <= -9; y++) Add(15, y);
            for (int x = 14; x >= 12; x--) Add(x, -9);
            for (int y = -9; y <= -6; y++) Add(11, y);
        }

        public void GenerateEnd() {
            if (endLocation != null) {
                Destroy(endLocation);
            }

            List<int> endXOptions = new List<int> {3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18};
            int endX = 0;
            int endY = 57;

            endXOptions.Sort((a, b) => 1 - 2 * (int) RandomNumberGenerator.Instance.Generate(0, 1));

            foreach (int end in endXOptions)
                if (RemoveTerrain(end, endY, false) && RemoveTerrain(end - 1, endY, false) &&
                    RemoveTerrain(end, endY + 1, false) && RemoveTerrain(end - 1, endY + 1, false)) {
                    endX = end;
                    break;
                }

            if (endX != 0) {
                RemoveTerrain(endX, endY, true);
                RemoveTerrain(endX - 1, endY, true);
                RemoveTerrain(endX, endY + 1, true);
                RemoveTerrain(endX - 1, endY + 1, true);

                GameObject post = Instantiate(objectEndPost, new Vector3(endX - 1, 0, endY),
                    Quaternion.Euler(0, 0, 0), transform);
                post.transform.rotation = new Quaternion(0.5f, 0.5f, 0.5f, -0.5f);
                endLocation = post;
            }
            else {
                Debug.LogWarning("Failed to place end");
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<Message>();
            if (msg.Joining) {
                if (ready) return;
                StartCoroutine(WaitAndSyncTracks(0.5f, msg));
                ready = true;
            }
            else {
                RemoveTerrain(msg.X, msg.Y, true);
                Add(msg.X, msg.Y);
            }
        }

        NetworkId INetworkObject.Id => new NetworkId(887845);

        private void SendTrackList(IPeer newPeer) {
            int mySuffix = roomClient.Me.UUID.Last();
            // use last character of UUID as integer, lowest integer in room sends new updates to new peer
            bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
                .All(peerSuffix => peerSuffix > mySuffix);

            if (!doSend) return;
            netContext.SendJson(new Message(0, 0, true, tracks));
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

            yield return new WaitForSeconds(0.5f);
            foreach (TrackPiece track in tracks) RemoveTerrain(track.x, track.y, true);
            ready = true; // we just joined (created) an empty room, we get to set the room's seed.
        }

        private IEnumerator WaitAndSyncTracks(float timeout, Message msg) {
            yield return new WaitForSeconds(timeout);
            Clear();
            foreach (TrackPiece track in msg.Tracks.Where(track => !Exists(track.x, track.y))) {
                RemoveTerrain(track.x, track.y, true);
                Add(track.x, track.y);
            }
        }

        private void InitState(IRoom newRoom) {
            StartCoroutine(SelectHost());
        }

        public bool RemoveTerrain(int x, int y, bool remove) {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 1f, y), Vector3.down, 10.0f);
            foreach (RaycastHit hit in hits)
                if (hit.collider.gameObject.name.Contains("ground")) {
                    if (remove)
                        foreach (Transform child in hit.collider.transform)
                            Destroy(child.gameObject);
                    return hit.collider.gameObject.name.Contains("ground_grass");
                }

            return false;
        }

        // The following parts of code are slightly horrible
        // but are by far the quickest ways of doing this
        private GameObject TrackType(TrackPieceSnake trackPiece) {
            bool north = trackPiece.connections.Contains(Connection.north);
            bool east = trackPiece.connections.Contains(Connection.east);
            bool south = trackPiece.connections.Contains(Connection.south);
            bool west = trackPiece.connections.Contains(Connection.west);

            Vector3 position = new Vector3(trackPiece.x, 0, trackPiece.y);

            // Perfect
            if (south && north)
                return Instantiate(objectStraightTrack, position, Quaternion.Euler(-90, 90, 0),
                    transform);
            if (south && east)
                return Instantiate(objectCurvedTrack, position, Quaternion.Euler(-90, 180, 0),
                    transform);
            if (south && west)
                return Instantiate(objectCurvedTrack, position, Quaternion.Euler(-90, 270, 0),
                    transform);
            if (east && west)
                return Instantiate(objectStraightTrack, position, Quaternion.Euler(-90, 0, 0),
                    transform);
            if (north && east)
                return Instantiate(objectCurvedTrack, position, Quaternion.Euler(-90, 90, 0),
                    transform);
            if (north && west)
                return Instantiate(objectCurvedTrack, position, Quaternion.Euler(-90, 0, 0),
                    transform);

            // Best guess
            if (north || south)
                return Instantiate(objectStraightTrack, position, Quaternion.Euler(-90, 90, 0),
                    transform);

            return Instantiate(objectStraightTrack, position, Quaternion.Euler(-90, 0, 0),
                transform);
        }

        private void UpdatePosition(TrackPieceSnake trackPiece) {
            Destroy(trackPiece.gameObject);
            trackPiece.gameObject = TrackType(trackPiece);
        }

        public void Add(int x, int y) {
            if (!CanAdd(x, y)) return;
            var newTrack = new TrackPieceSnake(x, y);

            if (tracks.Count != 0) {
                if (tracks.Last().x == x) {
                    if (tracks.Last().y == y + 1) {
                        tracks.Last().connections.Add(Connection.south);
                        newTrack.connections.Add(Connection.north);
                    }
                    else if (tracks.Last().y == y - 1) {
                        tracks.Last().connections.Add(Connection.north);
                        newTrack.connections.Add(Connection.south);
                    }
                }
                else if (tracks.Last().y == y) {
                    if (tracks.Last().x == x + 1) {
                        tracks.Last().connections.Add(Connection.west);
                        newTrack.connections.Add(Connection.east);
                    }
                    else if (tracks.Last().x == x - 1) {
                        tracks.Last().connections.Add(Connection.east);
                        newTrack.connections.Add(Connection.west);
                    }
                }

                UpdatePosition(tracks.Last());
            }

            newTrack.gameObject = TrackType(newTrack);
            tracks.Add(newTrack);
        }

        public bool CanAdd(int x, int y) {
            if (tracks.Count == 0) return true;
            return ((Math.Abs(x - tracks.Last().x) == 1 && y == tracks.Last().y) ||
                    (Math.Abs(y - tracks.Last().y) == 1 && x == tracks.Last().x));
        }

        public void BroadcastAdd(int x, int y) {
            netContext.SendJson(new Message(x, y, false, null));
        }

        public TrackPiece Closest(float x, float y, TrackPiece exclusion) {
            TrackPiece piece = null;
            var origin = new Vector2(x, y);
            var closest = new Vector2Int(-1000, -1000);
            foreach (TrackPiece trackPiece in tracks) {
                var newPiece = new Vector2Int(trackPiece.x, trackPiece.y);

                if (Vector2.Distance(origin, closest) > Vector2.Distance(origin, newPiece))
                    if (trackPiece != exclusion) {
                        closest = new Vector2Int(trackPiece.x, trackPiece.y);
                        piece = trackPiece;
                    }
            }

            return piece;
        }

        public bool Exists(int x, int y) {
            return tracks.Any(trackPiece => trackPiece.x == x && trackPiece.y == y);
        }

        public void Clear() {
            foreach (TrackPieceSnake trackPiece in tracks) Destroy(trackPiece.gameObject);
            tracks = new List<TrackPieceSnake>();
        }

        private struct Message {
            // ReSharper disable all FieldCanBeMadeReadOnly.Local
            public int X;
            public int Y;
            public bool Joining;
            public List<TrackPieceSnake> Tracks;

            public Message(int x, int y, bool joining, List<TrackPieceSnake> tracks) {
                X = x;
                Y = y;
                Joining = joining;
                Tracks = tracks;
            }
        }
    }

    [Serializable]
    public class TrackPieceSnake : TrackPiece {
        public List<Connection> connections;

        public TrackPieceSnake(int x, int y) : base(x, y) {
            this.x = x;
            this.y = y;
            connections = new List<Connection>();
        }

        public TrackPieceSnake(int x, int y, GameObject gameObject) : base(x, y, gameObject) {
            this.x = x;
            this.y = y;
            this.gameObject = gameObject;
            connections = new List<Connection>();
        }

        public bool CanConnect() {
            return (connections.Count < 2);
        }
    }

    [Serializable]
    public enum Connection {
        north,
        east,
        south,
        west
    }
}
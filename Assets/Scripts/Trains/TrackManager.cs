using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace Trains {
    public class TrackManager : MonoBehaviour {
        public GameObject objectStraightTrack;
        public GameObject objectCurvedTrack;
        public GameObject objectEndPost;

        public List<TrackPiece> tracks;

        public UnityEvent<int, int> spawnTrack;

        public TrackManager() {
            tracks = new List<TrackPiece>();
        }

        void Start() {
            if (spawnTrack == null) {
                spawnTrack = new UnityEvent<int, int>();
                spawnTrack.AddListener(Add);
            }

            // Starting Track
            for (int y = -14; y <= -9; y++) {
                Add(15, y);
            }

            for (int x = 14; x >= 12; x--) {
                Add(x, -9);
            }

            for (int y = -9; y <= -6; y++) {
                Add(11, y);
            }

            List<int> endXOptions = new List<int> {3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18};
            int endX = 0;
            int endY = 37;

            endXOptions.Sort((a, b) => 1 - 2 * (int) RandomNumberGenerator.Instance.Generate(0, 1));

            foreach (int end in endXOptions) {
                if (RemoveTerrain(end, endY, false) && RemoveTerrain(end, endY + 1, false)) {
                    endX = end;
                    break;
                }
            }

            if (endX != 0) {
                RemoveTerrain(endX, endY, true);
                RemoveTerrain(endX - 1, endY, true);
                RemoveTerrain(endX, endY + 1, true);
                RemoveTerrain(endX - 1, endY + 1, true);
                
                GameObject post = Object.Instantiate(objectEndPost, new Vector3(endX - 1, 0, endY),
                    Quaternion.Euler(0, 0, 0), this.transform);
                post.transform.rotation = new Quaternion(0.5f, 0.5f, 0.5f, -0.5f);
                Add(endX, endY);
                Add(endX, endY + 1);
            }
            else {
                Debug.LogWarning("Failed to place end");
            }

            
        }

        bool RemoveTerrain(int x, int y, bool remove) {
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 1f, y), Vector3.down, 10.0f);
            foreach (RaycastHit hit in hits) {
                if (hit.collider.gameObject.name.Contains("ground")) {
                    if (remove) {
                        foreach (Transform child in hit.collider.transform) {
                            Destroy(child.gameObject);
                        }
                    }
                    return true;

                }
            }

            return false;
        }

        // The following parts of code are slightly horrible
        // but are by far the quickest ways of doing this
        GameObject TrackType(int x, int y) {
            bool north, east, south, west;
            north = east = south = west = false;

            foreach (TrackPiece trackPiece in tracks) {
                if (trackPiece.x == x) {
                    if (trackPiece.y == y - 1) {
                        south = true;
                    }
                    else if (trackPiece.y == y + 1) {
                        north = true;
                    }
                }
                else if (trackPiece.y == y) {
                    if (trackPiece.x == x - 1) {
                        west = true;
                    }
                    else if (trackPiece.x == x + 1) {
                        east = true;
                    }
                }
            }

            // Perfect
            if (south && north) {
                return Object.Instantiate(objectStraightTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 90, 0),
                    this.transform);
            }
            else if (south && east) {
                return Object.Instantiate(objectCurvedTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 180, 0),
                    this.transform);
            }
            else if (south && west) {
                return Object.Instantiate(objectCurvedTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 270, 0),
                    this.transform);
            }
            else if (east && west) {
                return Object.Instantiate(objectStraightTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 0, 0),
                    this.transform);
            }
            else if (north && east) {
                return Object.Instantiate(objectCurvedTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 90, 0),
                    this.transform);
            }
            else if (north && west) {
                return Object.Instantiate(objectCurvedTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 0, 0),
                    this.transform);
            }

            // Best guess
            if (north || south) {
                return Object.Instantiate(objectStraightTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 90, 0),
                    this.transform);
            }

            return Object.Instantiate(objectStraightTrack, new Vector3(x, 0, y), Quaternion.Euler(-90, 0, 0),
                this.transform);
        }

        bool UpdatePosition(int x, int y) {
            TrackPiece piece = null;
            foreach (TrackPiece trackPiece in tracks) {
                if (trackPiece.x == x && trackPiece.y == y) {
                    piece = trackPiece;
                }
            }

            if (piece == null) {
                return false;
            }

            Object.Destroy(piece.gameObject);
            piece.gameObject = TrackType(x, y);
            return true;
        }

        public void Add(int x, int y) {
            GameObject gameObject = TrackType(x, y);
            TrackPiece newTrack = new TrackPiece(x, y, gameObject);
            tracks.Add(newTrack);
            UpdatePosition(x - 1, y);
            UpdatePosition(x + 1, y);
            UpdatePosition(x, y - 1);
            UpdatePosition(x, y + 1);
        }

        public TrackPiece Closest(float x, float y, TrackPiece exclusion) {
            TrackPiece piece = null;
            Vector2 origin = new Vector2(x, y);
            Vector2Int closest = new Vector2Int(-1000, -1000);
            foreach (TrackPiece trackPiece in tracks) {
                Vector2Int newPiece = new Vector2Int(trackPiece.x, trackPiece.y);

                if (Vector2.Distance(origin, closest) > Vector2.Distance(origin, newPiece)) {
                    if (trackPiece != exclusion) {
                        closest = new Vector2Int(trackPiece.x, trackPiece.y);
                        piece = trackPiece;
                    }
                }
            }

            return piece;
        }

        public bool Exists(int x, int y) {
            return tracks.Any(trackPiece => trackPiece.x == x && trackPiece.y == y);
        }
    }

    [System.Serializable]
    public class TrackPiece {
        public int x;
        public int y;
        public GameObject gameObject;

        public TrackPiece(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public TrackPiece(int x, int y, GameObject gameObject) {
            this.x = x;
            this.y = y;
            this.gameObject = gameObject;
        }
    }
}
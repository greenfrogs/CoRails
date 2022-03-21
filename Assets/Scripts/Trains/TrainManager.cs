using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Trains {
    public class TrainManager : MonoBehaviour {
        public TrackManager trackManager;

        public float speed = 0.2f;

        public TrackPiece currentTrack;
        private List<TrackPiece> repeatTrack;
        public float distance;

        public bool stop;

        void Start() {
            currentTrack = null;
            repeatTrack = new List<TrackPiece>();
        }

        void Update() {
            var position = transform.position;
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
            
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, step);

            float rotationStep = speed * 10 * Time.deltaTime;
            Quaternion lookAt =
                Quaternion.LookRotation((currentTrack.gameObject.transform.position - position).normalized);
            lookAt *= Quaternion.Euler(0, 270, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, rotationStep);
        }
        
    }
}
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Trains {
    public class CarManager : MonoBehaviour {
        public TrainManager trainManager;

        public TrackPiece currentTrack;


        void Start() {
            currentTrack = null;
        }

        void Update() {
            var position = transform.position;
            currentTrack ??= trainManager.trackManager.Closest(position.x, position.z + 0.1f, currentTrack);


            if (Vector2.Distance(new Vector2(position.x, position.z),
                new Vector2(currentTrack.x, currentTrack.y)) < 0.01f) {
                Vector3 currentDirection =
                    (Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, 0.1f) - position) * 2;


                currentTrack = trainManager.trackManager.Closest(position.x + currentDirection.x, position.z + currentDirection.z,
                    currentTrack);
            }


            if (trainManager.stop) return;
            
            float step = trainManager.speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(position, currentTrack.gameObject.transform.position, step);

            float rotationStep = trainManager.speed * 10 * Time.deltaTime;
            Quaternion lookAt =
                Quaternion.LookRotation((currentTrack.gameObject.transform.position - position).normalized);
            lookAt *= Quaternion.Euler(-90, 270, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookAt, rotationStep);
        }
        
    }
}
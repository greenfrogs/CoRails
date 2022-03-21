using Ubiq.Messaging;
using UnityEngine;

namespace Tools
{
    public class PickaxeManager : Tool
    {
        public GameObject stonePrefab;

        private void FixedUpdate()
        {
            SharedUpdate(new Vector3(0, 0, 0), 65, 0, 0, Vector3.zero);
        }

        private void Start()
        {
            StartShared(new NetworkId(1001));
        }
        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision, stonePrefab, "stone", ScoreEventType.StoneMine);
        }
    }
}
using Ubiq.Messaging;
using UnityEngine;

namespace Tools
{
    public class AxeManager : Tool
    {
        public GameObject logPrefab;
        
        private void FixedUpdate()
        {
            SharedUpdate(new Vector3(0, 0, 0), 65, 0, 0, Vector3.zero);
        }

        private void Start()
        {
            StartShared(new NetworkId(1000));
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision, logPrefab, "tree", ScoreEventType.WoodMine);
        }
    }
}
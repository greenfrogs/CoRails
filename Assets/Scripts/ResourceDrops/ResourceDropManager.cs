using Networking;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq.XR;
using UnityEngine;

namespace ResourceDrops {
    public class ResourceDropManager : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable, ISpawnable {
        public bool owner;

        public string type;

        private NetworkContext ctx;
        private Transform follow;
        private Rigidbody rb;
        private WorldManager worldManager;

        public void Start() {
            OnSpawned(true);
        }

        // Update is called once per frame
        private void Update() {
            if (!owner) return;
            if (follow == null) return;
            Transform transform1 = transform;
            Vector3 controllerPosition = follow.transform.position;
            transform1.position = new Vector3(controllerPosition.x, controllerPosition.y - (float) 0.1,
                controllerPosition.z);
            transform1.rotation = follow.transform.rotation;
            transform1.Rotate(50, 0, 0);
            // ctx.SendJson(new TransformMessage(transform));
            SendPositionUpdate();
        }

        public void Grasp(Hand controller) {
            follow = controller.transform;
            owner = true;
            rb.isKinematic = true;
        }

        public void Release(Hand controller) {
            follow = null;
            owner = false;
            if (rb == null) return;
            rb.isKinematic = false;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<TransformMessage>();
            Transform transform1 = transform;
            transform1.position = msg.position;
            transform1.rotation = msg.rotation;
            if (worldManager == null) worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            worldManager.MoveDrop(name, transform.position);
            rb.isKinematic = true;
            rb.isKinematic = false;
            worldManager.MoveDrop(name, msg.position);
        }

        public NetworkId Id { get; set; }

        public void OnSpawned(bool local) {
            ctx = NetworkScene.Register(this);
            GameObject o = gameObject;
            o.tag = "ResourceDrop";
            o.name = $"SpawnedObject-{Id}";
            rb = GetComponent<Rigidbody>();
        }

        public void ForceSendPositionUpdate() {
            // ctx.SendJson(new TransformMessage(transform));
            SendPositionUpdate();
        }

        private void SendPositionUpdate() {
            if (worldManager == null) worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            worldManager.MoveDrop(name, transform.position);
            ctx.SendJson(new TransformMessage(transform));
        }
    }
}
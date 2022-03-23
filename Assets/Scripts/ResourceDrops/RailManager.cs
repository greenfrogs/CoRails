using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;

namespace ResourceDrops {
    public class RailManager : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable, IUseable {
        public bool owner;

        private NetworkContext ctx;

        private Transform follow;

        // Start is called before the first frame update
        private void Start() {
            ctx = NetworkScene.Register(this);
            gameObject.tag = "ResourceDrop";
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
        }

        public void Grasp(Hand controller) {
            follow = controller.transform;
            owner = true;
        }

        public void Release(Hand controller) {
            follow = null;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<Message>();
        }

        public NetworkId Id { get; } = new NetworkId();

        public void Use(Hand controller) {
            ;
        }

        public void UnUse(Hand controller) {
            ;
        }

        public struct Message {
            public TransformMessage transform;

            public Message(Transform transform) {
                this.transform = new TransformMessage(transform);
            }
        }
    }
}
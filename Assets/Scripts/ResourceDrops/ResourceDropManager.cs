using Networking;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Samples;
using Ubiq.XR;

namespace ResourceDrops
{
    public class ResourceDropManager : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable, ISpawnable
    {
        private WorldManager worldManager;
        private Transform follow;
        public bool owner;

        private NetworkContext ctx;
        public NetworkId Id { get; set; }

        public string type;

        public void Grasp(Hand controller)
        {
            follow = controller.transform;
            owner = true;
        }
        
        public void Release(Hand controller)
        {
            follow = null;
            owner = false;
        }

        public void OnSpawned(bool local)
        {
            ctx = NetworkScene.Register(this);
            var o = gameObject;
            o.tag = "ResourceDrop";
            o.name = $"SpawnedObject-{Id}";
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<TransformMessage>();
            var transform1 = transform;
            transform1.position = msg.position;
            transform1.rotation = msg.rotation;
            if (worldManager == null)
            {
                worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            }
            worldManager.MoveDrop(name, transform.position);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!owner) return;
            if (follow == null) return;
            var transform1 = transform;
            var controllerPosition = follow.transform.position;
            transform1.position = new Vector3(controllerPosition.x, controllerPosition.y - (float)0.1, controllerPosition.z);
            transform1.rotation = follow.transform.rotation;
            transform1.Rotate(50, 0, 0);
            // ctx.SendJson(new TransformMessage(transform));
            SendPositionUpdate();
        }

        public void ForceSendPositionUpdate()
        {
            // ctx.SendJson(new TransformMessage(transform));
            SendPositionUpdate();
        }

        private void SendPositionUpdate()
        {
            if (worldManager == null)
            {
                worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            }
            worldManager.MoveDrop(name, transform.position);
            ctx.SendJson(new TransformMessage(transform));
        }
    }
}
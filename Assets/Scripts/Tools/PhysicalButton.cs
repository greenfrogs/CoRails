using System;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;

namespace Tools {
    public abstract class PhysicalButton : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable {
        private NetworkContext ctx;
        private Transform follow;

        private bool owner;
        private bool particles;

        private Vector3 startingPosition;
        private Quaternion startingRotation;

        private void Awake() {
            startingPosition = transform.position;
            startingRotation = transform.rotation;
        }

        private void Start() {
            ctx = NetworkScene.Register(this);
        }

        private void FixedUpdate() {
            if (!owner) return;

            if (follow != null) {
                transform.position = follow.position;
                GetComponent<Rigidbody>().useGravity = false;
            }
            else {
                GetComponent<Rigidbody>().useGravity = true;
                if (GetComponent<Rigidbody>().velocity.magnitude < 0.2f && !particles) {
                    particles = true;
                    GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                    GetComponent<MeshRenderer>().enabled = false;
                    GetComponentInChildren<ParticleSystem>().Play();
                    Run();
                    ctx.SendJson(new Message(transform, false, true));
                    Invoke("Respawn", 5);
                    return;
                }
            }

            ctx.SendJson(new Message(transform, false, false));
        }


        public void Grasp(Hand controller) {
            if (owner) return;
            owner = true;
            follow = controller.transform;
            ctx.SendJson(new Message(transform, false, false));
        }

        public void Release(Hand controller) {
            follow = null;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            if (owner) return;
            var msg = message.FromJson<Message>();
            owner = msg.owner;
            transform.position = msg.transform.position;
            transform.rotation = msg.transform.rotation;
            if (!particles && msg.particles) {
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                GetComponent<MeshRenderer>().enabled = false;
                GetComponentInChildren<ParticleSystem>().Play();
                Run();
                Invoke("Respawn", 5);
            }

            particles = msg.particles;
        }

        public NetworkId Id { get; set; }

        public abstract void Run();

        private void Respawn() {
            owner = false;
            particles = false;
            transform.position = startingPosition;
            transform.rotation = startingRotation;
            GetComponent<MeshRenderer>().enabled = true;
        }

        private struct Message {
            // ReSharper disable all FieldCanBeMadeReadOnly.Local
            public TransformMessage transform;
            public bool owner;
            public bool particles;

            public Message(Transform transform, bool owner, bool particles) {
                this.transform = new TransformMessage(transform);
                this.owner = owner;
                this.particles = particles;
            }
        }
    }
}
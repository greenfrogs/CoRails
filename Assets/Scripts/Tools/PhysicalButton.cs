using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Networking;
using Trains;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;


namespace Tools {
    public class PhysicalButton : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable {
        public NetworkId Id => new NetworkId(13654654);
        private NetworkContext ctx;

        private bool owner = false;
        private Transform follow = null;
        private bool particles = false;

        private Vector3 startingPosition;
        private Quaternion startingRotation;

        public TrainManager trainManager;


        private struct Message {
            public TransformMessage transform;
            public bool owner;
            public bool particles;

            public Message(Transform transform, bool owner, bool particles) {
                this.transform = new TransformMessage(transform);
                this.owner = owner;
                this.particles = particles;
            }
        }

        private void Start() {
            ctx = NetworkScene.Register(this);
            startingPosition = transform.position;
            startingRotation = transform.rotation;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            if (!owner) ;
            Message msg = message.FromJson<Message>();
            owner = msg.owner;
            transform.position = msg.transform.position;
            transform.rotation = msg.transform.rotation;
            if (!particles && msg.particles) {
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                GetComponent<MeshRenderer>().enabled = false;
                GetComponentInChildren<ParticleSystem>().Play();
                trainManager.stop = !trainManager.stop;
                Invoke("Respawn", 5);
            }

            particles = msg.particles;
        }


        public void Grasp(Hand controller) {
            if (owner) return;
            owner = true;
            follow = controller.transform;
            ctx.SendJson(new Message(transform, true, false));
        }

        public void Release(Hand controller) {
            follow = null;
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
                    trainManager.stop = !trainManager.stop;
                    ctx.SendJson(new Message(transform, true, true));
                    Invoke("Respawn", 5);
                    return;
                }
            }

            ctx.SendJson(new Message(transform, true, false));
        }

        private void Respawn() {
            owner = false;
            particles = false;
            transform.position = startingPosition;
            transform.rotation = startingRotation;
            GetComponent<MeshRenderer>().enabled = true;
        }
    }
}
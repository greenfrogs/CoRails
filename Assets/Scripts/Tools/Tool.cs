using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Networking;
using Ubiq.Messaging;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tools
{
    public class Tool : MonoBehaviour, INetworkComponent, INetworkObject, IGraspable
    {
        public GameObject respawnPoint;
        public Transform follow;
        public bool owner;
        public bool followRespawn = true;
        public Rigidbody rb;
        public bool mineCoolDown = true;
        private IEnumerator resetFollowEnumerator;

        private Scoring.ScoringEvent onScoreEvent;
        private WorldManager worldManager;

        public AudioClip soundEffect;

        public NetworkId Id { get; private set; }
        
        private NetworkContext ctx;
        public bool useCustomNetworking;

        private List<Material> originalMaterials;
        private List<Material> greyedMaterials;
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

        private void Awake()
        {
            if (!TryGetComponent(out Renderer cooldownRenderer)) return;
            originalMaterials = cooldownRenderer.materials.ToList();
            greyedMaterials = new List<Material>();
            foreach (var originalMaterial in originalMaterials) {
                var material = new Material(originalMaterial);
                var materialColor = material.color;
                materialColor.a = 0.2f;
                material.color = materialColor;
                    
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat(SrcBlend, (float)BlendMode.One);
                material.SetFloat(DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat(ZWrite, 0.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                greyedMaterials.Add(material);
            }
        }

        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        // broadcast position and whether a player has claimed it
        private struct Message
        {
            public TransformMessage ToolTransform;
            public bool Claim;

            public Message(Transform transform, bool claim)
            {
                ToolTransform = new TransformMessage(transform);
                Claim = claim;
            }
        }


        // stop the respawn timer, broadcast claim, and set positions to follow the controller
        protected void GraspShared(Hand controller)
        {
            if (resetFollowEnumerator != null) StopCoroutine(resetFollowEnumerator);
            followRespawn = false;
            follow = controller.transform;
            rb.isKinematic = true;
            owner = true;
            if (!useCustomNetworking)  ctx.SendJson(new Message(transform, true));
        }
        
        // stop following the controller, start the countdown timer, broadcast release
        protected void ReleaseShared(Hand controller)
        {
            follow = null;
            owner = false;
            rb.isKinematic = false;
            resetFollowEnumerator = ResetFollow();
            StartCoroutine(resetFollowEnumerator);
            if (!useCustomNetworking)  ctx.SendJson(new Message(transform, false));
        }

        protected void StartShared(NetworkId netID)
        {
            rb = GetComponent<Rigidbody>();
            onScoreEvent = GameObject.Find("Scoring").GetComponent<Scoring>().OnScoreEvent;

            Id = netID;
            if (!useCustomNetworking)  ctx = NetworkScene.Register(this);
        }

        // wait 5 seconds and respawn at the train
        protected IEnumerator ResetFollow()
        {
            yield return new WaitForSeconds(5);
            followRespawn = true;
        }
        
        // reset cooldown effects after mining timer is up
        private IEnumerator ResetCooldown()
        {
            yield return new WaitForSeconds(1);
            Debug.Log("reset!");
            mineCoolDown = true;
            if (TryGetComponent(out Renderer cooldownRenderer)) {
                cooldownRenderer.materials = originalMaterials.ToArray();
            }
        }

        // move the tool to the broadcast position, and start the respawn timer if it is unclaimed
        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            if (useCustomNetworking) return;
            
            var msg = message.FromJson<Message>();
            
            var transform1 = transform;
            rb.MoveRotation(msg.ToolTransform.rotation);
            rb.MovePosition(transform1.position - transform1.localPosition + msg.ToolTransform.position);

            if (msg.Claim)
            {
                if (resetFollowEnumerator != null)  StopCoroutine(resetFollowEnumerator);
                followRespawn = false;
                rb.isKinematic = true;
            }
            else
            {
                resetFollowEnumerator = ResetFollow();
                StartCoroutine(resetFollowEnumerator);
                rb.isKinematic = false;
            }
        }

        protected void SharedUpdate(Vector3 initAngles, float xAngle, float yAngle, float zAngle, Vector3 positionOffset)
        {
            var transform1 = transform;
            
            // if the tool has fallen off the map, respawn it at the train
            if (transform1.position.y < -30)
            {
                followRespawn = true;
            }

            // if the tool is respawned, follow the train
            if (followRespawn)
            {
                transform1.position = respawnPoint.transform.position;
                rb.isKinematic = true;
                transform1.eulerAngles = initAngles;
                // ReSharper disable once Unity.InefficientPropertyAccess -- stop jitter while on train
                rb.isKinematic = false;
                return;
            }

            if (!owner) return;
            if (follow == null) return;

            // if the tool is the owner, follow the controller position/angle and broadcast the new orientation
            var rotation = follow.transform.rotation;
            rb.MoveRotation(rotation * Quaternion.Euler(xAngle, yAngle, zAngle));
            
            var targetPosition = follow.transform.position + rotation * Quaternion.Euler(xAngle, yAngle, zAngle) * positionOffset;
            if (Vector3.Distance(rb.position, targetPosition) > 0.5) rb.position = targetPosition;
            rb.MovePosition(targetPosition);

            var msg = new Message(transform, true);
            if (!useCustomNetworking)  ctx.SendJson(msg);
        }

        // attempt to mine an object if it is compatible with the tool, spawn the resource if successful, and start the cooldown
        protected void HandleCollision(Collision collision, GameObject prefab, string key, ScoreEventType eventType)
        {
            if (!mineCoolDown || !collision.gameObject.name.Contains(key)) return;
            // var spawnPos = collision.gameObject.transform.position;
            // Destroy(collision.gameObject);
            // Instantiate(prefab, spawnPos, Quaternion.identity);
            
            if (TryGetComponent( out AudioSource audioSource)) {
                audioSource.PlayOneShot(soundEffect, 1f);
                Debug.Log("Sound");
            }
            
            if (worldManager == null)
            {
                worldManager = GameObject.Find("Scene Manager").GetComponent<WorldManager>();
            }
            worldManager.OnWorldUpdate.Invoke(collision.gameObject, prefab);
            mineCoolDown = false;
            StartCoroutine(ResetCooldown());
            if (TryGetComponent(out Renderer toolRenderer)) {
                toolRenderer.materials = greyedMaterials.ToArray();
            }
            onScoreEvent.Invoke(eventType);
        }

        public void Grasp(Hand controller)
        {
            GraspShared(controller);
        }

        public void Release(Hand controller)
        {
            ReleaseShared(controller);
        }
    }
}
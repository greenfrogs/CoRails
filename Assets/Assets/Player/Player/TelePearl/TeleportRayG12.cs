using System;
using System.Linq;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Events;

namespace Prefabs.TelePearl
{
    [RequireComponent(typeof(LineRenderer))]
    public class TeleportRayG12 : MonoBehaviour
    {
        [Serializable]
        public class TeleportEvent : UnityEvent<Vector3, Vector3>
        {
        }

        public TeleportEvent OnTeleport;

        [HideInInspector] public Vector3 teleportLocation;

        [HideInInspector] public bool teleportLocationValid;

        public bool isTeleporting;

        private new LineRenderer renderer;

        public static readonly float launchSpeed = 5f;
        private readonly float stepCoefficient = 8f;
        public static readonly float gravityOverride = 2f;
        private readonly int segments = 128;
        private Vector3 pearlLaunchForward;
        private const float ExpFilterWeight = 0.99f;
        private const float ExpFilterTimeStep = 0.003f;  // in seconds
        private float elapsedTime;
        private float lastForwardUpdateTime;

        private Color validColour = new Color(0f, 1f, 0f, 0.4f);
        private Color collisionColour = new Color(1f, 1f, 0f, 0.4f);
        private Color invalidColour = new Color(1f, 0f, 0f, 0.4f);

        private void Awake()
        {
            renderer = GetComponent<LineRenderer>();
            renderer.useWorldSpace = true;

            if (OnTeleport == null)
            {
                OnTeleport = new TeleportEvent();
            }
        }

        private void Start()
        {
            foreach (IPrimaryButtonProvider item in GetComponentsInParent<MonoBehaviour>()
                         .Where(c => c is IPrimaryButtonProvider))
            {
                item.PrimaryButtonPress.AddListener(UpdateTeleport);
            }
        }

        public void UpdateTeleport(bool teleporterActivation)
        {
            if (teleporterActivation)
            {
                pearlLaunchForward = Vector3.down;
                lastForwardUpdateTime = Time.time;
                isTeleporting = true;
            }
            else
            {
                if (teleportLocationValid)
                {
                    OnTeleport.Invoke(transform.position, pearlLaunchForward);
                }

                isTeleporting = false;
            }
        }

        private void Update()
        {
            if (isTeleporting)
            {
                ComputeArc();
                renderer.enabled = true;
            }
            else
            {
                renderer.enabled = false;
            }
        }

        private void ComputeArc()
        {
            // Compute an arc using Euler's method, inspired by Valve's teleporter.

            teleportLocationValid = false;
            renderer.sharedMaterial.color = invalidColour;

            var positions = new Vector3[segments];

            var numSegments = 1;
            var step = stepCoefficient / segments;
            var dy = Vector3.down * gravityOverride;
            
            var forward = transform.forward;

            elapsedTime += Time.time - lastForwardUpdateTime;
            while (elapsedTime > ExpFilterTimeStep)
            {
                pearlLaunchForward = ExpFilterWeight * pearlLaunchForward + (1f - ExpFilterWeight) * forward;
                elapsedTime -= ExpFilterTimeStep;
            }
            lastForwardUpdateTime = Time.time;
            var velocity = pearlLaunchForward * launchSpeed;

            positions[0] = transform.position;
            for (var i = 1; i < segments; i++)
            {
                positions[i] = positions[i - 1] + velocity * step;
                velocity += dy * step;

                numSegments++;

                if (!Physics.Linecast(positions[i - 1], positions[i], out var raycastHitInfo)) continue;
                if (raycastHitInfo.collider.isTrigger) continue;
                
                if (raycastHitInfo.collider.CompareTag("Teleport"))
                {
                    positions[i] = raycastHitInfo.point;
                    teleportLocation = raycastHitInfo.point;
                    teleportLocationValid = true;
                    break;
                }
                renderer.sharedMaterial.color = collisionColour;
                break;
            }

            renderer.positionCount = numSegments;
            renderer.SetPositions(positions);

            renderer.startWidth = 0.01f;
            renderer.endWidth = 0.01f;

            if (teleportLocationValid)
            {
                renderer.sharedMaterial.color = validColour;
            }
        }
    }
}
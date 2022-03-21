using Prefabs.Player;
using Prefabs.TelePearl;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.Player.Player.TelePearl
{
    public class TelePearl : MonoBehaviour
    {
        private Vector3 startPos;
        private Rigidbody mRigidBody;

        public class PearlLandEvent : UnityEvent<Vector3>
        {
        }

        public PearlLandEvent OnPearlLand;

        private void Awake()
        {
            OnPearlLand ??= new PearlLandEvent();
        }

        private void FixedUpdate()
        {
            mRigidBody.AddForce(Vector3.up * (9.81f - TeleportRayG12.gravityOverride) * mRigidBody.mass);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            
            var dist = Vector3.Distance(transform.position, startPos);
            if (dist <= 0.8f)
            {
                return;
            }
            
            if (other.CompareTag("Teleport"))
                OnPearlLand.Invoke(transform.position);
            Destroy(gameObject);
        }

        private void Start()
        {
            startPos = transform.position;
            mRigidBody = GetComponent<Rigidbody>();
        }
    }
}
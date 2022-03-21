using System;
using System.Collections;
using System.Collections.Generic;
using Org.BouncyCastle.Bcpg.OpenPgp;
using UnityEditor;
using UnityEngine;
using ResourceDrops;

namespace Tools
{
    public class SuctionManager : MonoBehaviour
    {
        public GameObject target;
        public GameObject holoRail; // holographic rail prefab
        public GameObject currentHolo; // currently displayed rail hologram

        public HashSet<Tuple<GameObject, ResourceDropManager>> currentlySucking;
        public List<GameObject> ground;
        private VacuumManager parent;

        public SuctionManager()
        {
            currentlySucking = new HashSet<Tuple<GameObject, ResourceDropManager>>();
            ground = new List<GameObject>();
        }

        private void Start()
        {
            parent = GetComponentInParent<VacuumManager>();
        }

        // attempt to suck a ResourceDrop or add a ground tile for rail placement tracking upon collision
        private void OnTriggerEnter(Collider collision)
        {
            if (!collision.CompareTag("ResourceDrop"))
            {
                if (collision.name.Contains("ground"))
                {
                    ground.Add(collision.gameObject);
                    AttemptHoloRailProjection();
                }

                return;
            }

            currentlySucking.Add(new Tuple<GameObject, ResourceDropManager>(collision.gameObject,
                collision.gameObject.GetComponent<ResourceDropManager>()));
        }

        // stop tracking colliding ground tile when it leaves collision area, redraw hologram to reflect changes
        private void OnTriggerExit(Collider other)
        {
            if (other.name.Contains("ground"))
            {
                ground.Remove(other.gameObject);
                AttemptHoloRailProjection();
            }
        }

        // apply a constant motion to all sucked objects towards the collection zone
        private void FixedUpdate()
        {
            // new Tuple<GameObject, ResourceDropManager>
            foreach (var tup in currentlySucking)
            {
                var (o, rdp) = tup;
                if (o.transform.position != target.transform.position)
                {
                    o.transform.position = Vector3.MoveTowards(o.transform.position, target.transform.position,
                        5f * Time.deltaTime);
                    rdp.ForceSendPositionUpdate();
                }
                else
                {
                    // drop reached gun
                    currentlySucking.Remove(tup);
                }
            }
        }

        public void RemoveObj(Tuple<GameObject, ResourceDropManager> tup)
        {
            currentlySucking.Remove(tup);
        }

        public void CreateHoloRail(float x, float z)
        {
            DestroyCurrentHolo();
            currentHolo = Instantiate(holoRail, new Vector3(x, 0, z), Quaternion.Euler(-90, 0, 0));
        }

        public void DestroyCurrentHolo()
        {
            if (currentHolo != null)
            {
                Destroy(currentHolo);
                currentHolo = null;
            }
        }

        // Try to place a holographic rail at the latest collided ground object, provided you are able to place a rail
        public void AttemptHoloRailProjection()
        {
            if (parent.inventoryItem == 3 && !parent.placeCoolDown)
            {
                var tail = ground.Count - 1;
                if (ground.Count > 0)
                {
                    CreateHoloRail(ground[tail].transform.position.x, ground[tail].transform.position.z);
                }
                else
                {
                    DestroyCurrentHolo();
                }
            }
        }

        // Clear all lists when disabled to prevent undefined behaviour
        private void OnDisable()
        {
            currentlySucking.Clear();
            ground.Clear();
            DestroyCurrentHolo();
        }
    }
}
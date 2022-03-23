using System;
using System.Collections.Generic;
using ResourceDrops;
using Trains;
using UnityEngine;

namespace Tools {
    public class SuctionManager : MonoBehaviour {
        public GameObject target;
        public GameObject holoRail; // holographic rail prefab
        public GameObject currentHolo; // currently displayed rail hologram
        public GameObject collectionSphere;

        public HashSet<Tuple<GameObject, ResourceDropManager>> currentlySucking;
        public List<GameObject> ground;
        private VacuumManager parent;
        
        public TrackManager trackManager;

        public SuctionManager() {
            currentlySucking = new HashSet<Tuple<GameObject, ResourceDropManager>>();
            ground = new List<GameObject>();
        }

        private void Start() {
            parent = GetComponentInParent<VacuumManager>();
        }

        // apply a constant motion to all sucked objects towards the collection zone
        private void FixedUpdate() {
            // new Tuple<GameObject, ResourceDropManager>
            foreach (Tuple<GameObject, ResourceDropManager> tup in currentlySucking) {
                (GameObject o, ResourceDropManager rdp) = tup;
                if (o.transform.position != target.transform.position) {
                    o.transform.position = Vector3.MoveTowards(o.transform.position, target.transform.position,
                        5f * Time.deltaTime);
                    rdp.ForceSendPositionUpdate();
                }
                else {
                    // drop reached gun
                    currentlySucking.Remove(tup);
                }
            }
        }

        // Clear all lists when disabled to prevent undefined behaviour
        private void OnDisable() {
            currentlySucking.Clear();
            ground.Clear();
            DestroyCurrentHolo();
            collectionSphere.SetActive(false);
        }

        // attempt to suck a ResourceDrop or add a ground tile for rail placement tracking upon collision
        private void OnTriggerEnter(Collider collision) {
            if (!collision.CompareTag("ResourceDrop")) {
                if (collision.name.Contains("ground")) {
                    ground.Add(collision.gameObject);
                    AttemptHoloRailProjection();
                }
                return;
            }

            collectionSphere.SetActive(true);
            currentlySucking.Add(new Tuple<GameObject, ResourceDropManager>(collision.gameObject,
                collision.gameObject.GetComponent<ResourceDropManager>()));
        }

        // stop tracking colliding ground tile when it leaves collision area, redraw hologram to reflect changes
        private void OnTriggerExit(Collider other) {
            if (other.name.Contains("ground")) {
                ground.Remove(other.gameObject);
                AttemptHoloRailProjection();
            }
        }

        public void RemoveObj(Tuple<GameObject, ResourceDropManager> tup) {
            currentlySucking.Remove(tup);
            if (currentlySucking.Count == 0) collectionSphere.SetActive(false);
        }

        public void CreateHoloRail(float x, float z) {
            DestroyCurrentHolo();
            currentHolo = Instantiate(holoRail, new Vector3(x, 0, z), Quaternion.Euler(-90, 0, 0));
        }

        public void DestroyCurrentHolo() {
            if (currentHolo != null) {
                Destroy(currentHolo);
                currentHolo = null;
            }
        }

        // Try to place a holographic rail at the latest collided ground object, provided you are able to place a rail
        public void AttemptHoloRailProjection() {
            if (parent.inventoryItem == 3 && !parent.placeCoolDown) {
                int tail = ground.Count - 1;
                if (ground.Count > 0)
                {
                    if (trackManager.Exists((int) ground[tail].transform.position.x,
                            (int) ground[tail].transform.position.z))
                    {
                        DestroyCurrentHolo();
                        return;
                    }
                    
                    foreach (Transform child in ground[tail].transform)
                    {
                        if (child.CompareTag("Untagged"))
                        {
                            if (child.name.Contains("tree") || child.name.Contains("stone"))
                            {
                                DestroyCurrentHolo();
                                return;
                            }
                        }
                    }
                    CreateHoloRail(ground[tail].transform.position.x, ground[tail].transform.position.z);
                }
                else
                    DestroyCurrentHolo();
            }
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using TMPro;
using Trains;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.XR;

namespace Tools
{
    public class VacuumManager : Tool, INetworkComponent, INetworkObject, IGraspable, IUseable
    {
        public GameObject suctionZone;
        public GameObject collectionSphere;
        public int inventoryCount;
        public int inventoryItem;  // 0 = None, 1 = log, 2 = rock, 3 = rail
        public TextMeshPro inventoryDisplay;
        public TrackManager trackManager;
        public bool placeCoolDown;  // false = able to place, true = on cooldown from previously sucking

        // broadcast internal inventory, position, and if it has been claimed by a player
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
        private struct VacuumMessage
        {
            public TransformMessage Transform;
            public bool Claim;
            public int Count;
            public int Item;

            public VacuumMessage(Transform transform, bool claim, int count, int item)
            {
                Transform = new TransformMessage(transform);
                Claim = claim;
                Count = count;
                Item = item;
            }
        }

        private NetworkContext ctx;
        
        public new void Grasp(Hand controller)
        {
            GraspShared(controller);
            ctx.SendJson(new VacuumMessage(transform, true, inventoryCount, inventoryItem));    // broadcast player claim
        }
        
        public new void Release(Hand controller)
        {
            ReleaseShared(controller);
            ctx.SendJson(new VacuumMessage(transform, false, inventoryCount, inventoryItem));   // broadcast player unclaim
        }
        
        public void Use(Hand controller)
        {
            SuctionMode(true);  // enable suction/rail placement
        }

        public void UnUse(Hand controller)
        {
            // if it has sucked an object this trigger pull, enable rail placement next trigger pull
            if (placeCoolDown)  
            {
                placeCoolDown = false;
                SuctionMode(false);
                return;
            }
            
            // if it can place rails, attempt to place one at the latest colliding ground tile
            if (inventoryItem == 3)
            {
                var suckObject = suctionZone.GetComponent<SuctionManager>();
                if (suckObject.currentlySucking.Count == 0 && suckObject.ground.Count != 0)
                {
                    var tail = suckObject.ground.Count - 1;
                    
                    // if a rail already exists here, do nothing
                    if (trackManager.Exists((int) suckObject.ground[tail].transform.position.x,
                            (int) suckObject.ground[tail].transform.position.z))
                    {
                        return;
                    }
                    
                    // clear above ground debris
                    foreach (Transform child in suckObject.ground[tail].transform)
                    {
                        Destroy(child.gameObject);
                    }
                    
                    // add rail to the trackManager for placement
                    trackManager.Add((int) suckObject.ground[tail].transform.position.x, (int) suckObject.ground[tail].transform.position.z);
                    inventoryCount -= 1;
                    if (inventoryCount == 0)
                    {
                        inventoryItem = 0;
                    }
                    ctx.SendJson(new VacuumMessage(transform, true, inventoryCount, inventoryItem));    // broadcast new inventory
                }
                suckObject.DestroyCurrentHolo();    // destroy projection once rail is placed
            }
            SuctionMode(false);
        }

        public new NetworkId Id { get; } = new NetworkId(1002);
        // Start is called before the first frame update
        private void Start()
        {
            useCustomNetworking = true;
            ctx = NetworkScene.Register(this);
            StartShared(new NetworkId());  // ignore this network id
        }
        
        // move object to received position and inventory, if it has been unclaimed start the respawn timer
        public new void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<VacuumMessage>();

            var transform1 = transform;
            transform1.localPosition = msg.Transform.position;
            transform1.localRotation = msg.Transform.rotation;

            if (msg.Claim)
            {
                StopCoroutine(ResetFollow());
                followRespawn = false;
            }
            else
            {
                StartCoroutine(ResetFollow());
            }

            inventoryCount = msg.Count;
            inventoryItem = msg.Item;
        }

        // attempt to add an object to the inventory, if it is incompatible return false
        public bool AddItem(string item)
        {
            var code = item switch
            {
                "log" => 1,
                "rock" => 2,
                "rail" => 3,
                _ => 0
            };
            
            placeCoolDown = true;

            if (inventoryItem != 0 && inventoryItem != code) return false;
            inventoryItem = code;
            inventoryCount += 1;
            ctx.SendJson(new VacuumMessage(transform, true, inventoryCount, inventoryItem));
            return true;
        }

        // set suction and collection active or inactive
        private void SuctionMode(bool activate)
        {
            suctionZone.SetActive(activate);
            collectionSphere.SetActive(activate);
        }
        
        // show what is currently in the inventory above the gun
        private void UpdateDisplay()
        {
            var str = inventoryItem switch
            {
                1 => "log",
                2 => "rock",
                3 => "rail",
                _ => ""
            };
            str += "\n";
            str = str + "  " + inventoryCount;
            inventoryDisplay.text = str;
        }
        // Update is called once per frame
        private void Update()
        {            
            UpdateDisplay();
            SharedUpdate(new Vector3(270, 0, 0), -65, 180, 0, 
                Vector3.forward * -0.1f + Vector3.left * -0.04f + Vector3.up * 0.04f);
            if (!owner) return;
            if (follow == null) return;
            ctx.SendJson(new VacuumMessage(transform, true, inventoryCount, inventoryItem));
        }
    }
}
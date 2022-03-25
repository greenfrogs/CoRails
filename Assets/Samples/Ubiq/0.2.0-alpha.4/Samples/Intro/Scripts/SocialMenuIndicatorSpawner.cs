using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class SocialMenuIndicatorSpawner : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public SocialMenu socialMenu;
        public GameObject indicatorTemplate;

        private RoomClient roomClient;
        private NetworkScene networkScene;
        private string roomUUID;

        private NetworkContext context;
        private struct SocialMenuMessage {
            public string SpawnedUUID;
            public string PeerUUID;
        }
        private readonly Dictionary<string, string> peerToPeerMenu = new Dictionary<string, string>();

        private void Start() {
            context = NetworkScene.Register(this);
            if (socialMenu && socialMenu.roomClient && socialMenu.networkScene)
            {
                roomClient = socialMenu.roomClient;
                networkScene = socialMenu.networkScene;
                roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
                roomClient.OnPeerRemoved.AddListener(RoomClient_OnPeerRemoved);
                roomClient.OnPeerAdded.AddListener(RoomClient_OnPeerAdded);
            }
        }

        private void RoomClient_OnPeerAdded(IPeer newPeer) {
            foreach (SocialMenuMessage msg in peerToPeerMenu.Select(kvp => new SocialMenuMessage() {
                PeerUUID = kvp.Key,
                SpawnedUUID = kvp.Value,
            })) {
                context.SendJson(msg);
            }
        }

        private void OnDestroy()
        {
            if (roomClient)
            {
                roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }
        }

        private void RoomClient_OnPeerRemoved(IPeer removedPeer) {
            // GameObject peerNetMenuIndicator = networkScene.gameObject.GetComponentsInChildren<NetworkedMainMenuIndicator>().Where(mb => mb.gameObject.name.Contains($"#")).FirstOrDefault() as ISpawnable
            Debug.Log($"Removing peer {removedPeer.UUID}'s menu indicator");
            if (!peerToPeerMenu.ContainsKey(removedPeer.UUID)) return;

            Transform peerNetMenuIndicator = networkScene.transform.Find($"Scene Manager/Menu Indicator#{peerToPeerMenu[removedPeer.UUID]}");
            if (peerNetMenuIndicator == null) return;
            Debug.Log($"Removed peer {removedPeer.UUID}'s menu indicator 'Scene Manager/Menu Indicator#{peerToPeerMenu[removedPeer.UUID]}'");
            Destroy(peerNetMenuIndicator.gameObject);
        }

        private void RoomClient_OnJoinedRoom(IRoom room) {
            if (!roomClient || !networkScene || roomClient.Room == null || roomClient.Room.UUID == roomUUID) return;
            // destroy menu indicators from previous rooms
            foreach (NetworkedMainMenuIndicator remoteMenuIndicator in networkScene.gameObject.GetComponentsInChildren<NetworkedMainMenuIndicator>()) {
                Destroy(remoteMenuIndicator.gameObject);
            }
            roomUUID = roomClient.Room.UUID;

            var spawner = NetworkSpawner.FindNetworkSpawner(networkScene);
            var indicator = spawner.SpawnPersistent(indicatorTemplate);
                
            // Debug.Log();  // was going to implement destroying menu server-side on leave, but not worth given time remaining
            string spawnedNetId = ((INetworkObject)indicator.GetSpawnableInChildren()).Id.ToString();
            peerToPeerMenu[roomClient.Me.UUID] = spawnedNetId;
                
            context.SendJson(new SocialMenuMessage {
                PeerUUID = roomClient.Me.UUID,
                SpawnedUUID = spawnedNetId,
            });
                
            var bindable = indicator.GetComponent<ISocialMenuBindable>();
            if (bindable != null)
            {
                bindable.Bind(socialMenu);
            }
        }
        public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
            var msg = message.FromJson<SocialMenuMessage>();
            peerToPeerMenu[msg.PeerUUID] = msg.SpawnedUUID;
        }

        public NetworkId Id {
            get;
        } = new NetworkId(7279);
    }
}
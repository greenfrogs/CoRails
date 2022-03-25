using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Trains;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class BgmManager : MonoBehaviour, INetworkComponent, INetworkObject
{
    [FormerlySerializedAs("BackMusic")] public AudioSource backMusic;
    private NetworkContext netContext;
    public NetworkScene networkScene;
    public bool ready;

    private RoomClient roomClient;
    NetworkId INetworkObject.Id => new NetworkId(685670);
    
    public TrainManager trainManager;
    public bool running;
    public bool playing;

    private void Awake() {
        networkScene = (NetworkScene) FindObjectOfType(typeof(NetworkScene));
        roomClient = networkScene.GetComponent<RoomClient>();
        roomClient.OnPeerAdded.AddListener(SendAudioState);
        roomClient.OnJoinedRoom.AddListener(InitState);
    }

    private void Start() {
        backMusic = GetComponent<AudioSource>();
        netContext = NetworkScene.Register(this);
        
    }

    private void Update() {
        if (running) {
            if (playing) {
                return;
            }
            backMusic.Play();
            playing = true;
        }
        else {
            running = !trainManager.stop;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
        var msg = message.FromJson<Message>();
        if (ready && msg.Joining) return;
        backMusic.timeSamples = msg.SeekTime;
        ready = true;
    }
    
    private void SendAudioState(IPeer newPeer) {
        int mySuffix = roomClient.Me.UUID.Last();

        // use last character of UUID as integer, lowest integer in room sends new updates to new peer
        bool doSend = roomClient.Peers.Where(peer => peer != newPeer).Select(peer => peer.UUID.Last())
            .All(peerSuffix => peerSuffix > mySuffix);


        if (!doSend) return;
        backMusic = GetComponent<AudioSource>();
        netContext.SendJson(new Message(backMusic.timeSamples, true));
    }

    private IEnumerator SelectHost() {
        ready = false;
        const int timeoutMax = 5; // give 500ms for initializing world sync
        bool roomHasPeers = false;
        for (int timeoutTicker = 0; timeoutTicker < timeoutMax; timeoutTicker++) {
            if (roomClient.Peers.Any()) // waiting for peers to join within timeout period
            {
                roomHasPeers = true;
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (roomHasPeers)
            yield break; // don't destroy terrain, peer(s) exist so wait for initState to be sent by someone else
        backMusic.Stop();
        backMusic.timeSamples = 0;
        playing = false;
        running = false;
        ready = true; // we just joined (created) an empty room, we get to set the room's seed.
    }

    private void InitState(IRoom newRoom) {
        StartCoroutine(SelectHost());
    }

    private struct Message {
        // ReSharper disable all FieldCanBeMadeReadOnly.Local

        public int SeekTime;
        public bool Joining;

        public Message(int seekTime, bool joining) {
            SeekTime = seekTime;
            Joining = joining;
        }
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using SIPSorcery.Sys;
// using Ubiq.Messaging;
// using Ubiq.Rooms;
// using Ubiq.Samples;
// using UnityEngine;
// using Utils;
//
// public class ssScore : MonoBehaviour, INetworkComponent, INetworkObject, ISpawnable
// {
//     public NetworkId Id { get; set; }
//
//     public void OnSpawned(bool local)
//     {
//         roomClient = GameObject.Find("Network Scene with Social").GetComponent<RoomClient>();
//         roomClient.OnPeerAdded.AddListener(SendScoreToNewPeer);
//         Debug.Log("SsScore: hello!");
//         localScoreObject = GameObject.Find("Scoring").GetComponent<Scoring>();
//         if (localScoreObject.initBy.Valid && !localScoreObject.initBy.Equals(Id))
//         {
//             Debug.Log("SsScore: goodbye!");
//             Destroy(this);
//         }
//         netContext = NetworkScene.Register(this);
//     }
//
//     private RoomClient roomClient;
//     private NetworkContext netContext;
//
//     private Scoring localScoreObject;
//     
//     
//     // todo: clean this mess up
//     private readonly DictionaryWithDefault<int, float> scorePerType = new DictionaryWithDefault<int, float>(0f)
//     {
//         {5, 0f}
//     };
//     private readonly DictionaryWithDefault<int, float> scoreMapping = new DictionaryWithDefault<int, float>(5f);
//     private float startTimeOffset;
//     
//     
//     private struct SsScoreMessage
//     {
//         public Dictionary<int, float> ScorePerType;
//         public Dictionary<int, float> ScoreMapping;
//         public float startTimeOffset;
//         public string targetPeer;
//         public NetworkId SsScoreNID;
//     }
//
//
//     public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
//     {
//         var msg = message.FromJson<SsScoreMessage>();
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         Debug.Log("Received ssscore message");
//         // ignore messages not targeted at us, this is only used when joining a room.
//         if (msg.targetPeer != roomClient.Me.UUID) return;  
//         
//         foreach (var (eventTypeInt, totalScore) in msg.ScorePerType)
//         {
//             localScoreObject.SyncScore((ScoreEventType) eventTypeInt, totalScore);
//         }
//         foreach (var (eventTypeInt, scorePerEvent) in msg.ScoreMapping)
//         {
//             localScoreObject.SyncScoreMapping((ScoreEventType) eventTypeInt, scorePerEvent);
//         }
//         localScoreObject.initBy = Id;
//     }
//
//     public void SyncStartTime(float startTime)
//     {
//         startTimeOffset = startTime;
//     }
//
//     public void UpdateScore(ScoreEventType eventType)
//     {
//         scorePerType[(int) eventType] += scoreMapping[(int) eventType];
//     }
//
//     void SendScoreToNewPeer(IPeer newPeer)
//     {
//         Debug.Log(newPeer.UUID);
//         var m = new SsScoreMessage
//         {
//             ScorePerType = scorePerType,
//             ScoreMapping = scoreMapping,
//             targetPeer = newPeer.UUID,
//             SsScoreNID = Id,
//         };
//         netContext.SendJson(m);
//     }
//
// }

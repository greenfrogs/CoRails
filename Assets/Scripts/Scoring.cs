using System;
using System.Collections.Generic;
using System.Linq;
using Trains;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utils;

public enum ScoreEventType {
    StoneMine,
    WoodMine,
    StonePickUp,
    WoodPickUp,
    TrackPlace,
    BonusTimeDecay,
    Won,
}

public class Scoring : MonoBehaviour, INetworkComponent, INetworkObject {
    private const float BonusTimeScore = 1000f;
    private const float BonusTimeDecayRate = 1.25f;
    
    public Canvas canvas;
    public Text scoreText;

    public TrainManager trainManager;

    private readonly DictionaryWithDefault<int, float> scoreMapping = new DictionaryWithDefault<int, float>(6f);

    private readonly DictionaryWithDefault<int, float> scorePerType = new DictionaryWithDefault<int, float>(0f) {
        {6, 0f}
    };

    private NetworkContext netContext;

    public ScoringEvent OnScoreEvent;

    private RoomClient roomClient;
    private bool running;
    private float score;
    private float startTimeOffset;

    private void Awake() {
        OnScoreEvent ??= new ScoringEvent();
        OnScoreEvent.AddListener(UpdateScore);
        roomClient = GameObject.Find("Network Scene with Social").GetComponent<RoomClient>();
        roomClient.OnPeerAdded.AddListener(SendScore);
    }

    private void Start() {
        netContext = NetworkScene.Register(this);
        canvas.gameObject.SetActive(true);
    }

    // Update is called once per frame
    private void Update() {
        if (running) {
            if (trainManager.won) {
                scorePerType[(int) ScoreEventType.Won] = 1000;
            }
            else if (trainManager.failed) {
                scorePerType[(int) ScoreEventType.Won] = -1000;
            }
            else {
                scorePerType[(int) ScoreEventType.BonusTimeDecay] =
                    Mathf.Max(BonusTimeScore - (Time.time - startTimeOffset) * BonusTimeDecayRate, 0f);
            }

            score = 0;
            score += scorePerType.Sum(x => x.Value);
        }
        else {
            running = !trainManager.stop;
            startTimeOffset = Time.time;
        }

        scoreText.text = "";

        if (trainManager.failed) {
            scoreText.text += "Train has Derailed!!!\n";
        }
        else if (trainManager.won) {
            scoreText.text += "You have Won!!!!\n";
        }

        scoreText.text += "Score: " + Math.Round(score);
    }

    public void Reset() {
        running = false;
        startTimeOffset = Time.time;
        score = 0;
        scorePerType.Clear();
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message) {
        var scoringMessage = message.FromJson<ScoringMessage>();
        if (scoringMessage.TargetUuid == roomClient.Me.UUID) {
            if (scoringMessage.ScorePerType != null)
                foreach ((int eventType, float eventScore) in scoringMessage.ScorePerType)
                    scorePerType[eventType] = eventScore;

            Debug.Log(Time.time);
            Debug.Log(scoringMessage.CurrentSessionTime);
            startTimeOffset = Time.time - scoringMessage.CurrentSessionTime;
            Debug.Log(startTimeOffset);
            return;
        }

        scorePerType[(int) scoringMessage.EventType] += scoreMapping[(int) scoringMessage.EventType];
    }

    NetworkId INetworkObject.Id => new NetworkId(2817);

    private void SendScore(IPeer newPeer) {
        int mySuffix = roomClient.Me.UUID.Last();

        // use last character of UUID as integer, lowest integer in room sends new updates to new peer
        bool doSend = roomClient.Peers.Select(peer => peer.UUID.Last()).All(peerSuffix => peerSuffix > mySuffix);

        if (!doSend) return;
        var message = new ScoringMessage {
            ScorePerType = scorePerType.Select(x => new Tuple<int, float>(x.Key, x.Value)).ToList(),
            CurrentSessionTime = Time.time - startTimeOffset,
            TargetUuid = newPeer.UUID
        };
        Debug.Log(message);
        netContext.SendJson(message);
    }

    private void UpdateScore(ScoreEventType eventType) {
        scorePerType[(int) eventType] += scoreMapping[(int) eventType];
        var m = new ScoringMessage {
            EventType = eventType
        };
        netContext.SendJson(m);
    }

    private struct ScoringMessage {
        public ScoreEventType EventType;

        public string TargetUuid; // for new players joining

        public List<Tuple<int, float>> ScorePerType;

        public float CurrentSessionTime;
    }

    public class ScoringEvent : UnityEvent<ScoreEventType> { }
}
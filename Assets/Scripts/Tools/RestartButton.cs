using Trains;
using Ubiq.Messaging;
using UnityEngine;

namespace Tools {
    public class RestartButton : PhysicalButton {
        public TrackManager trackManager;
        public NetworkId Id => new NetworkId(13654656);

        public override void Run() {
            trackManager.Clear();
            Application.LoadLevel(Application.loadedLevel);
        }
    }
}
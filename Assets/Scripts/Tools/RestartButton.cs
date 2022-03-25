using Networking;
using Trains;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using Terrain = Terrain_Generation.Terrain;

namespace Tools {
    public class RestartButton : PhysicalButton {
        public TrackManagerSnake trackManager;
        public Scoring score;
        public RoomClient roomClient;
        public TrainManager trainManager;
        public Texture blackTexture;
        public NetworkId Id => new NetworkId(13654656);

        public override void Run() {
            Debug.LogError("DELETE EVERYTHING");
            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);
            asyncOperation.completed += (_) => {
                AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync("CoRails");
                asyncOperation.completed += (_) => {
                    AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("CoRails", LoadSceneMode.Additive);
                    asyncOperation.completed += (_) => SceneManager.UnloadSceneAsync("SampleScene");
                };
            };


            // trackManager.Clear();
            // score.Reset();
            // trainManager.Reset();
            // roomClient.Leave();
        }
    }
}
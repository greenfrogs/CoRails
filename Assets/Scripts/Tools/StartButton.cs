using Trains;
using Ubiq.Messaging;

namespace Tools {
    public class StartButton : PhysicalButton {
        public TrainManager trainManager;
        public NetworkId Id => new NetworkId(13654655);

        public override void Run() {
            trainManager.stop = !trainManager.stop;
        }
    }
}
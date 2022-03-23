using System;

namespace Console {
    public class DebugCommandBase {
        public DebugCommandBase(string id, string description, string format) {
            CommandId = id;
            commandDescription = description;
            commandFormat = format;
        }

        public string CommandId { get; }

        public string commandDescription { get; }

        public string commandFormat { get; }
    }

    public class DebugCommand : DebugCommandBase {
        private readonly Action command;

        public DebugCommand(string id, string description, string format, Action command) : base(id, description,
            format) {
            this.command = command;
        }

        public void Invoke() {
            command.Invoke();
        }
    }

    public class DebugCommand<T1> : DebugCommandBase {
        private readonly Action<T1> command;

        public DebugCommand(string id, string description, string format, Action<T1> command) : base(id, description,
            format) {
            this.command = command;
        }

        public void Invoke(T1 value1) {
            command.Invoke(value1);
        }
    }

    public class DebugCommand<T1, T2> : DebugCommandBase {
        private readonly Action<T1, T2> command;

        public DebugCommand(string id, string description, string format, Action<T1, T2> command) : base(id,
            description, format) {
            this.command = command;
        }

        public void Invoke(T1 value1, T2 value2) {
            command.Invoke(value1, value2);
        }
    }
}
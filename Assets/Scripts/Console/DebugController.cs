using System.Collections.Generic;
using System.Text.RegularExpressions;
using Trains;
using UnityEngine;

namespace Console {
    public class DebugController : MonoBehaviour {
        public static DebugCommand<int, int> CREATE_TRACK;
        public static DebugCommand<int> SET_WOOD, SET_STONE;
        public bool showConsole;

        public List<object> commandList;

        public TrackManager trackManager;
        public WoodCart woodCart;
        public StoneCart stoneCart;
        private readonly char[] newLine = "\n\r".ToCharArray();
        private readonly Regex regularExpression = new Regex("^[a-zA-Z0-9_ ]*$");
        private string textField;

        private void Awake() {
            CREATE_TRACK = new DebugCommand<int, int>("track", "Create track at specific location x y", "track",
                (x, y) => {
                    trackManager.RemoveTerrain(x, y, true);
                    trackManager.Add(x, y);
                    trackManager.BroadcastAdd(x, y);
                });

            SET_WOOD = new DebugCommand<int>("wood", "Set count for wood", "wood",
                x => { woodCart.WoodCount = x; });
            SET_STONE = new DebugCommand<int>("stone", "Set count for stone", "stone",
                x => { stoneCart.StoneCount = x; });

            commandList = new List<object> {
                CREATE_TRACK,
                SET_WOOD,
                SET_STONE
            };
        }

        public void Update() {
            if (Input.GetKeyUp(KeyCode.BackQuote)) showConsole = !showConsole;
        }

        private void OnGUI() {
            if (!showConsole) return;

            float y = 0;
            GUI.Box(new Rect(0, y, Screen.width, 30), "");

            GUI.SetNextControlName("TextField");
            string input = GUI.TextArea(new Rect(10f, y + 5f, Screen.width - 20f, 20f), textField);
            GUI.FocusControl("TextField");
            if (input == null) return;

            if (regularExpression.IsMatch(input)) {
                if (input.IndexOfAny(newLine) != -1) {
                    HandleInput();
                    textField = "";
                    showConsole = false;
                }
                else {
                    textField = input;
                }
            }
        }

        private void HandleInput() {
            string[] properties = textField.ToLower().Split(' ');

            foreach (DebugCommandBase command in commandList)
                if (textField.Contains(command.CommandId)) {
                    if (command is DebugCommand debugCommand)
                        debugCommand.Invoke();
                    else if (command is DebugCommand<int> debugCommand2)
                        debugCommand2.Invoke(int.Parse(properties[1]));
                    else if (command is DebugCommand<int, int> debugCommand3)
                        debugCommand3.Invoke(int.Parse(properties[1]), int.Parse(properties[2]));
                }
        }
    }
}
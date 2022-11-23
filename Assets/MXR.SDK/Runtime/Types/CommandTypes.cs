using System;

namespace MXR.SDK {
    /// <summary>
    /// Represents a ManageXR command.
    /// Commands are sent from the MXR Web Dashboard and relayed
    /// to the SDK via the MXR Admin App
    /// </summary>
    [Serializable]
    public class Command {
        /// <summary>
        /// The action for this command
        /// </summary>
        public CommandAction action;

        /// <summary>
        /// Data associated with this command
        /// </summary>
        public string data;
    }

    /// <summary>
    /// The different types of commands supported by the SDK
    /// </summary>
    public enum CommandAction {
        PLAY_VIDEO,
        PAUSE_VIDEO
    }

    /// <summary>
    /// The data associated with a command 
    /// when the action is <see cref="CommandAction.PLAY_VIDEO"/>
    /// </summary>
    [Serializable]
    public class PlayVideoCommandData {
        public string videoId;
        public bool playFromBeginning;
    }

    /// <summary>
    /// The data ssociated with a command when the 
    /// action is <see cref="CommandAction.PAUSE_VIDEO"/>
    /// </summary>
    [Serializable]
    public class PauseVideoCommandData { }
}

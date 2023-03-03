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
        /// The action string used for Play Video commands
        /// </summary>
        public const string PLAY_VIDEO_ACTION = "PLAY_VIDEO";

        /// <summary>
        /// The action string used for Pause Video commands
        /// </summary>
        public const string PAUSE_VIDEO_ACTION = "PAUSE_VIDEO";

        /// <summary>
        /// The action for this command, used to distinguish
        /// different command types.
        /// </summary>
        public string action;

        /// <summary>
        /// Data associated with this command represented in JSON format
        /// </summary>
        public string data;
    }

    /// <summary>
    /// The data associated with a command 
    /// when the action is <see cref="Command.PLAY_VIDEO_ACTION"/>
    /// </summary>
    [Serializable]
    public class PlayVideoCommandData {
        public string videoId;
        public bool playFromBeginning;
    }

    /// <summary>
    /// The data ssociated with a command when the 
    /// action is <see cref="Command.PAUSE_VIDEO_ACTION"/>
    /// </summary>
    [Serializable]
    public class PauseVideoCommandData { }
}

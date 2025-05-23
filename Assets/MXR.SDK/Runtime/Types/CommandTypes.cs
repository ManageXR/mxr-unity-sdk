﻿using System;

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
        /// The action string used for Resume Video commands
        /// </summary>
        public const string RESUME_VIDEO_ACTION = "RESUME_VIDEO";
        
        /// <summary>
        /// The action string used for Launching the MXR Home Screen to a specific view/location. 
        /// </summary>
        public const string LAUNCH_ACTION = "LAUNCH_APP";

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
    /// The data associated with a command when the 
    /// action is <see cref="Command.PAUSE_VIDEO_ACTION"/>
    /// </summary>
    [Serializable]
    public class PauseVideoCommandData { }

    /// <summary>
    /// The data associated with a command when the 
    /// action is <see cref="Command.RESUME_VIDEO_ACTION"/>
    /// </summary>
    [Serializable]
    public class ResumeVideoCommandData { }
        
    /// <summary>
    /// The data associated with a command 
    /// when the action is <see cref="Command.LAUNCH_ACTION"/>
    /// </summary>
    [Serializable]
    public class LaunchMXRHomeScreenCommandData {
        public string launchLocation;
    }
}

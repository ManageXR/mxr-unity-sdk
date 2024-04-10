using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace MXR.SDK {
    /// <summary>
    /// Represents the current state of the homescreen.
    /// This state is sent to the MXR Web Dashboard via
    /// the MXR Admin App for reporting.
    /// </summary>
    [Serializable]
    public class HomeScreenState {
        /// <summary>
        /// The current view of the homescreen
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public HomeScreenView view;

        /// <summary>
        /// The details associated with the state of the homescreen.
        /// Currently the SDK supports video related details.
        /// </summary>
        public HomeScreenData data = new HomeScreenData();
    }

    /// <summary>
    /// The different view types recognised by the SDK. 
    /// </summary>
    [Serializable]
    public enum HomeScreenView {
        LIBRARY,
        VIDEO_PLAYER,
        WIFI,
        ADMIN_PIN,
        ADMIN_SETTINGS,
        SHORTCUT_MENU,
        PASSCODE
    }

    /// <summary>
    /// The different language types recognised by the SDK. 
    /// </summary>
    [Serializable]
    public enum HomeScreenDisplayLanguage {
        enUS,
        frFR,
        deDE,
        esES,
        ukUA,
        ptPT,
        svSE
    }

    /// <summary>
    /// Details associated with the view of the home screen.
    /// Currently contains fields for video reporting.
    /// </summary>
    [Serializable]
    public class HomeScreenData {
        /// <summary>
        /// The ID of the video currently being played
        /// </summary>
        public string videoId;

        /// <summary>
        /// Where in the video is the viewer (in milliseconds)
        /// </summary>
        public long videoLocation;

        /// <summary>
        /// The duration of the video currently being played (in milliseconds)
        /// </summary>
        public long videoDuration;

        /// <summary>
        /// The title of the video currently being played
        /// </summary>
        public string videoTitle;

        /// <summary>
        /// The current state of video playback
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public HomeScreenVideoState videoState;
    }

    /// <summary>
    /// State of video playback
    /// </summary>
    [Serializable]
    public enum HomeScreenVideoState {
        /// <summary>
        /// Whether a video is currently playing
        /// </summary>
        PLAYING,

        /// <summary>
        /// Whether a video is currently paused
        /// </summary>
        PAUSED
    }
}

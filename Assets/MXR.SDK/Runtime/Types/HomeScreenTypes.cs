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
        public HomeScreenView view;

        /// <summary>
        /// The details associated with the view.
        /// Currently the SDK supports video related details.
        /// </summary>
        public HomeScreenViewDetails viewDetails = new HomeScreenViewDetails();
    }

    /// <summary>
    /// The different view types recognised by  the SDK. 
    /// </summary>
    [Serializable]
    public enum HomeScreenView {
        LIBRARY,
        VIDEO_PLAYER,
        WIFI,
        ADMIN_PIN,
        ADMIN_SETTINGS,
        SHORTCUT_MENU
    }

    /// <summary>
    /// Details associated with the view of the home screen.
    /// Currently contains fields for video reporting.
    /// </summary>
    [Serializable]
    public class HomeScreenViewDetails {
        /// <summary>
        /// The ID of the video currently being played
        /// </summary>
        public string videoId;

        /// <summary>
        /// Where in the video is the viewer (in seconds)
        /// </summary>
        public int videoLocation;

        /// <summary>
        /// The duration of the video currently being played (in seconds)
        /// </summary>
        public int videoDuration;

        /// <summary>
        /// The title of the video currently being played
        /// </summary>
        public string videoTitle;

        /// <summary>
        /// The current state of video playback
        /// </summary>
        public HomeScreenVideoState videoState;
    }

    /// <summary>
    /// State of video playback
    /// </summary>
    [Serializable]
    public enum HomeScreenVideoState {
        PLAYING,
        PAUSED
    }
}

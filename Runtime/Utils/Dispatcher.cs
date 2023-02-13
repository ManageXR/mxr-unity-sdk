using UnityEngine;
using System.Collections.Generic;
using System;

namespace MXR.SDK {
    /// <summary>
    /// A utility class that provides some common helpers 
    /// for Unity Player/Runtime. These include:
    /// - Player Focus events and properties. OnApplicationFocus
    /// may be invoked by Unity several times in the same
    /// frame. This class provides a single reliable event for
    /// player focus change.
    /// - MainThreadExecution. Dispatch an action to execute the next
    /// frame on the main/UI thread. Use this with threading to 
    /// execute UI or gameplay code that uses the Unity APIs that are
    /// not thread-safe.
    /// </summary>
    internal class Dispatcher : MonoBehaviour {
        // ================================================
        // Instance
        // ================================================
        static Dispatcher instance = null;

        [RuntimeInitializeOnLoadMethod]
        static void Init() {
            if (instance == null && Application.isPlaying) {
                var go = new GameObject("MXR SDK Dispatcher");
                go.hideFlags = HideFlags.HideAndDontSave;
                DontDestroyOnLoad(go);
                instance = go.AddComponent<Dispatcher>();
            }
        }

        void Start() {
            StartFocus();
        }

        void Update() {
            UpdateActionQueue();
        }

        void LateUpdate() {
            LateUpdateFocus();
        }

        void OnDestroy() {
            instance = null;
        }

        // ================================================
        // FOCUS
        // ================================================
        /// <summary>
        /// Event fired when the player focus changes. The bool
        /// is true when the player is in focus and vice-versa.
        /// </summary>
        static public event Action<bool> OnPlayerFocusChange;

        /// <summary>
        /// The number of times the application has gained focus.
        /// Default: 0 
        /// </summary>
        static public int FocusCount { get; private set; }

        void StartFocus() {
            FocusCount = 1;
            OnPlayerFocusChange?.Invoke(true);
        }

        // OnApplicationFocus can get invoked multiple times
        // in the same frame. A nullable boolean helps us know
        // if the focus was gained or lost (true/false) and
        // whether it was handled or not this frame (null/notnull)
        bool? focus = null;

        void OnApplicationFocus(bool hasFocus) {
            focus = hasFocus;
        }

        private void OnApplicationPause(bool pause) {
            focus = !pause;
        }

        void LateUpdateFocus() {
            if (focus != null) {
                if (focus.Value)
                    FocusCount++;
                try {
                    OnPlayerFocusChange?.Invoke(focus.Value);
                }
                catch(Exception e) {
                    Debug.LogError("MXR Dispatcher encountered an exception invoking the focus change callback. " + e);
                }
                focus = null;
            }
        }

        // ================================================
        // Main Thread dispatch
        // ================================================
        static readonly Queue<Action> actionQueue = new Queue<Action>();

        void UpdateActionQueue() {
            lock (actionQueue) {
                while (actionQueue.Count > 0) {
                    try {
                        actionQueue.Dequeue().Invoke();
                    }
                    catch(Exception e) {
                        Debug.LogError("MXR Dispatcher encountered an exception dequeuing the action queue. " + e);
                    }
                }
            }
        }

        /// <summary>
        /// Runs an action on the main thread at the next frame/update loop
        /// </summary>
        /// <param name="action">Action that will be executed from the main thread.</param>
        public static void RunOnMainThread(Action action) {
            lock (actionQueue) {
                try {
                    actionQueue.Enqueue(action);
                }
                catch(Exception e) {
                    Debug.LogError("MXR Dispatcher encountered an exception running on main thread. " + e);
                }
            }
        }
    }
}
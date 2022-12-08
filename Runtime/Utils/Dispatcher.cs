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

        public static void Init() {
            if (instance == null && Application.isPlaying) {
                var go = new GameObject("Dispatcher");
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
        static public int FocusCount { get; private set; } = 1;

        void StartFocus() {
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

        void LateUpdateFocus() {
            if (focus != null) {
                if (focus.Value)
                    FocusCount++;
                OnPlayerFocusChange?.Invoke(focus.Value);
                focus = null;
            }
        }

        // ================================================
        // Main Thread dispatch
        // ================================================
        static readonly Queue<Action> actionQueue = new Queue<Action>();

        void UpdateActionQueue() {
            lock (actionQueue) {
                while (actionQueue.Count > 0)
                    actionQueue.Dequeue().Invoke();
            }
        }

        /// <summary>
        /// Runs an action on the main thread at the next frame/update loop
        /// </summary>
        /// <param name="action">Action that will be executed from the main thread.</param>
        public static void RunOnMainThread(Action action) {
            if (instance == null)
                Debug.LogWarning("Dispatcher not initialized, actions will not be executed until initialization. " +
                "The action has been added to the queue. To run them, call Dispatcher.Init()");

            lock (actionQueue) {
                actionQueue.Enqueue(action);
            }
        }
    }
}
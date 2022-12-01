using UnityEngine;
using System.Collections.Generic;
using System;

namespace MXR.SDK {
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
            UpdateFocus();
        }

        void OnDestroy() {
            instance = null;
        }

        // ================================================
        // FOCUS
        // ================================================
        static public event Action<bool> OnPlayerFocusChange;
        public static bool FirstFocus => FocusCount == 0;
        static public int FocusCount { get; private set; } = 0;

        void StartFocus() {
            OnPlayerFocusChange?.Invoke(true);
        }

        bool thisFrame = false;
        void OnApplicationFocus(bool hasFocus) {
            if (!thisFrame) {
                if (hasFocus) {
                    FocusCount++;
                    thisFrame = true;
                }
                OnPlayerFocusChange?.Invoke(hasFocus);
            }
        }

        void OnApplicationPause(bool pauseStatus) {
            if (!thisFrame) {
                if (!pauseStatus) {
                    FocusCount++;
                    thisFrame = true;
                }
                OnPlayerFocusChange?.Invoke(!pauseStatus);
            }
        }

        void UpdateFocus() {
            if (thisFrame)
                thisFrame = false;
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
            Init();
            lock (actionQueue) {
                actionQueue.Enqueue(action);
            }
        }
    }
}
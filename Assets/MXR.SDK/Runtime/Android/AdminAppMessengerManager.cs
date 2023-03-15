using System;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// A proxy for ManageXR Java code. Facilitates communication 
    /// between Unity and the ManageXR Android Admin App.
    /// </summary>
    public class AdminAppMessengerManager {
        /// <summary>
        /// Whether the instance is bound to the native service
        /// </summary>
        public bool IsBoundToService { get; private set; }

        /// <summary>
        /// Event fired when the binding to the native service changes
        /// </summary>
        public event Action<bool> OnBoundStatusToAdminAppChanged;

        /// <summary>
        /// Event fired when the admin app sends a message
        /// </summary>
        public event Action<int, string> OnMessageFromAdminApp;

        /// <summary>
        /// Holds an instance of native class "com.mightyimmersion.customlauncher.AdminAppMessengerManager"
        /// </summary>
        readonly AndroidJavaObject native;

        /// <summary>
        /// Creates an instance of the messenger manager
        /// </summary>
        public AdminAppMessengerManager() {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            native = new AndroidJavaObject(
                "com.mightyimmersion.customlauncher.AdminAppMessengerManager", 
                context, 
                new AdminAppMessengerListener(this)
            );
        }

        /// <summary>
        /// Invokes a method in <see cref="native"/> using method name
        /// and returns a result.
        /// </summary>
        /// <typeparam name="T">The return type of the method</typeparam>
        /// <param name="methodName">The name of the method invoked</param>
        public T Call<T>(string methodName) {
            return native.Call<T>(methodName);
        }

        /// <summary>
        /// Invokes a method in <see cref="native"/> using method name
        /// </summary>
        /// <param name="methodName">The name of the method invoked</param>
        public void Call(string methodName) {
            native.Call(methodName);
        }

        /// <summary>
        /// Invokes a method in <see cref="native"/> using name and arguments
        /// and returns a result
        /// </summary>
        /// <typeparam name="T">The return type of the method</typeparam>
        /// <param name="methodName">The name of the method invoked</param>
        /// <param name="args">The arguments passed to the method</param>
        /// <returns></returns>
        public T Call<T>(string methodName, params object[] args) {
            return native.Call<T>(methodName, args);
        }

        /// <summary>
        /// Invokes a method in <see cref="native"/> using name and arguments
        /// </summary>
        /// <param name="methodName">The name of the method invoked</param>
        /// <param name="args">The arguments passed to the method</param>
        public void Call(string methodName, params object[] args) {
            native.Call(methodName, args);
        }

        /// <summary>
        /// Sends a message to the Admin App through messenger.
        /// Same as Call<bool>("sendMessage", int)
        /// </summary>
        /// <param name="messageType">The type/ID of the message</param>
        /// <returns>Whether the message was sent. This will be false if the messenger wasn't bound to service</returns>
        public bool SendMessage(int messageType) {
            return Call<bool>("sendMessage", messageType);
        }

        /// <summary>
        /// Sends a message to the Admin App through the messenger.
        /// Same as Call<bool>("sendMessage", int, string)
        /// </summary>
        /// <param name="messageType">The type/ID of the message</param>
        /// <param name="dataJson">Payload associated with the message as a json string</param>
        /// <returns>Whether the message was sent. This will be false if the messenger wasn't bound to service</returns>
        public bool SendMessage(int messageType, string dataJson) {
            return Call<bool>("sendMessage", messageType, dataJson);
        }

        /// <summary>
        /// Class that implements the AdminAppMessengerListener native interface
        /// and used as a listener for messenger events.
        /// </summary>
        class AdminAppMessengerListener : AndroidJavaProxy {
            readonly AdminAppMessengerManager messenger;

            public AdminAppMessengerListener(AdminAppMessengerManager _messenger)
            : base("com.mightyimmersion.customlauncher.AdminAppMessengerManager$AdminAppMessengerListener") {
                messenger = _messenger;
            }

            /// <summary>
            /// Called by Java when bind status changes
            /// </summary>
            /// <param name="bound">New bound status</param>
            public void onBindStatusToAdminAppChanged(bool bound) {
                if (messenger.IsBoundToService != bound) {
                    messenger.IsBoundToService = bound;
                    messenger.OnBoundStatusToAdminAppChanged?.Invoke(bound);
                }
            }

            /// <summary>
            /// Called by Java when a message is sent
            /// </summary>
            /// <param name="what">The message type</param>
            /// <param name="json">Message data</param>
            public void onMessageFromAdminApp(int what, string json) {
                messenger.OnMessageFromAdminApp?.Invoke(what, json);
            }
        }
    }

}

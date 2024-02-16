using System;

using UnityEngine;

namespace MXR.SDK {
    /// <summary>
    /// This utility class provides extension methods for invoking JNI methods while logging any errors that occur.
    /// The occurring exception is consumed and not propagated up, this is intentional as our general usage of JNI 
    /// in Unity C# is to only check if a call succeeded or not, and not really handle exceptions.
    /// </summary>
    public static class SafeJNI {
        const string TAG = "SafeJNI";

        /// <summary>
        /// Calls a static void method on a native object using method name with optional arguments.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns>Success: true. Failure: false</returns>
        public static bool SafeCallStatic(this AndroidJavaObject obj, string methodName, params object[] args) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to call " + methodName + " on a null AndroidJavaObject");
                return false;
            }

            try {
                if (args == null || args.Length == 0)
                    obj.CallStatic(methodName);
                else
                    obj.CallStatic(methodName, args);
                return true;
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return false;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Calls a static method on a native object using method name with optional arguments, and returns the result.
        /// </summary>
        /// <typeparam name="ReturnType">The type as which the result should be returned.</typeparam>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns>Success: The JNI result. Failure: Default type value.</returns>
        public static ReturnType SafeCallStatic<ReturnType>(this AndroidJavaObject obj, string methodName, params object[] args) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to call " + methodName + " on a null AndroidJavaObject");
                return default;
            }

            try {
                if (args == null || args.Length == 0)
                    return obj.CallStatic<ReturnType>(methodName);
                else
                    return obj.CallStatic<ReturnType>(methodName, args);
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return default;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return default;
            }
        }

        /// <summary>
        /// Calls a void method on a native object using method name with optional arguments.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns>Success: true. Failure: false</returns>
        public static bool SafeCall(this AndroidJavaObject obj, string methodName, params object[] args) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to call " + methodName + " on a null AndroidJavaObject");
                return false;
            }

            try {
                if (args == null || args.Length == 0)
                    obj.Call(methodName);
                else
                    obj.Call(methodName, args);
                return true;
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return false;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Calls a method on a native object using method name with optional parameters, and returns the result.
        /// </summary>
        /// <typeparam name="ReturnType">The type as which the result should be returned.</typeparam>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns>Success: The field value. Failure: Default type value.</returns>
        public static ReturnType SafeCall<ReturnType>(this AndroidJavaObject obj, string methodName, params object[] args) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to call " + methodName + " on a null AndroidJavaObject");
                return default;
            }

            try {
                if (args == null || args.Length == 0)
                    return obj.Call<ReturnType>(methodName);
                else
                    return obj.Call<ReturnType>(methodName, args);
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return default;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return default;
            }
        }

        /// <summary>
        /// Gets a static field of a native object.
        /// </summary>
        /// <typeparam name="ReturnType">The type as which the field should be retrieved.</typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns>Success: The field value. Failure: Default type value.</returns>
        public static ReturnType SafeGetStatic<ReturnType>(this AndroidJavaObject obj, string fieldName) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to get field " + fieldName + " from a null AndroidJavaObject");
                return default;
            }

            try {
                return obj.GetStatic<ReturnType>(fieldName);
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return default;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return default;
            }
        }

        /// <summary>
        /// Gets a field of a native object.
        /// </summary>
        /// <typeparam name="ReturnType">The type as which the field should be retrieved.</typeparam>
        /// <param name="obj"></param>
        /// <param name="fieldName"></param>
        /// <returns>Success: The field value. Failure: Default type value.</returns>
        public static ReturnType SafeGet<ReturnType>(this AndroidJavaObject obj, string fieldName) {
            if (obj == null) {
                Debug.unityLogger.Log(LogType.Error, TAG, "JNI Error: Tried to get field " + fieldName + " from a null AndroidJavaObject");
                return default;
            }

            try {
                return obj.Get<ReturnType>(fieldName);
            }
            catch (AndroidJavaException e) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "JNI Exception: " + e);
                return default;
            }
            catch (Exception ex) {
                Debug.unityLogger.Log(LogType.Exception, TAG, "Unexpected Exception: " + ex);
                return default;
            }
        }
    }
}

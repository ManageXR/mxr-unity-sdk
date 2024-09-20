using System;
using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        /// <summary>
        /// JNI to get a class instance of android.os.Build
        /// </summary>
        public static AndroidJavaClass AndroidOSBuild {
            get {
                if (androidOSBuild == null)
                    androidOSBuild = new AndroidJavaClass("android.os.Build");
                return androidOSBuild;
            }
        }
        static AndroidJavaClass androidOSBuild;

        // HARDWARE STRINGS
        /// <summary>
        /// The manufacturer string of current device. Equivalent to android.os.Build.MANUFACTURER
        /// Returns "EDITOR" when running in the Unity editor
        /// </summary>
        public static string DeviceManufacturer {
            get {
                if(deviceManufacturer == null) 
                    deviceManufacturer = Application.isEditor ? "EDITOR" : AndroidOSBuild.SafeGetStatic<string>("MANUFACTURER");
                return deviceManufacturer;
            }
        }
        static string deviceManufacturer;

        /// <summary>
        /// The model string of current device. Equivalent to android.os.Build.MODEL
        /// Returns "EDITOR" when running in the Unity editor
        /// </summary>
        public static string DeviceModel {
            get {
                if(deviceModel == null)
                    deviceModel = Application.isEditor ? "EDITOR" : AndroidOSBuild.SafeGetStatic<string>("MODEL");
                return deviceModel;
            }
        }
        static string deviceModel;

        /// <summary>
        /// The ro.product.name string of current device reported by android.os.SystemProperties
        /// Equivalent to android.os.SystemProperties("ro.product.name")
        /// Returns "EDITOR" when running in the Unity editor
        /// </summary>
        public static string DeviceProductName {
            get {
                if(deviceProductName == null) 
                    deviceProductName = Application.isEditor ? "EDITOR" : GetSystemProperty("ro.product.name");
                return deviceProductName;
            }
        }
        static string deviceProductName;

        /// <summary>
        /// The product string of current device. Equivalent to android.os.Build.PRODUCT
        /// Returns "EDITOR" when running in the Unity editor
        /// </summary>
        public static string DeviceProduct {
            get {
                if(deviceProduct == null)
                    deviceProduct = Application.isEditor ? "EDITOR" : AndroidOSBuild.SafeGetStatic<string>("PRODUCT");
                return deviceProduct;
            }
        }
        static string deviceProduct;

        // MANUFACTURER DETECTION
        /// <summary>
        /// Returns true if the current device is a Pico device
        /// </summary>
        public static bool IsPicoDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("Pico", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is an Oculus device
        /// </summary>
        public static bool IsOculusDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("Oculus", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is an HTC device
        /// </summary>
        public static bool IsHTCDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("HTC", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is a Lenovo device
        /// </summary>
        public static bool IsLenovoDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("Lenovo", StringComparison.OrdinalIgnoreCase);

        // HTC DEVICE DETECTION
        /// <summary>
        /// Returns true if the current device is HTC Vive Flow 
        /// </summary>
        public static bool IsHTCViveFlow =>
            Application.isEditor ? false : DeviceModel.Equals("Vive Flow", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is HTC Vive Focus 3
        /// </summary>
        public static bool IsHTCViveFocus3 =>
            Application.isEditor ? false : DeviceModel.Equals("VIVE Focus 3", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is HTC Vive Focus Plus
        /// </summary>
        public static bool IsHTCViveFocusPlus =>
            Application.isEditor ? false : DeviceModel.Equals("Vive Focus Plus", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is HTC Vive XR Series (e.g. XR Elite)
        /// </summary>
        public static bool IsHTCViveXRSeries =>
            Application.isEditor ? false : DeviceModel.Equals("VIVE XR Series", StringComparison.OrdinalIgnoreCase);

        // PICO DEVICE DETECTION
        static readonly List<string> knownPicoG2DeviceModels = new List<string> {
            "Pico G2", "Pico G2 4K" 
        };

        /// <summary>
        /// Returns true if the current device is Pico G2
        /// </summary>
        public static bool IsPicoG2 =>
            Application.isEditor ? false : knownPicoG2DeviceModels.Contains(DeviceModel);

        /// <summary>
        /// Returns true if the current device is Pico Neo 2
        /// </summary>
        public static bool IsPicoNeo2 =>
            Application.isEditor ? false : DeviceModel.Equals("Pico Neo 2", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is Pico Neo 3
        /// </summary>
        public static bool IsPicoNeo3 =>
            Application.isEditor ? false : DeviceModel.Equals("Pico Neo 3", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is Pico G3
        /// </summary>
        public static bool IsPicoG3 =>
            Application.isEditor ? false : DeviceProduct.Equals("PICO G3", StringComparison.OrdinalIgnoreCase);

        static readonly List<string> knownPico4DeviceModels = new List<string> {
            "A8140", "A8110", "A81X0", "A8E50", "A8150", "A81E0",
            "A8120", "A8250", "A82E0", "A82X0", "A8E10", "A8E40"
        };
        /// <summary>
        /// Returns true if the current device is Pico 4
        /// </summary>
        public static bool IsPico4 {
            get {
                if (Application.isEditor) 
                    return false;
                return knownPico4DeviceModels.Contains(DeviceModel);
            }
        }
  
        /// <summary>
        /// Returns true if the current device is Pico 4 Ultra
        /// </summary>
        public static bool IsPico4Ultra {
            get {
                return Application.isEditor ? false : (DeviceProductName.Equals("sparrow", StringComparison.OrdinalIgnoreCase) || DeviceProduct.Equals("PICO 4 Enterprise Ultra", StringComparison.OrdinalIgnoreCase));
            }
        }

        // OCULUS DEVICE DETECTION
        /// <summary>
        /// Returns true if the current device is Oculus Quest 2
        /// </summary>
        public static bool IsQuest2 =>
            Application.isEditor ? false : DeviceProduct.Equals("hollywood", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is Oculus Quest Pro
        /// </summary>
        public static bool IsQuestPro =>
            Application.isEditor ? false : DeviceProduct.Equals("seacliff", StringComparison.OrdinalIgnoreCase); 


        /// <summary>
        /// Returns true if the current device is Oculus Quest 3
        /// </summary>
        public static bool IsQuest3 =>
            Application.isEditor ? false : DeviceProduct.Equals("eureka", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns true if the current device is Oculus Go
        /// </summary>
        public static bool IsOculusGo =>
            Application.isEditor ? false : DeviceModel.Equals("Pacific", StringComparison.OrdinalIgnoreCase);

        // DEVICE DEGREES OF FREEDOM DETECTION
        /// <summary>
        /// Returns true if the SDK is able to detect the headsets degrees of freedom tracking capability
        /// </summary>
        public static bool IsHeadsetDOFKnown =>
            IsHeadset3DOF || IsHeadset6DOF;

        /// <summary>
        /// Returns whether the headset has 6 degrees of freedom tracking capability
        /// </summary>
        public static bool IsHeadset6DOF =>
            // Oculus headsets
            IsQuestPro || IsQuest2 || IsQuest3 ||

            // Pico headsets
            IsPico4Ultra || IsPico4 || IsPicoNeo3 || IsPicoNeo2 ||

            // HTC Headsets
            IsHTCViveFlow || IsHTCViveFocus3 || IsHTCViveFocusPlus || IsHTCViveXRSeries;

        /// <summary>
        /// Returns whether the headset as 3 degrees of freedom traacking capability
        /// </summary>
        public static bool IsHeadset3DOF =>
            // Oculus headsets
            IsOculusGo ||

            // Pico headsets
            IsPicoG2 || IsPicoG3;

        /// <summary>
        /// Returns whether the SDK is running on a Pico device with 6DoF headset tracking
        /// </summary>
        public static bool IsPico6DOF => IsPicoDevice && IsHeadset6DOF;

        /// <summary>
        /// Returns whether the SDK is running on an Oculus device with 6DoF headset tracking
        /// </summary>
        public static bool IsOculus6DOF => IsOculusDevice && IsHeadset6DOF;

        #region OBSOLETE
        /// <summary>
        /// Returns Pico's UI version. returns "0.0.0" if current device is not a Pico device
        /// </summary>
        [Obsolete("Use MXRPicoUtils.PUIVersion instead. This property may be removed in the future")]
        public static string PicoUIVersion =>
            MXRPicoUtils.PUIVersion;

        /// <summary>
        /// Returns if current Pico UI version is 4.x.x if current device is a Pico device
        /// </summary>
        [Obsolete("Use MXRPicoUtils.IsPUI4 instead. This property may be removed in the future")]
        public static bool IsPicoUI4 =>
            MXRPicoUtils.IsPUI4;

        /// <summary>
        /// Returns whether the SDK is running on a device with 3DoF headset
        /// </summary>
        [Obsolete("Use IsHeadset3DOF instead. This proparty may be removed in the future.")]
        public static bool Is3DOF => IsHeadset3DOF;
        #endregion
    }
}

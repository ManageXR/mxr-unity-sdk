using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK {
    public static partial class MXRAndroidUtils {
        public static AndroidJavaClass AndroidOSBuild =>
            new AndroidJavaClass("android.os.Build");

        public static string DeviceManufacturer =>
            Application.isEditor ? "EDITOR" : AndroidOSBuild.GetStatic<string>("MANUFACTURER");

        public static string DeviceModel =>
            Application.isEditor ? "EDITOR" : AndroidOSBuild.GetStatic<string>("MODEL");

        public static string DeviceProduct =>
            Application.isEditor ? "EDITOR" : AndroidOSBuild.GetStatic<string>("PRODUCT");

        public static string PicoUIVersion =>
            IsPicoDevice ? AndroidOSBuild.GetStatic<string>("DISPLAY") : "0.0.0";

        public static bool IsPicoDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("Pico");

        public static bool IsOculusDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("Oculus");

        public static bool IsHTCDevice =>
            Application.isEditor ? false : DeviceManufacturer.Equals("HTC");

        public static bool IsHTCViveFocus3 =>
            Application.isEditor ? false : DeviceModel.Equals("VIVE Focus 3");

        public static bool IsHTCViveFlow =>
            Application.isEditor ? false : DeviceModel.Equals("Vive Flow");

        static readonly List<string> knownPicoG2DeviceModels = new List<string> {
            "Pico G2", "Pico G2 4K" 
        };
        public static bool IsPicoG2 =>
            Application.isEditor ? false : knownPicoG2DeviceModels.Contains(DeviceModel);

        public static bool IsPicoNeo2 =>
            Application.isEditor ? false : DeviceModel.Equals("Pico Neo 2");

        public static bool IsPicoNeo3 =>
            Application.isEditor ? false : DeviceModel.Equals("Pico Neo 3");
        public static bool IsQuest2 =>
            Application.isEditor ? false : DeviceProduct.Equals("hollywood");

        public static bool IsQuestPro =>
            Application.isEditor ? false : DeviceProduct.Equals("seacliff");            


        static readonly List<string> knownPico4DeviceModels = new List<string> {
            "A8140", "A8110", "A81X0", "A8E50", "A8150", "A81E0" 
        };
        public static bool IsPico4 {
            get {
                if (Application.isEditor) 
                    return false;
                return knownPico4DeviceModels.Contains(DeviceModel);
            }
        }

        public static bool IsOculusGo =>
            Application.isEditor ? false : DeviceModel.Equals("Pacific");

        public static bool IsPicoUI4 =>
            PicoUIVersion.StartsWith("4");

        public static bool IsPico6DOF => IsPicoNeo2 || IsPicoNeo3;

        public static bool IsOculus6DOF => IsOculusDevice && !IsOculusGo;

        public static bool Is3DOF => IsOculusGo || IsPicoG2 || IsHTCViveFlow;
    }
}

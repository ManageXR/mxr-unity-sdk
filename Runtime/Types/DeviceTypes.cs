﻿using System;

namespace MXR.SDK {
    [Serializable]
    public class DeviceData {
        public long availableStorage;
        public long totalStorage;
        public bool isQfbDevice;
        public string firmwareVersion;
        public string model;
        public string manufacturer;
    }
}
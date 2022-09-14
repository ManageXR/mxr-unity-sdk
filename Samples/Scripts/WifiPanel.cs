using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

/*
 * A panel class that updates UI when the wifi connection status
 * and the available wifi networks change.
 * 
 * Buttons in the panel allow you to toggle wifi and connect to a network. 
 */

namespace MXR.SDK.Samples { 
    public class WifiPanel : MonoBehaviour {
        public Transform wifiSsidContainer;
        public Text wifiSsidTemplate;
        public InputField ssidInput;
        public InputField passwordInput;
        public Text wifiIsEnabled;
        public Text ssid;
        public Text state;
        public Text hasInternetAccess;
        public Text requiresCaptivePortal;
        public Text signalStrength;
        public Text linkSpeed;
        public Text frequency;
        public Text frequencyString;
        public Text ipAddress;
        public Text macAddress;
        public Text gateway;
        public Text subnetMask;
        public Text dnsAddresses;
        public Text ipv6Addresses;
        public Text capabilities;
        public Text networkSecurityType;
        public Text captivePortalUrl;

        public void ToggleWifi() {
            if (MXRManager.System.WifiConnectionStatus.wifiIsEnabled)
                MXRManager.System.DisableWifi();
            else
                MXRManager.System.EnableWifi();
        }

        public void Connect() {
            MXRManager.System.ConnectToWifiNetwork(ssidInput.text, passwordInput.text);
        }

        void UpdateText(Text text, object value) {
            text.text = value.ToString();
        }

        void Awake() {
            MXRManager.Init();
        }

        void Start() {
            wifiSsidTemplate.gameObject.SetActive(false);
            MXRManager.Init();

            OnWifiConnectionStatusChange(MXRManager.System.WifiConnectionStatus);
            OnWifiNetworksChange(MXRManager.System.WifiNetworks);
            
            MXRManager.System.OnWifiConnectionStatusChange += OnWifiConnectionStatusChange;
            MXRManager.System.OnWifiNetworksChange += OnWifiNetworksChange;
        }

        List<Text> wifiSsidInstances = new List<Text>();
        private void OnWifiNetworksChange(List<ScannedWifiNetwork> obj) {
            foreach (var instance in wifiSsidInstances)
                Destroy(instance.gameObject);
            wifiSsidInstances.Clear();

            foreach(var x in obj) {
                var text = Instantiate(wifiSsidTemplate, wifiSsidContainer);
                text.gameObject.SetActive(true);
                text.text = x.ssid;
                wifiSsidInstances.Add(text);
            }
            LayoutRebuilder.MarkLayoutForRebuild(wifiSsidContainer.GetComponent<RectTransform>());
        }

        private void OnWifiConnectionStatusChange(WifiConnectionStatus obj) {
            UpdateText(wifiIsEnabled, obj.wifiIsEnabled);
            UpdateText(ssid, obj.ssid);
            UpdateText(state, obj.state);
            UpdateText(hasInternetAccess, obj.hasInternetAccess);
            UpdateText(requiresCaptivePortal, obj.requiresCaptivePortal);
            UpdateText(signalStrength, obj.signalStrength);
            UpdateText(linkSpeed, obj.linkSpeed);
            UpdateText(frequency, obj.frequency);
            UpdateText(frequencyString, obj.frequencyString);
            UpdateText(ipAddress, obj.ipAddress);
            UpdateText(macAddress, obj.macAddress);
            UpdateText(gateway, obj.gateway);
            UpdateText(subnetMask, obj.subnetMask);
            UpdateText(dnsAddresses, string.Join(", ", obj.dnsAddresses));
            UpdateText(ipv6Addresses, string.Join(", ", obj.ipv6Addresses));
            UpdateText(capabilities, obj.capabilities);
            UpdateText(networkSecurityType, obj.networkSecurityType);
            UpdateText(captivePortalUrl, obj.captivePortalUrl);
        }
    }
}
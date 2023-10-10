using System;
using System.Collections.Generic;
using System.ComponentModel;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MXR.SDK {
    /// <summary>
    /// Network types.
    /// </summary>
    [Serializable]
    public enum NetworkType {
        /// <summary>
        /// Does not require a password
        /// </summary>
        OPEN,

        /// <summary>
        /// Requires a password
        /// </summary>
        WEP,

        /// <summary>
        /// Requires a password
        /// </summary>
        WPA,

        /// <summary>
        /// Requires a password
        /// </summary>
        WPA2,

        /// <summary>
        /// Requires a password
        /// </summary>
        WPA3,

        /// <summary>
        /// Requires advanced enterprise authentication
        /// </summary>
        WPA_ENTERPRISE,

        /// <summary>
        /// Requires advanced enterprise authentication
        /// </summary>
        WPA2_ENTERPRISE,

        /// <summary>
        /// Requires advanced enterprise authentication
        /// </summary>
        WPA3_ENTERPRISE,

        /// <summary>
        /// Unknown network type.
        /// </summary>
        UNKNOWN
    }

    /// <summary>
    /// Object that represents the current status of the device.
    /// </summary>
    [Serializable]
    public class WifiConnectionStatus {
        /// <summary>
        /// State type for the status. 1-1 Mapping with Android native type
        /// </summary>
        [Serializable]
        public enum State {
            /// <summary>
            /// Ready to start data connection setup.
            /// </summary>
            IDLE,

            /// <summary>
            /// Searching for an available access point.
            /// </summary>
            SCANNING,

            /// <summary>
            /// Currently setting up data connection.
            /// </summary>
            CONNECTING,

            /// <summary>
            /// Network link established, performing authentication.
            /// </summary>
            AUTHENTICATING,

            /// <summary>
            /// Awaiting response from DHCP server in order to assign IP address information. 
            /// </summary>
            OBTAINING_IPADDR,

            /// <summary>
            /// IP traffic should be available. 
            /// </summary>
            CONNECTED,

            /// <summary>
            /// IP traffic is suspended 
            /// </summary>
            SUSPENDED,

            /// <summary>
            /// Currently tearing down data connection. 
            /// </summary>
            DISCONNECTING,

            /// <summary>
            /// IP traffic not available. 
            /// </summary>
            DISCONNECTED,

            /// <summary>
            /// Attempt to connect failed.
            /// </summary>
            FAILED,

            /// <summary>
            /// Access to this network is blocked. 
            /// </summary>
            BLOCKED,

            /// <summary>
            /// Link has poor connectivity. 
            /// </summary>
            VERIFYING_POOR_LINK,

            /// <summary>
            /// Checking if network is a captive portal 
            /// </summary>
            CAPTIVE_PORTAL_CHECK
        }

        /// <summary>
        /// Whether or not the device's wifi radio is turned on/off
        /// </summary>
        public bool wifiIsEnabled;

        /// <summary>
        /// Name of the current network that we are connected to 
        /// or are actively connecting to (or empty string/null if there is no network)
        /// </summary>
        public string ssid;

        /// <summary>
        /// Current state of the network connection
        /// </summary>
        public State state;

        /// <summary>
        /// True if the device can access the internet
        /// </summary>
        public bool hasInternetAccess;

        /// <summary>
        /// True if the device needs to authenticate via a captive portal. 
        /// If true, launch the captivePortalUrl in the native browser
        /// </summary>
        public bool requiresCaptivePortal;

        /// <summary>
        /// Value from 0 to 100
        /// </summary>
        public int signalStrength;

        /// <summary>
        /// Link speed in MBs
        /// </summary>
        public int linkSpeed;

        /// <summary>
        /// Frequency of the network connection. 
        /// Use frequenceString to visualize this
        /// </summary>
        public int frequency;

        /// <summary>
        /// Returns the frequency in a more readable manner.
        /// Eg. "2.4GHz" or "5GHz"
        /// </summary>
        public string frequencyString;

        /// <summary>
        /// The current IP Address
        /// </summary>
        public string ipAddress;

        /// <summary>
        /// MAC Address of the device
        /// </summary>
        public string macAddress;

        /// <summary>
        /// The default gateway address
        /// </summary>
        public string gateway;

        /// <summary>
        /// Subnet mask
        /// </summary>
        public string subnetMask;

        /// <summary>
        /// List of DNS Addresses 
        /// </summary>
        public List<string> dnsAddresses = new List<string>();

        /// <summary>
        /// List of IPv6 Addresses
        /// </summary>
        public List<string> ipv6Addresses = new List<string>();

        /// <summary>
        /// List of network capabilities
        /// Ref: https://developer.android.com/reference/android/net/NetworkCapabilities
        /// </summary>
        public string capabilities;

        /// <summary>
        /// Security type of the network currently connected/connecting to
        /// </summary>
        public NetworkType networkSecurityType;

        /// <summary>
        /// Authentication error whlie trying to connect to a network.
        /// If no error is encountered, this field is null.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public WifiAuthenticationError? authenticationError;

        /// <summary>
        /// The captive portal URL for connecting to the network
        /// </summary>
        public string captivePortalUrl;
    }

    /// <summary>
    /// Represents an available Wifi network 
    /// </summary>
    [Serializable]
    public class ScannedWifiNetwork {
        /// <summary>
        /// The name of the Wifi network
        /// </summary>
        public string ssid;

        /// <summary>
        /// Capabilities of the network
        /// </summary>
        public string capabilities;

        /// <summary>
        /// Signal strength of the network. 0 to 100
        /// </summary>
        public int signalStrength;

        /// <summary>
        /// Indicates if the network is saved in the system and 
        /// does not require password entry (or needs to be forgotten)
        /// </summary>
        public bool isSaved;

        /// <summary>
        /// Whether the network is managed by ManageXR
        /// </summary>
        public bool isManaged;

        /// <summary>
        /// The security type of the network.
        /// </summary>
        public NetworkType networkSecurityType;

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return ((ScannedWifiNetwork)obj).ssid.Equals(ssid);
        }

        public override int GetHashCode() {
            return ssid.GetHashCode() + capabilities.GetHashCode() + signalStrength.GetHashCode()
            + isSaved.GetHashCode() + isManaged.GetHashCode() + networkSecurityType.GetHashCode();
        }

        /// <summary>
        /// Whether the network is an enterprise network.
        /// Enterprise networks require more complex authentication methods (typically a username + password or a certificate)
        /// </summary>
        [JsonIgnore]
        public bool IsEnterpriseNetwork =>
            networkSecurityType == NetworkType.WPA_ENTERPRISE || networkSecurityType == NetworkType.WPA2_ENTERPRISE ||
            networkSecurityType == NetworkType.WPA3_ENTERPRISE;

        /// <summary>
        /// Whether the network requires a password to connect 
        /// </summary>
        [JsonIgnore]
        public bool IsOpen => networkSecurityType == NetworkType.OPEN;
    }

    public enum EapMethod
    {
        PEAP,
        TTLS,
        PWD,
    }

    public enum Phase2Method
    {
        PAP,
        MSCHAP,
        MSCHAPV2,
        GTC
    }

    [Serializable]
    public class EnterpriseWifiConnectionRequest
    {
        public string ssid;
        public string password;
        public string identity;
        [DefaultValue(false)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool hidden;

        public EapMethod eapMethod;
        public Phase2Method phase2Method;
        public NetworkType networkType;

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string anonymousIdentity = string.Empty;

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string domain = string.Empty;

        public EnterpriseWifiConnectionRequest() { }

        public EnterpriseWifiConnectionRequest(string ssid, string password, string identity, EapMethod eapMethod, Phase2Method phase2AuthenticationMethod, NetworkType networkType,bool hidden, string anonymousIdentity, string domain)
        {
            //Only throw exceptions for required fields 
            this.ssid = ssid ?? throw new ArgumentNullException(nameof(ssid));
            this.password = password ?? throw new ArgumentNullException(nameof(password));
            this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.networkType = networkType;
            this.eapMethod = eapMethod;
            this.phase2Method = phase2AuthenticationMethod;
            this.anonymousIdentity = anonymousIdentity;
            this.domain = domain;
            this.hidden = hidden;
        }
    }

    public enum WifiAuthenticationError {
        TIMEOUT,
        WRONG_PASSWORD,
        EAP_FAILURE,
        UNKNOWN_AUTH_ERROR
    }
}
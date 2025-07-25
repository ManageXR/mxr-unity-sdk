package com.mightyimmersion.customlauncher;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.net.wifi.WifiConfiguration;
import android.net.wifi.WifiManager;
import android.content.BroadcastReceiver;
import android.net.wifi.ScanResult;
import android.net.ConnectivityManager;
import android.net.NetworkInfo;
import java.util.List;

public class WiFiService extends BroadcastReceiver {

    public static interface OnWiFiFoundListener {
        public void onWiFiListUpdate(List<ScanResult> scanResults);
    }

    public static interface OnWiFiChangeListener {
        public void onWiFiChange(String ssid);
    }

    private Context context;
    private WifiManager wifiManager;
    private int i = 0;
    private OnWiFiFoundListener onWiFiFoundListener;
    private OnWiFiChangeListener onWiFiChangeListener;


    public WiFiService(Context _context, OnWiFiFoundListener _onWiFiFoundListener, OnWiFiChangeListener _onWiFiChangeListener) {
        context = _context;
        wifiManager = (WifiManager) _context.getSystemService(Context.WIFI_SERVICE);
        onWiFiFoundListener = _onWiFiFoundListener;
        onWiFiChangeListener = _onWiFiChangeListener;

        IntentFilter filter = new IntentFilter();
        filter.addAction(WifiManager.SCAN_RESULTS_AVAILABLE_ACTION);
        filter.addAction(WifiManager.NETWORK_STATE_CHANGED_ACTION);
        context.registerReceiver(this, filter);
    }

    public void startScan(){
        wifiManager.setWifiEnabled(true);
        wifiManager.startScan();
    }

    public boolean isConnectedToInternet(){
        ConnectivityManager cm = (ConnectivityManager) context.getSystemService(Context.CONNECTIVITY_SERVICE);
        if (cm == null) return false;
        NetworkInfo activeNetwork = cm.getActiveNetworkInfo();
        return activeNetwork != null && activeNetwork.isConnectedOrConnecting();
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        if(intent.getAction().equals(WifiManager.SCAN_RESULTS_AVAILABLE_ACTION) && onWiFiFoundListener != null){
            onWiFiFoundListener.onWiFiListUpdate(wifiManager.getScanResults());
        }else if(intent.getAction().equals(WifiManager.NETWORK_STATE_CHANGED_ACTION) && onWiFiChangeListener != null){
            onWiFiChangeListener.onWiFiChange(wifiManager.getConnectionInfo().getSSID());
        }
    }

    public boolean connectToWiFi(String ssid, String key, String networkType) {
        return connectToWiFi(ssid, key, networkType, false);
    }

    public boolean connectToWiFi(String ssid, String key, String networkType, boolean hidden) {
        if (wifiManager == null) return false;

        WifiConfiguration conf = new WifiConfiguration();
        conf.SSID = String.format("\"%s\"", ssid);
        conf.hiddenSSID = hidden;

        if ("OPEN".equals(networkType)) {
            conf.allowedKeyManagement.set(WifiConfiguration.KeyMgmt.NONE);
        } else if ("WEP".equals(networkType)) {
            conf.wepKeys[0] = String.format("\"%s\"", key);
            conf.wepTxKeyIndex = 0;
            conf.allowedKeyManagement.set(WifiConfiguration.KeyMgmt.NONE);
            conf.allowedGroupCiphers.set(WifiConfiguration.GroupCipher.WEP40);
        } else if ("WPA".equals(networkType)) {
            conf.preSharedKey = String.format("\"%s\"", key);
        }

        int netId = wifiManager.addNetwork(conf);
        boolean success = netId != -1;
        if (success) {
            wifiManager.disconnect();
            success = wifiManager.enableNetwork(netId, true);
            wifiManager.reconnect();        
        }

        return success;
    }
}
/*
 * Copyright 2021 Mighty Immersion, Inc. All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium is strictly prohibited.
 *
 * Proprietary and confidential.
 */

package com.mightyimmersion.customlauncher;

import android.content.ComponentName;
import android.content.Context;
import android.content.Intent;
import android.content.ServiceConnection;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.IBinder;
import android.os.Message;
import android.os.Messenger;
import android.os.RemoteException;
import android.util.Log;

import java.util.List;

public class AdminAppMessengerManager {
    public interface  AdminAppMessengerListener {
        void onBindStatusToAdminAppChanged(boolean bound);
        void onMessageFromAdminApp(int what, String json);
    }

    static final String TAG = "AdminAppMessengerManager";

    private final static String ADMIN_SERVICE_CLASS_NAME = "com.mightyimmersion.mightyplatform.AdminService";

    private final Messenger incomingMessenger = new Messenger(new IncomingMessageHandler(Looper.getMainLooper()));
    private Messenger outgoingMessenger;
    private boolean bound;
    private Context context;
    private AdminAppMessengerListener listener;

    private int checkBindingFrequency = 10_000; // 10 seconds
    private Handler checkBindingHandler = new Handler(Looper.getMainLooper());

    public AdminAppMessengerManager(Context _context, AdminAppMessengerListener _listener) {
        context = _context;
        listener = _listener;
        startBindToAdminServiceLoop();
    }

    public void startBindToAdminServiceLoop() {
        bindToAdminServiceLoop();
    }

    private void bindToAdminServiceLoop() {
        tryBindToAdminService();
        checkBindingHandler.postDelayed(this::bindToAdminServiceLoop, checkBindingFrequency);
    }

    private void tryBindToAdminService() {
        Log.v(TAG, "tryBindToAdminService. Already bound? = " + bound);
        if (bound) return;

        ComponentName adminServiceComponent = getInstalledAdminServiceComponent();
        if (adminServiceComponent != null) {
            launchAdminAppServiceIfNeeded(adminServiceComponent.getPackageName());
            Intent bindIntent = new Intent();
            bindIntent.setComponent(adminServiceComponent);
            // This will bind to the service whether or not it is running. As soon as the service is started
            // The onServiceConnected method will fire.
            context.bindService(bindIntent, mConnection, 0);
        } else {
            Log.v(TAG, "ManageXR Admin App not installed!");
        }
    }

    private void unbindFromAdminService() {
        context.unbindService(mConnection);
    }

    class IncomingMessageHandler extends Handler {
        IncomingMessageHandler(Looper looper) {
            super(looper);
        }

        @Override
        public void handleMessage(Message msg) {
            Bundle bundle = msg.getData();
            listener.onMessageFromAdminApp(msg.what, bundle.getString("json", null));
        }
    }

    private ServiceConnection mConnection = new ServiceConnection() {
        public void onServiceConnected(ComponentName className, IBinder service) {
            Log.v(TAG, "onServiceConnected");
            outgoingMessenger = new Messenger(service);
            bound = true;
            boolean registeredAsClient = registerAsClient();
            if (registeredAsClient) {
                Log.v(TAG, "Registered as client");
                listener.onBindStatusToAdminAppChanged(true);
            } else {
                Log.e(TAG, "Failed to register as client. Unbinding...");
                unbindFromAdminService();
            }
        }

        public void onServiceDisconnected(ComponentName className) {
            Log.v(TAG, "onServiceDisconnected");
            outgoingMessenger = null;
            bound = false;
            listener.onBindStatusToAdminAppChanged(false);
        }
    };

    private boolean registerAsClient() {
        return sendMessage(AdminAppMessageTypes.REGISTER_CLIENT);
    }

    public boolean getWifiNetworksAsync() {
        return sendMessage(AdminAppMessageTypes.GET_WIFI_NETWORKS);
    }

    public boolean getWifiConnectionStatusAsync() {
        return sendMessage(AdminAppMessageTypes.GET_WIFI_CONNECTION_STATUS);
    }

    public boolean getRuntimeSettingsAsync() {
        return sendMessage(AdminAppMessageTypes.GET_RUNTIME_SETTINGS);
    }

    public boolean getDeviceStatusAsync() {
        return sendMessage(AdminAppMessageTypes.GET_DEVICE_STATUS);
    }

    public boolean getDeviceDataAsync() {
        return sendMessage(AdminAppMessageTypes.GET_DEVICE_DATA);
    }

    public boolean enableKioskModeAsync() {
        return sendMessage(AdminAppMessageTypes.ENABLE_KIOSK_MODE);
    }

    public boolean disableKioskModeAsync() {
        return sendMessage(AdminAppMessageTypes.DISABLE_KIOSK_MODE);
    }

    public boolean overrideKioskAppAsync(String packageName) {
        return sendMessage(AdminAppMessageTypes.OVERRIDE_KIOSK_APP, "{\"packageName\":\""+packageName+"\"}");
    }

    public boolean exitLauncherAsync() {
        return sendMessage(AdminAppMessageTypes.EXIT_LAUNCHER);
    }

    public boolean killApp(String packageName) {
        return sendMessage(AdminAppMessageTypes.KILL_APP, "{\"packageName\":\""+packageName+"\"}");
    }

    public boolean restartApp(String packageName) {
        return sendMessage(AdminAppMessageTypes.RESTART_APP, "{\"packageName\":\""+packageName+"\"}");
    }

    public boolean shutdown() {
        return sendMessage(AdminAppMessageTypes.POWER_OFF);
    }

    public boolean reboot() {
        return sendMessage(AdminAppMessageTypes.REBOOT);
    }

    public boolean checkDbAsync() {
        return sendMessage(AdminAppMessageTypes.CHECK_DB);
    }

    public boolean enableTutorialModeAsync() {
        return sendMessage(AdminAppMessageTypes.ENABLE_TUTORIAL_MODE);
    }

    public boolean disableTutorialModeAsync() {
        return sendMessage(AdminAppMessageTypes.DISABLE_TUTORIAL_MODE);
    }

    public boolean enableWifiAsync() {
        return sendMessage(AdminAppMessageTypes.ENABLE_WIFI);
    }

    public boolean disableWifiAsync() {
        return sendMessage(AdminAppMessageTypes.DISABLE_WIFI);
    }

    public boolean connectToWifiNetworkAsync(String ssid, String password) {
        if (password == null) password = "";
        return sendMessage(AdminAppMessageTypes.CONNECT_TO_WIFI_NETWORK, "{\"ssid\": \""+ssid+"\", \"password\":\""+password+"\" }");
    }

    public boolean connectToEnterpriseWifiNetworkAsync(String requestJson) {
        return sendMessage(AdminAppMessageTypes.CONNECT_TO_WIFI_NETWORK, requestJson);
    }

    public boolean forgetWifiNetworkAsync(String ssid) {
        return sendMessage(AdminAppMessageTypes.FORGET_WIFI_NETWORK, "{\"ssid\":\""+ssid+"\"}");
    }

    public boolean sendHomeScreenState(String stateJson) {
        return sendMessage(AdminAppMessageTypes.HOME_SCREEN_STATE, stateJson);
    }
    
    public boolean requestCastingCodeAsync() {
        return sendMessage(AdminAppMessageTypes.GET_CASTING_CODE);
    }
    
    public boolean stopCastingAsync() {
        return sendMessage(AdminAppMessageTypes.STOP_CASTING);
    }
    
    public boolean uploadDeviceLogsAsync() {
        return sendMessage(AdminAppMessageTypes.UPLOAD_DEVICE_LOGS);
    }

    public boolean sendMessage(int what) {
        return sendMessage(what, null);
    }

    public boolean sendMessage(int what, String jsonString) {
        if (!bound) {
            tryBindToAdminService();
            return false;
        }

        Message msg = Message.obtain(null, what);
        msg.replyTo = incomingMessenger;

        if (jsonString != null) {
            Bundle bundle = new Bundle();
            bundle.putString("json", jsonString);
            msg.setData( bundle);
        }

        try {
            outgoingMessenger.send(msg);
        } catch (RemoteException e) {
            Log.e(TAG, e.getMessage());
            return false;
        }
        return true;
    }

    private ComponentName getInstalledAdminServiceComponent() {
        PackageManager pm = context.getPackageManager();
        List<PackageInfo> packages = pm.getInstalledPackages(0);

        for (PackageInfo packageInfo : packages) {
            if (packageInfo.packageName.startsWith("com.mightyimmersion.mightyplatform.adminapp") && !packageInfo.packageName.contains("preload") && !packageInfo.packageName.contains("test")) {
                return new ComponentName(packageInfo.packageName, ADMIN_SERVICE_CLASS_NAME);
            }
        }
        return null;
    }


    private void launchAdminAppServiceIfNeeded(String packageName) {
        Intent intent = new Intent();
        intent.setClassName(packageName, ADMIN_SERVICE_CLASS_NAME);
        if (Build.VERSION.SDK_INT >= 26) {
            context.startForegroundService(intent);
        } else {
            context.startService(intent);
        }
    }
}

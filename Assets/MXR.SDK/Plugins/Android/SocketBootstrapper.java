/*
 * Copyright 2026 Mighty Immersion, Inc. All Rights Reserved.
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
import android.os.Bundle;
import android.os.Handler;
import android.os.IBinder;
import android.os.Looper;
import android.os.Message;
import android.os.Messenger;
import android.os.ParcelFileDescriptor;
import android.os.RemoteException;
import android.util.Log;

import java.util.List;

/**
 * Binds SocketBootstrapService by class name and takes the handed-over socket fd.
 * SOCKET_IPC off makes onBind return null. The bootstrapper then reports that no socket arrived.
 */
public class SocketBootstrapper {
    public static final int BOOTSTRAP = 1;
    public static final String EXTRA_SOCKET = "socket";
    public static final String SERVICE_CLASS = "com.mightyimmersion.mightylibrary.ipc.SocketBootstrapService";
    public static final String NOT_HANDED_OVER = "the socket was not handed over";

    private static final String ADMIN_APP_PACKAGE_PREFIX = "com.mightyimmersion.mightyplatform.adminapp";
    private static final String TAG = "SocketBootstrapper";

    public interface Listener {
        void onSocketFd(int fd);
        void onSocketNotHandedOver(String reason);
    }

    private Listener listener;
    private boolean reported;

    private final Handler replyHandler = new Handler(Looper.getMainLooper()) {
        @Override
        public void handleMessage(Message msg) {
            Bundle data = msg.getData();
            if (data != null) {
                data.setClassLoader(ParcelFileDescriptor.class.getClassLoader());
            }
            ParcelFileDescriptor pfd = data == null ? null : data.getParcelable(EXTRA_SOCKET);
            if (pfd == null) {
                reportNotHandedOver("reply had no socket");
                return;
            }
            int fd = pfd.detachFd();
            if (listener != null) {
                listener.onSocketFd(fd);
            }
        }
    };

    private final Messenger replyTo = new Messenger(replyHandler);

    private final ServiceConnection connection = new ServiceConnection() {
        @Override
        public void onServiceConnected(ComponentName name, IBinder service) {
            if (service == null) {
                reportNotHandedOver("onBind returned null");
                return;
            }
            Message msg = Message.obtain(null, BOOTSTRAP);
            msg.replyTo = replyTo;
            try {
                new Messenger(service).send(msg);
            } catch (RemoteException e) {
                reportNotHandedOver("bootstrap send failed");
            }
        }

        @Override
        public void onServiceDisconnected(ComponentName name) {
        }

        @Override
        public void onBindingDied(ComponentName name) {
            reportNotHandedOver("binding died");
        }

        @Override
        public void onNullBinding(ComponentName name) {
            reportNotHandedOver("onBind returned null");
        }
    };

    public void bind(Context context, Listener listener) {
        this.listener = listener;
        String packageName = findAdminAppPackage(context);
        if (packageName == null) {
            reportNotHandedOver("Admin App package was not visible");
            return;
        }

        Intent intent = new Intent();
        intent.setComponent(new ComponentName(packageName, SERVICE_CLASS));
        boolean requested;
        try {
            requested = context.bindService(intent, connection, Context.BIND_AUTO_CREATE);
        } catch (SecurityException e) {
            reportNotHandedOver("bind was rejected");
            return;
        }
        if (!requested) {
            reportNotHandedOver("bindService returned false");
        }
    }

    private void reportNotHandedOver(String detail) {
        if (reported) {
            return;
        }
        reported = true;
        Log.w(TAG, NOT_HANDED_OVER + " (" + detail + ")");
        if (listener != null) {
            listener.onSocketNotHandedOver(NOT_HANDED_OVER);
        }
    }

    private static String findAdminAppPackage(Context context) {
        PackageManager manager = context.getPackageManager();
        List<PackageInfo> packages;
        try {
            packages = manager.getInstalledPackages(0);
        } catch (Exception e) {
            Log.w(TAG, "package scan failed", e);
            return null;
        }
        for (int i = 0; i < packages.size(); i++) {
            String name = packages.get(i).packageName;
            if (name.startsWith(ADMIN_APP_PACKAGE_PREFIX)
                    && !name.contains("preload")
                    && !name.contains("test")) {
                return name;
            }
        }
        return null;
    }
}

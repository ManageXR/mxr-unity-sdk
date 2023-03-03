/*
 * Copyright 2021 Mighty Immersion, Inc. All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium is strictly prohibited.
 *
 * Proprietary and confidential.
 */

package com.mightyimmersion.customlauncher;

// This should be in sync with MightyLibrary.ServiceMessageTypes
public class AdminAppMessageTypes {
    public static final int UNREGISTER_CLIENT = -1;
    public static final int REGISTER_CLIENT = 0;
    public static final int GET_WIFI_NETWORKS = 1;
    public static final int WIFI_NETWORKS = 1000;

    public static final int CONNECT_TO_WIFI_NETWORK = 2;

    public static final int GET_WIFI_CONNECTION_STATUS = 3;
    public static final int WIFI_CONNECTION_STATUS = 3000;

    public static final int GET_RUNTIME_SETTINGS = 4;
    public static final int RUNTIME_SETTINGS = 4000;

    public static final int GET_DEVICE_STATUS = 5;
    public static final int DEVICE_STATUS = 5000;

    public static final int ENABLE_KIOSK_MODE = 6;
    public static final int DISABLE_KIOSK_MODE = 7;
    public static final int EXIT_LAUNCHER = 8;
    public static final int CHECK_DB = 9;
    public static final int ENABLE_TUTORIAL_MODE = 10;
    public static final int DISABLE_TUTORIAL_MODE = 11;

    public static final int FORGET_WIFI_NETWORK = 12;

    public static final int ENABLE_WIFI = 13;
    public static final int DISABLE_WIFI = 14;
    public static final int HOME_SCREEN_STATE = 15;
}
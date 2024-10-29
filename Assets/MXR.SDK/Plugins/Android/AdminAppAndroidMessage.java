/*
 * Copyright 2024 Mighty Immersion, Inc. All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium is strictly prohibited.
 *
 * Proprietary and confidential.
 */

package com.mightyimmersion.customlauncher;

import android.content.Context;

import org.json.JSONObject;
import org.json.JSONException;

class AdminAppAndroidMessage {
        
    private static final Array<Int> ANDROID_MESSAGE_TYPES = {
        AdminAppMessageTypes.SET_SECURE_STRING,
        AdminAppMessageTypes.GET_SECURE_STRING,
        AdminAppMessageTypes.REMOVE_SECURE_STRING
    };
    
    public static boolean isAndroidMessage(Message msg) {
        return ANDROID_MESSAGE_TYPES.contains(msg.what);
    }

    public static void handleMessage(Context context, Message msg) {
        switch (msg.what) {
            case (AdminAppMessageTypes.SET_SECURE_STRING): {
                processSetSecureStringMessage(context, msg);
            }
            break;

            case (AdminAppMessageTypes.GET_SECURE_STRING): {
                processGetSecureStringMessage(context, msg);
            }
            break;

            case (AdminAppMessageTypes.REMOVE_SECURE_STRING): {
                processRemoveSecureStringMessage(context, msg);
            }
            break;

            default: {
                // Should never be called
            }
            break;
        }
    }

    private static String getServiceEncryptedPrefsKey(String msgKey) {
        return ADMIN_SERVICE_CLASS_NAME + "." + msgKey;
    }

    /**
     * Admin App has sent a string to be saved to Encrypted Preferences
     * @param msg json message containing key and value entries
     */
    private static void processSetSecureStringMessage(Context context, Message msg) {
        String jsonString = bundle.getString("json", null);
        try {
            JSONObject jsonObject = new JSONObject(jsonStr);
            String key = jsonObject.getString("key");
            String value = jsonObject.getString("value");
            if (key != null && !key.isEmpty() && value != null && !value.isEmpty()) {
                EncryptedPrefs.saveSecretString(context, getServiceEncryptedPrefsKey(key), value);
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    /**
     * Admin App is requesting a string from Encrypted Preferences
     * @param msg json message containing key to search for
     */
    private static void processGetSecureStringMessage(Context context, Message msg) {
        String jsonString = bundle.getString("json", null);
        try {
            JSONObject jsonObject = new JSONObject(jsonStr);
            String key = jsonObject.getString("key");
            if (key != null && !key.isEmpty()) {
                String value = EncryptedPrefs.getSecretString(context, getServiceEncryptedPrefsKey(key));
                if (value != null && !value.isEmpty()) {
                    // TODO: send value back to Admin App
                } else {
                    // TODO: report back to Admin App that there is no value saved
                }
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }

    /**
     * Admin App is requesting the removal of a string in Encrypted Preferences
     * @param msg json message containing key to search for and remove
     */
    private static void processRemoveSecureStringMessage(Context context, Message msg) {
        String jsonString = bundle.getString("json", null);
        try {
            JSONObject jsonObject = new JSONObject(jsonStr);
            String key = jsonObject.getString("key");
            if (key != null && !key.isEmpty()) {
                EncryptedPrefs.removeSecret(context, getServiceEncryptedPrefsKey(key));
            }
        } catch (JSONException e) {
            e.printStackTrace();
        }
    }
}
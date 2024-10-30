/*
 * Copyright 2024 Mighty Immersion, Inc. All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium is strictly prohibited.
 *
 * Proprietary and confidential.
 */

package com.mightyimmersion.customlauncher;

import android.content.Context;

import com.mightyimmersion.customlauncher.AdminAppMessageTypes;
import com.mightyimmersion.customlauncher.AdminAppMessengerManager;

import java.rmi.server.Operation;

import org.json.JSONException;
import org.json.JSONObject;

class AdminAppAndroidMessage {

    private static final String TAG = "AdminAppAndroidMessage";
    private static final String KEY = "key";
    private static final String REQUEST = "request";
    private static final String VALUE = "value";

    // List of all types that should be handled here
    private static final Array<Int> ANDROID_MESSAGE_TYPES = {
        AdminAppMessageTypes.SECURE_STRING_REQUEST
    };
    
    public static boolean isAndroidMessage(Message msg) {
        return ANDROID_MESSAGE_TYPES.contains(msg.what);
    }

    /**
     * Process the Android Message
     * @param context Application context
     * @param msg Message containing a json string
     */
    public static void handleMessage(Context context, Message msg) {
        switch (msg.what) {
            case (AdminAppMessageTypes.SECURE_STRING_REQUEST): {
                processSecureStringMessage(context, msg);
            }
            break;

            default: {
                // Should never be called if 'isAndroidMessage' checked first
            }
            break;
        }
    }

    private enum SecureStringRequest {
        GET,
        SET,
        EXISTS,
        DELETE;
    
        public String getValue() {
            return name().toUpperCase();
        }

        public static SecureStringRequest secureStringRequest(String str) {
            for (SecureStringRequest request : SecureStringRequest.values()) {
                if (request.name().equalsIgnoreCase(str)) {
                    return request;
                }
            }
            return null;
        }
    }

    private static String getServiceEncryptedPrefsKey(String msgKey) {
        return ADMIN_SERVICE_CLASS_NAME + "." + msgKey;
    }

    private static String geString(JSONObject jsonObject, String toFind){
        String str = null;
        try {
            str = jsonObject.getString(toFind);
        } catch (JSONException e) {
            Log.e(TAG, "Secure String Request - did not find " + toFind);
            e.printStackTrace();
            return null;
        }
        
        if (str.isEmpty()) str = null;
        if (str == null) {
            Log.e(TAG, "Secure String Request - empty string: " + toFind);
        }
        return str;
    }

    private static void processSecureStringMessage(Context context, Message msg) {

        // TODO - report back any errors

        String jsonString = bundle.getString(AdminAppMessengerManager.JSON, null);
        JSONObject jsonObject = null;
        try {
            jsonObject = new JSONObject(jsonStr);
        } catch (JSONException e) {
            Log.e(TAG, "Secure String Request - error parsing json", e);
            return;
        }

        String requestStr = getString(jsonObject, REQUEST);
        if (requestStr == null) return;

        Log.v(TAG, "Secure String Request: " + requestStr);
        SecureStringRequest request = SecureStringRequest.secureStringRequest(requestStr);
        if (request == null) return;

        String key = getString(jsonObject, KEY);
        if (key == null ) return;
        String prefsKey = getServiceEncryptedPrefsKey(key);

        switch (request) {
            /**
            * Admin App is requesting a string from Encrypted Preferences
            */
            case GET: {
                String value = EncryptedPrefs.getSecretString(context, prefsKey);
                if (value != null && !value.isEmpty()) {
                    // TODO: send value back to Admin App
                } else {
                    // TODO: error - report back to Admin App that there is no value saved
                }
            }
            break;

            /**
            * Admin App has sent a string to be saved to Encrypted Preferences
            */
            case SET: {
                String value = getString(jsonObject, VALUE);
                if (value == null) return;
                EncryptedPrefs.saveSecretString(context, prefsKey, value);
            }
            break;

            /**
            * Admin App is requesting if a string is present in Encrypted Preferences
            */
            case EXISTS: {
                boolean isFound = EncryptedPrefs.hasNonEmptySecretString(context, prefsKey);
                // TODO: send value back to Admin App
            }
            break;

            /**
            * Admin App is requesting the removal of a string in Encrypted Preferences
            */
            case DELETE: {
                EncryptedPrefs.removeSecret(context, prefsKey);
            }
            break;
        }
    }
}
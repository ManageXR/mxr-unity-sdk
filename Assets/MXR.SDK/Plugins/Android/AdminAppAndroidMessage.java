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

import java.rmi.RemoteException;
import java.rmi.server.Operation;

import org.json.JSONException;
import org.json.JSONObject;

class AdminAppAndroidMessage {

    private static final String TAG = "AdminAppAndroidMessage";
    private static final String KEY = "key";
    private static final String REQUEST = "request";
    private static final String VALUE = "value";
    private static final String RESPONSE = "response";
    private static final String RESULT = "result";
    private static final String ERROR = "error";

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

    private static void replyWithJson(int what, String json, Message originalMsg) {

        Messenger replyToApp = originalMsg.replyTo;    
        if (replyToApp == null) {
            Log.w(TAG, "No app to reply to");
            return;
        }
        try {
            Message response = Message.obtain(null, what);
            Bundle jsonBundle = new Bundle();
            jsonBundle.putString(JSON, json);
            response.setData(jsonBundle);
            replyToApp.send(response);
        } catch (RemoteException e) {
            Log.e(TAG, "Error responding to app", e);
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
        
        public static String errorResponsePayload(SecureStringRequest request, String error) {
            return responsePayload(request, error, true);
        }

        public static String responsePayload(SecureStringRequest request, String response, Boolean isError) {
            JSONObject jsonObject = new JSONObject();
            try {
                if (request != null) {
                    jsonObject.put(RESPONSE, request.getValue());
                }
                String safeResponse = org.json.JSONObject.quote(response);
                jsonObject.put(isError ? ERROR : RESULT, safeResponse);
                return jsonObject.toString();
            } catch (JSONException e) {
                Log.e(TAG, "Secure String Request error in forming response", e);
                StringBuilder sb = new StringBuilder("{");
                if (request != null) {
                    sb.append("\"").append(RESPONSE).append("\": \"");
                    sb.append(request.getValue()).append("\",");
                }
                sb.append("\"").append(ERROR).append("\": \"Unknown error\"}");
                return sb.toString();
            }
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
            return null;
        }
        
        if (str.isEmpty()) str = null;
        if (str == null) {
            Log.e(TAG, "Secure String Request - empty string: " + toFind);
        }
        return str;
    }

    private static void processSecureStringMessage(Context context, Message msg) {

        String jsonString = bundle.getString(AdminAppMessengerManager.JSON, null);
        JSONObject jsonObject = null;
        try {
            jsonObject = new JSONObject(jsonStr);
        } catch (JSONException e) {
            Log.e(TAG, "Secure String Request - error parsing json", e);
            replyWithJson(msg.what, errorResponsePayload(null, "Error parsing json"), msg);
            return;
        }

        String requestStr = getString(jsonObject, REQUEST);
        if (requestStr == null) {
            replyWithJson(msg.what, errorResponsePayload(null, "Error getting request string"), msg);
            return;
        }

        Log.v(TAG, "Secure String Request: " + requestStr);
        SecureStringRequest request = SecureStringRequest.secureStringRequest(requestStr);
        if (request == null) {
            replyWithJson(msg.what, errorResponsePayload(null, "Error identifying request"), msg);
            return;
        }

        String key = getString(jsonObject, KEY);
        if (key == null ) {
            replyWithJson(msg.what, errorResponsePayload(request, "Error getting key"), msg);
            return;
        }
        String prefsKey = getServiceEncryptedPrefsKey(key);

        switch (request) {
            /**
            * Admin App is requesting a string from Encrypted Preferences
            */
            case GET: {
                String value = EncryptedPrefs.getSecretString(context, prefsKey);
                if (value != null && !value.isEmpty()) {
                    // Send result to Admin App
                    replyWithJson(msg.what, responsePayload(request, value, false), msg);
                } else {
                    // Error - report back to Admin App that there is no value saved
                    replyWithJson(msg.what, errorResponsePayload(request, "Error getting string"), msg);
                }
            }
            break;

            /**
            * Admin App has sent a string to be saved to Encrypted Preferences
            */
            case SET: {
                String value = getString(jsonObject, VALUE);
                if (value == null) {
                    replyWithJson(msg.what, errorResponsePayload(request, "Error identifying string to save"), msg);
                    return;
                }
                boolean success = EncryptedPrefs.saveSecretString(context, prefsKey, value);
                if (!success) {
                    replyWithJson(msg.what, errorResponsePayload(request, "Error saving string"), msg);
                }
            }
            break;

            /**
            * Admin App is requesting if a string is present in Encrypted Preferences
            */
            case EXISTS: {
                boolean isFound = EncryptedPrefs.hasNonEmptySecretString(context, prefsKey);
                // Send value back to Admin App
                replyWithJson(msg.what, responsePayload(request, Boolean.toString(isFound), false), msg);
            }
            break;

            /**
            * Admin App is requesting the removal of a string in Encrypted Preferences
            */
            case DELETE: {
                boolean success = EncryptedPrefs.removeSecret(context, prefsKey);
                if (!success) {
                    replyWithJson(msg.what, errorResponsePayload(request, "Error deleting string"), msg);
                }
            }
            break;
        }
    }
}
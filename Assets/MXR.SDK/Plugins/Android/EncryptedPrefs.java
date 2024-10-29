/*
 * Copyright 2020 Mighty Immersion, Inc. All Rights Reserved.
 *
 * Unauthorized copying of this file, via any medium is strictly prohibited.
 *
 * Proprietary and confidential.
 */

 package com.mightyimmersion.customlauncher;

 import android.content.Context;
 import android.content.SharedPreferences;
 
 import androidx.security.crypto.EncryptedSharedPreferences;
 import androidx.security.crypto.MasterKeys;
 
 import java.util.concurrent.atomic.AtomicBoolean;
 
 public class EncryptedPrefs {
     private static final AtomicBoolean isFirstGetPrefsCall
             = new AtomicBoolean(true);
 
     private static SharedPreferences getPrefs(Context context) {
         boolean wasFirstCall = isFirstGetPrefsCall.getAndSet(false);
         try {
             String masterKeyAlias =
                     MasterKeys.getOrCreate(MasterKeys.AES256_GCM_SPEC);
             return EncryptedSharedPreferences.create(
                     Consts.getAppEncryptedSharedPreferences(context),
                     masterKeyAlias,
                     context,
                     EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                     EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM);
         } catch (Exception e) {
             Log.e("EncryptedPrefs: error on first getPrefs call", e);
             return null;
         }
     }
 
     private static SharedPreferences.Editor getPrefsEditor(Context context) {
         SharedPreferences prefs = getPrefs(context);
         if (prefs != null) return prefs.edit();
         return null;
     }
 
     /**
      * Gets a secret string from the encrypted shared preferences.
      *
      * @param context    application context
      * @param secretName name of the secret to get
      * @return secret string if it exists; else null.
      */
     public static String getSecretString(Context context, String secretName) {
         SharedPreferences prefs = getPrefs(context);
         if (prefs == null) return null;
         return prefs.getString(secretName, null);
     }
 
     /**
      * Checks if a secret string exists in the encrypted shared preferences and is non-empty.
      *
      * @param context    application context
      * @param secretName name of the secret to check
      * @return true if the secret exists and is a non-empty string; else false.
      */
     public static boolean hasNonEmptySecretString(Context context, String secretName) {
         if (!hasSecret(context, secretName)) return false;
         String str = getSecretString(context, secretName);
         return str != null && !str.isEmpty();
     }
 
     /**
      * Saves a secret string to the encrypted shared preferences.
      *
      * @param context    application context
      * @param secretName name of the secret to save
      * @param secret     secret to save
      * @return true if no error was encountered; else false. Does not indicate if the secret was
      * actually saved, or if an existing secret was overwritten.
      */
     public static boolean saveSecretString(Context context, String secretName, String secret) {
         SharedPreferences.Editor editor = getPrefsEditor(context);
         if (editor == null) return false;
         editor.putString(secretName, secret);
         editor.commit();
         return true;
     }
 
     /**
      * Removes a secret from the encrypted shared preferences.
      *
      * @param context    application context
      * @param secretName name of the secret to remove
      * @return true if no error was encountered; else false. Does not indicate if there was a
      * secret
      * to actually remove.
      */
     public static boolean removeSecret(Context context, String secretName) {
         SharedPreferences.Editor editor = getPrefsEditor(context);
         if (editor == null) return false;
         editor.remove(secretName);
         editor.commit();
         return true;
     }
 
     /**
      * Checks if a secret exists in the encrypted shared preferences. NOTE: The secret can be of
      * any
      * type, including null or empty string.
      *
      * @param context    application context
      * @param secretName name of the secret to check
      * @return true if the secret exists; else false.
      */
     static boolean hasSecret(Context context, String secretName) {
         SharedPreferences prefs = getPrefs(context);
         if (prefs == null) return false;
         return prefs.contains(secretName);
     }
 }
 
package com.mightyimmersion.customlauncher;

import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.util.Log;
import android.os.Bundle;
import android.os.Build;
import android.app.ActivityManager;
import android.graphics.Bitmap;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.Canvas;
import java.util.List;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import java.util.*;
import java.io.ByteArrayOutputStream;
import android.util.TimingLogger;
import android.util.Log;
import android.net.Uri;

public class NativeUtils {

    Context mContext;
    ActivityManager mActivityManager;

    public NativeUtils(Context context) {
        mContext = context;
        mActivityManager = (ActivityManager) context.getSystemService(Context.ACTIVITY_SERVICE);
    }

    public boolean launchIntentAction(String intentAction) {
        try {
            Intent intent = new Intent(intentAction);
            if (intent == null) return false;
            mContext.startActivity(intent);
            return true;
        } catch (Exception e){
            return false;
        }
    }

    public boolean launchApp(String packageName) {
        try {
            Intent intent = mContext.getPackageManager().getLaunchIntentForPackage(packageName);
            if (intent == null) return false;
            mContext.startActivity(intent);
            return true;
        } catch (Exception e){
            return false;
        }
    }

    public boolean launchAppWithClass(String packageName, String className) {
        try {
            if (!isAppInstalled(packageName)) return false;

            Intent i = new Intent();
            i.setClassName(packageName, className);
            mContext.startActivity(i);
            return true;
        } catch (Exception e){
            return false;
        }
    }
    
    public void openUrl(String url) {
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setData(Uri.parse(url));
        mContext.startActivity(intent);
    }

    public boolean launchOculusSystemUx(String dataUri) {
        try {
            Intent i = new Intent(Intent.ACTION_VIEW);
            i.setClassName("com.oculus.vrshell", "com.oculus.vrshell.MainActivity");
            i.setData(Uri.parse(dataUri));
            i.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            mContext.startActivity(i);
            return true;
        } catch (Exception e){
            return false;
        }
    }

    public boolean isAppInstalled(String packageName) {
        return getInstalledPackagedVersionCode(packageName) != -1;
    }

    public long getInstalledPackagedVersionCode(String packageName) {
        try {
            PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(packageName, 0);
            // if (Build.VERSION.SDK_INT >= 28) {
            //     return pInfo.getLongVersionCode();
            // }
            return pInfo.versionCode;
        } catch (Exception e){
            return -1;
        }
    }

    public String getInstalledPackagedVersionName(String packageName) {
        try {
            PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(packageName, 0);
            return pInfo.versionName;
        } catch (Exception e){
            return null;
        }
    }

    // Note: This function only works on certain device / firmware combinations.
    // Instead, rely on the AdminAppMessengerManager to send KillApp messages to the
    // admin app.
    public void killApp(String packageName) {
        try {
            Log.v("NativeUtils", "Killing " + packageName);
            mActivityManager.killBackgroundProcesses(packageName);
        } catch (Exception e) {
            Log.e("NativeUtils", e.toString());
        }
    }

    public class PInfo
    {
        private String appName = "";
        private String packageName = "";
    }

    public ArrayList<PInfo> getPackages()
    {
        ArrayList<PInfo> apps = getInstalledApps();
        return apps;
    }

    public byte[] getIcon(String packageName){
        try{
            PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(packageName, 0);
            Drawable drawable = pInfo.applicationInfo.loadIcon(mContext.getPackageManager());
            return drawableToByte(drawable);
        }catch (PackageManager.NameNotFoundException e){
            Log.e("NativeUtils", e.toString());
            return null;
        }
    }


    private Bitmap getBitmapFromDrawable(Drawable drawable) {
        Bitmap bmp = Bitmap.createBitmap(drawable.getIntrinsicWidth(), drawable.getIntrinsicHeight(), Bitmap.Config.ARGB_8888);
        Canvas canvas = new Canvas(bmp);
        drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
        drawable.draw(canvas);
        return bmp;
    }

    private byte[] drawableToByte(Drawable d)
    {
        Bitmap bitmap = getBitmapFromDrawable(d);
        ByteArrayOutputStream stream = new ByteArrayOutputStream();
        bitmap.compress(Bitmap.CompressFormat.PNG, 100, stream);
        return stream.toByteArray();
    }

    private ArrayList<PInfo> getInstalledApps() {
        ArrayList<PInfo> res = new ArrayList<PInfo>();
        List<PackageInfo> packs = mContext.getPackageManager().getInstalledPackages(0);
        for(int i=0;i<packs.size();i++) {
            PackageInfo p = packs.get(i);
            if (p.versionName == null) {
                continue ;
            }
            PInfo newInfo = new PInfo();
            newInfo.appName = p.applicationInfo.loadLabel(mContext.getPackageManager()).toString();
            newInfo.packageName = p.packageName;
            res.add(newInfo);
        }
        return res;
    }

    public void sendBroadcastAction(String action) {
        try {
            Intent i = new Intent();
            i.setAction(action);
            i.addFlags(Intent.FLAG_ACTIVITY_NO_ANIMATION|Intent.FLAG_FROM_BACKGROUND|Intent.FLAG_INCLUDE_STOPPED_PACKAGES);        
            mContext.sendBroadcast(i);
        } catch (Exception e) {}
    }

    public String getInstalledAdminAppPackageName() {
        PackageManager pm = mContext.getPackageManager();
        List<PackageInfo> packages = pm.getInstalledPackages(0);

        for (PackageInfo packageInfo : packages) {
            if (packageInfo.packageName.startsWith("com.mightyimmersion.mightyplatform.adminapp") && !packageInfo.packageName.contains("preload")) {
                return packageInfo.packageName;
            }
        }
        return null;
    }

}

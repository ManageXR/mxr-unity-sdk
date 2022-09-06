package com.mightyimmersion.nativesamplecode;

import android.content.Context;
import android.content.Intent;
import android.net.Uri;

public class MightyMDMNativeSampleCode {

    Context context;

    public MightyMDMNativeSampleCode(Context _context) {
        context = _context;
    }

    public void openUrlWithFirefox(String url) {
        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setData(Uri.parse(url));
        intent.setClassName("org.mozilla.vrbrowser", "org.mozilla.vrbrowser.VRBrowserActivity");
        context.startActivity(intent);
    }

    public void launchAppWithClass(String packageName, String className) {
        Intent intent = new Intent();
        intent.setClassName(packageName, className);
        context.startActivity(intent);
    }

    public boolean launchApp(String packageName) {
        Intent intent = context.getPackageManager().getLaunchIntentForPackage(packageName);
        if (intent == null) return false;
        
        context.startActivity(intent);
        return true;   
    }

    public boolean launchAppWithAction(String intentAction) {
        Intent intent = new Intent(intentAction);
        context.startActivity(intent);
        return true;
    }
}

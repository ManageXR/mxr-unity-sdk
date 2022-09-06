package com.mightyimmersion.customlauncher;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.BroadcastReceiver;

public class PackageChangeReceiver extends BroadcastReceiver {

    public interface OnPackageChangeListener {
        void onPackageChange();
    }

    private Context context;
    private OnPackageChangeListener onPackageChangeListener;

    public PackageChangeReceiver(Context _context, OnPackageChangeListener _onPackageChangeListener) {
        context = _context;
        onPackageChangeListener = _onPackageChangeListener;
        IntentFilter filter = new IntentFilter();
        filter.addDataScheme("package");
        filter.addAction(Intent.ACTION_PACKAGE_ADDED);
        filter.addAction(Intent.ACTION_PACKAGE_CHANGED);
        filter.addAction(Intent.ACTION_PACKAGE_REMOVED);
        filter.addAction(Intent.ACTION_PACKAGE_REPLACED);

        context.registerReceiver(this, filter);
    }

    public void onDestroy(){
        context.unregisterReceiver(this);
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        onPackageChangeListener.onPackageChange();
    }
}

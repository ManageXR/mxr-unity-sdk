package com.mightyimmersion.customlauncher;

import android.content.Context;
import android.os.BatteryManager;

import static android.content.Context.BATTERY_SERVICE;

public class BatteryService {
    private Context context;

    public BatteryService(Context _context) {
        context = _context;
    }

    public int getBatteryLevel() {
        BatteryManager bm = (BatteryManager)context.getSystemService(BATTERY_SERVICE);
        if (bm == null) {
            return -1;
        }
        return bm.getIntProperty(BatteryManager.BATTERY_PROPERTY_CAPACITY);
    }
}

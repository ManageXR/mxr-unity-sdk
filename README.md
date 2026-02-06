> ⚠️ This SDK is in beta testing. We appreciate your patience while we continue to improve this SDK. Please report any issues via GitHub.

# ManageXR Unity SDK

[![openupm](https://img.shields.io/npm/v/com.mxr.unity.sdk?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mxr.unity.sdk/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.mxr.unity.sdk)](https://openupm.com/packages/com.mxr.unity.sdk/)

The ManageXR Unity SDK enables developers to query device status information from the ManageXR Admin App running on your device, including:
- Device Serial Number
- Device Name, Configuration, Tags, etc.
- VR Content library details
- VR Content update download/install status


This SDK drives the ManageXR Home Screen, meaning you can use this to make your own custom home screen from the ground up!

Example usages of this SDK:
- Create your own Home Screen
- Update users in-headset when an update for you app is downloading/available
- Use device serial / details to augment your app's analytics reporting

## Installation  
> ⚠️  
> We recommend installing the latest version of the SDK.  
> Version name are in the format `vx.x.x`, for example `v1.0.10`  
> [Version history can be found here](https://github.com/ManageXR/mxr-unity-sdk/releases)  

Go to your project manifest json file located at `Packages/manifest.json`.
Add this line to your dependency list
* `"com.mxr.unity.sdk":"https://www.github.com/ManageXR/mxr-unity-sdk.git#VERSION"` 
    * `VERSION` is the SDK version you want to install
    * For example, if you want to install version `v1.0.10`, the line should be `"com.mxr.unity.sdk":"https://www.github.com/ManageXR/mxr-unity-sdk.git#v1.0.10"`

## Changing the SDK version
If you want to move to a different SDK version (for example, when a new version is released and you want to upgrade), open `Packages/manifest.json` and change the version in the `"com.mxr.unity.sdk"` dependency line.

## Samples  
The repository includes samples that demonstrate basic integration with the Content API, Status API, Wifi API and SDK Commands.

To import them 
* Go to Package Manager 
* Select the ManageXR Unity SDK package
* Select the Samples tab and click on the Import button

## Usage  
The SDK uses `IMXRSystem` to communicate with the system layer. It provides methods, events, properties to observe, query and invoke operations in the ManageXR Admin/System.

Initialization of the SDK is asynchronous. Use `await MXRManager.InitAsync();`. You may then access the `MXRSystem` using `MXRManager.System`.
Sample code for initializing the SDK:
```
async void InitMXRSDK() {
    await MXRManager.InitAsync();
    // Use MXRManager.System the way you want
}
```

The `IMXRSystem` relays information to your app through a few different value classes:

- `IMXRSystem.RuntimeSettingsSummary` contains all information about the device and its current configuration. This will include information about all of the apps, files, and settings that are deployed to this device.
- You may subscribe to realtime changes of this data with `MXRManager.System.OnRuntimeSettingsSummaryChange += OnRuntimeSettingsSummaryChange;`
- `IMXRSystem.DeviceStatus` contains all information about the device's current status. This includes the device's serial number and the status of apps/files that are currently being downloaded (including download progress).
- You may subscribe to realtime changes of this data with `MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChange;`
- See `Assets/MXR.SDK/Runtime/Types/RuntimeTypes.cs` for full code documentation of this type and the data included in it.
- See `Assets/MXR.SDK/Runtime/Types/StatusTypes.cs` for full code documentation of this type and the data included in it.
- See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/runtimeSettingsSummary.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor)
- See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/deviceStatus.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor.)
- `MXRManager` also internally handles `System.OnHomeScreenStateRequest` events by sending the last reported `HomeScreenState`. You can modify the `HomeScreenState` using `SetHomeScreenState` and `ModifyHomeScreenState` methods in `MXRManager`. 

See `LibraryPanel.cs` and `WifiPanel.cs` under `Assets/MXR.SDK/Samples/Scripts/` for an example of how to initialize the MXRSystem and subscribe to events.

IntelliSense code comments have been added for the key APIs and models. API reference and further documentation coming soon.

Logging is enabled by default. Use `MXRManager.System.EnableLogging = false` after initialization to disable messages from being logged to the console.  

## Building your Unity project
Make sure that the VR device has been set up using the ManageXR Device Setup Tool ([docs here](https://help.managexr.com/en/articles/5296578-register-a-new-device)) which installs the Android applications on your device that the SDK requires.
  
Add the `READ_EXTERNAL_STORAGE` permission to your Android Manifest.  
`<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"/>`

For working with the Wifi API, add the following permissions:  
`<uses-permission android:name="android.permission.INTERNET" />`  
`<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />`  
`<uses-permission android:name="android.permission.ACCESS_WIFI_STATE" />`  
`<uses-permission android:name="android.permission.CHANGE_WIFI_STATE" />`  

### Additional steps for Target API Level 29 (Android 10)

Add `requestLegacyExternalStorage="true"` to the `application` tag in your Android Manifest.  
`<application android:requestLegacyExternalStorage="true">`

### Additional steps for Target API Level 30 and above (Android 11 and above)
Android 11 introduces two new permissions that are required for the MXR SDK to function.
  
__1.__ [__Package Visibilty__](https://developer.android.com/training/package-visibility)  
This is for querying information about other apps installed on the device. Since the MXR SDK heavily relies on the ManageXR Admin App (installed by the Device Setup Tool), granting this permission is critical. To do this, add the `QUERY_ALL_PACKAGES` to your manifest:  
`<uses-permission android:name="android.permission.QUERY_ALL_PACKAGES" />`

__2.__ [__External Storage Manager__](https://developer.android.com/reference/android/Manifest.permission#MANAGE_EXTERNAL_STORAGE)  
This allows the SDK to access external files on the device. The SDK needs to access external files created by the ManageXR Admin App to function properly. 
The ManageXR Admin App stores files in the `MightyImmersion` directory in the SD card root that requires this permission. 

To do this:
- Add the `MANAGE_EXTERNAL_STORAGE` permission to your Android Manifest:  
`<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE" tools:ignore="ScopedStorage"/>`  
- You may see an error saying "Namespace prefix 'tools' is not defined" or "XmlException: 'tools' is an undeclared prefix". To fix this, add the following to the `manifest` tag in your manifest:  
`xmlns:tools="http://schemas.android.com/tools"`
- However, declaring the `MANAGE_EXTERNAL_STORAGE` permission in manifest isn't enough. The user must use an Android system dialog to grant this permission. MXR SDK provides a utility method to do this:  
`MXRAndroidUtils.RequestManageAppAllFilesAccessPermission();`  
- If you want to check if the user has already granted the `MANAGE_EXTERNAL_STORAGE` permission, the MXR SDK provides a static property to query this:  
`MXRAndroidUtils.IsExternalStorageManager`  
- If you want to know if you need the `MANAGE_EXTERNAL_STORAGE` permission, the SDK provides a static property to query it:
`MXRAndroidUtils.NeedsManageExternalStoragePermission`

Here's some sample code for external storage permission:
```
void TryRequestManageExternalStoragePermission() {
    // If we don't need the permission, don't do anything.
    if (!MXRAndroidUtils.NeedsManageExternalStoragePermission) 
        return;

    // If we do need the permission, but it has already been granted, don't do anything.
    if (MXRAndroidUtils.IsExternalStorageManager) 
        return;

    // If we need the permission and it's not been granted, we need to request it
    MXRAndroidUtils.RequestManageAppAllFilesAccessPermission();
}
```

Try to invoke `MXRAndroidUtils.RequestManageAppAllFilesAccessPermission()` as early as possible on startup and before initializing the ManageXR Unity SDK. This way your app and the MXR SDK will be able to access the ManageXR files stored on disk.  
* Invoking `MXRAndroidUtils.RequestManageAppAllFilesAccessPermission()` opens a system dialog with a toggle button that the user must enable in order to grant the permission. BUT if the `MANAGE_EXTERNAL_STORAGE` permission is not included in the AndroidManifest, that toggle button will be in disabled state.
* Use the `MXRAndroidUtils.RequestManageAppAllFilesAccessPermission()` method provided in the SDK instead of [Unity Permissions API](https://docs.unity3d.com/ScriptReference/Android.Permission.html) as it doesn't work with this permission.

## Support  
Please open a Github Issue or contact support@managexr.com for additional support.

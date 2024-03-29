> ⚠️ This SDK is in beta testing. We appreciate your patience while we continue to improve this SDK. Please report any issues via GitHub.

# ManageXR Unity SDK
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

___
## Installation & Setup  
Install the SDK package in your Unity project. There are multiple methods:

### _1. Via Package Manager window_  
The UPM package is [available on OpenUPM](https://openupm.com/packages/com.mxr.unity.sdk). To install, follow these instructions:  
- Add the OpenUPM registry with the ManageXR Unity SDK as a `scope`. To do this, go to `Edit/Project Settings/Package Manager`, add the OpenUPM scope registry with the URL `https://package.openupm.com` and add `com.mxr.unity.sdk` as a scope.

- Install the package from OpenUPM registry. To do this, go to `Window/Package Manager/`. Select in the `My Registries` view (located at the top left, the default selection is `Unity Registry`), locate `ManageXR Unity SDK` and click install. After installation, the package will show up in the `In Project` view as well.

### _2. Via Project Manifest_
Merge the following snippet in the project manifest json file located at `Packages/manifest.json` in your Unity project.  
```  

{
   "scopedRegistries": [
      {
            "name": "package.openupm.com",
            "url": "https://package.openupm.com",
            "scopes": [
               "com.mxr.unity.sdk"
            ]
      }
   ],
   "dependencies": {
      "com.mxr.unity.sdk": "1.0.0"
   }
}  

```

*Note:* Change `1.0.0` to the SDK version you wish to use. Latest recommended.
___
## Samples  
The repository includes samples that demonstrate basic integration with the Content API, Status API, Wifi API and SDK Commands.

### Installation
To install, select the ManageXR Unity SDK in the Package Manager window after you've successfully imported it click on `Import Into Project`. The samples will be imported under `Assets/Samples` in your Unity project.

### Files  
Sample Files allow you to simulate SDK operations in the editor.  
- After installation, go to `Library/PackageCache/com.mxr.unity.sdk@x.x.x/` directory in your Unity project and extract `Files.zip` to the root of your Unity project. Your project structure should then look like this:  

```

<Unity Project Directory>  
└── Assets
└── Files  
      └── MightyImmersion  
└── Library  
└── ...

```

It is recommended that you extract Files.zip in required location this every time you upgrade or downgrade the package version so the files inside remain relevant to the SDK version.  

Please refer to `README.txt` inside the samples directory for further information about running samples in the Unity editor.

___
## Usage  
The SDK uses `IMXRSystem` to communicate with the system layer. It provides methods, events, properties to observe, query and invoke operations in the ManageXR Admin/System.

To Initialize the `MXRSystem`, call `MXRManager.Init();`. You may then access the `MXRSystem` with `MXRManager.System`.

The `MXRSystem` relays information to your app through a few different value classes:

- `MXRSystem.RuntimeSettingsSummary` contains all information about the device and its current configuration. This will include information about all of the apps, files, and settings that are deployed to this device.
- `MXRManager` also internally handles `System.OnHomeScreenStateRequest` events by sending the last reported `HomeScreenState`. You can modify the `HomeScreenState` using `SetHomeScreenState` and `ModifyHomeScreenState` methods in `MXRManager`. 
- See `Assets/MXR.SDK/Runtime/Types/RuntimeTypes.cs` for full code documentation of this type and the data included in it.
- See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/runtimeSettingsSummary.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor)
- You may subscribe to realtime changes of this data with `MXRManager.System.OnRuntimeSettingsSummaryChange += OnRuntimeSettingsSummaryChange;`
- `MXRSystem.DeviceStatus` contains all information about the device's current status. This includes the device's serial number and the status of apps/files that are currently being downloaded (including download progress).
- See `Assets/MXR.SDK/Runtime/Types/StatusTypes.cs` for full code documentation of this type and the data included in it.
- See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/deviceStatus.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor.)
- You may subscribe to realtime  changes of this data with `MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChange;`

See `LibraryPanel.cs` and `WifiPanel.cs` under `Assets/MXR.SDK/Samples/Scripts/` as an example of how to initialize the MXRSystem and subscribe to events.

IntelliSense code comments have been added for the key APIs and models. API reference and further documentation coming soon.

___
## Building your Unity project
Make sure that the VR device has been set up using the ManageXR Device Setup Tool ([docs here](https://help.managexr.com/en/articles/5296578-register-a-new-device)) which installs the Android applications on your device that the SDK requires.
  
Add the `READ_EXTERNAL_STORAGE` permission to your Android Manifest.  
`<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"/>`

### For Target API Level 29 (Android 10)

Set `requestLegacyExternalStorage` to `true` in your Android Manifest.  
`<application android:requestLegacyExternalStorage="true">`

### For Target API Level 30 and above (Android 11 and above)
*Note:* The following is applicable only when targetting API Level 30 and above. You can get the device Android SDK INT using:  
`MXRAndroidUtils.AndroidSDKInt`

Android 11 introduces two new permissions that are required for the MXR SDK to function:
- [Package Visibilty](https://developer.android.com/training/package-visibility) for querying information about other apps installed on the device. Since the MXR SDK uses the ManageXR Admin App (installed by the Device Setup Tool), granting this permission is critical. To do this, add the `QUERY_ALL_PACKAGES` to your manifest:  
`<uses-permission android:name="android.permission.QUERY_ALL_PACKAGES" />`

- [External Storage Manager](https://developer.android.com/reference/android/Manifest.permission#MANAGE_EXTERNAL_STORAGE) that requires user permission for an app to be able to manage files on the device.  
The ManageXR SDK stores files in the `MightyImmersion` directory in the SD card root that requires this permission. To do this:
    - Add the `MANAGE_EXTERNAL_STORAGE` permission to your Android Manifest:  
    `<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE" tools:ignore="ScopedStorage"/>`  
    - You may see an error saying "Namespace prefix 'tools' is not defined" or "XmlException: 'tools' is an undeclared prefix". To fix this, add the following to the `manifest` tag in your manifest:  
    `xmlns:tools="http://schemas.android.com/tools"`
    - However, declaring the `MANAGE_ALL_FILES` in manifest isn't enough. The user must be sent to the Android settings to grant this permission. MXR SDK provides a utility method to do this:  
    `MXRAndroidUtils.RequestManageAllFilesPermission();`  
    - If you want to check if the user has already granted the `MANAGE_EXTERNAL_STORAGE` permission or not, the MXR SDK provides a static property to query this:  
    `MXRAndroidUtils.IsExternalStorageManager`  
    - *Tip*: Try to invoke `MXRAndroidUtils.RequestManageAllFilesPermission` as early as possible on startup. This way your app and the MXR SDK will be able to access the ManageXR files stored on disk. Do note the following:
        * `MXRAndroidUtils.RequestManageAllFilesPermission` directly launches a native Android UI, you might want to show a popup that informs the user via a popup and go ahead after confirmation.
        * Initialize the ManageXR SDK or try to access MightyImmersion files after this permission has been granted.
        * Use the methods provided in the SDK instead of [Unitys Permissions struct](https://docs.unity3d.com/ScriptReference/Android.Permission.html) for requesting this permission and checking if it has been granted.


*Note:* Logging is enabled by default. Use `MXRSystem.EnableLogging = false` to disable messages from being logged to the console.  
___
## Support  
Please open a Github Issue or contact support@managexr.com for additional support.
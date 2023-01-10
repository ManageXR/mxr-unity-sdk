> ⚠️ This SDK is in alpha testing. We appreciate your patience while we continue to improve this SDK. Please report any issues via GitHub.

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

## Installation & Setup  
1. Install the SDK package in your Unity project. There are multiple methods:

   ### _Via  Package Manager window (No Available Yet - Coming Soon!)_  
   The UPM package is [available on OpenUPM](https://openupm.com/packages/com.mxr.unity.sdk). To install, follow these instructions:  
   - Add the OpenUPM registry with the ManageXR Unity SDK as a `scope`. To do this, go to `Edit/Project Settings/Package Manager`, add the OpenUPM scope registry with the URL `https://package.openupm.com` and add `com.mxr.unity.sdk` as a scope.

   - Install the package from OpenUPM registry. To do this, go to `Window/Package Manager/`. Select in the `My Registries` view (located at the top left, the default selection is `Unity Registry`), locate `ManageXR Unity SDK` and click install. After installation, the package will show up in the `In Project` view as well.
  
   ### _Via manifest.json_  
   - Go to `Packages/manifest.json` inside your Unity project  
   - Add `"com.mxr.unity.sdk" : "https://github.com/managexr/mxr-unity-sdk.git#upm@latest"` under the `dependencies` object  
   - __Note:__ This method is NOT recommended, as this causes Unity to always fetch the latest version of UPM package from the Github URL into your project. If a new version introduces breaking or unexpected changes in the SDK, your project might not work as expected. With the Package Manager Window method described above, you can lock your project to a specific version of the package and up/downgrade when you want.  
  
1. Setup MXR files for Unity Editor testing. This allows you to simulate SDK operations in the editor.  
   - After installation, go to `Library/PackageCache/com.mxr.unity.sdk@x.x.x/` directory in your Unity project and extract `Files.zip` to the root of your Unity project. Your project structure should then look like this:  
```
      <Unity Project Directory>  
      └── Assets
      └── Files  
            └── MightyImmersion  
      └── Library  
      └── ...
```  

   - It is recommended that you extract Files.zip in required location this every time you upgrade or downgrade the package version so the files inside remain relevant to the SDK version.  

1. _Optional: Import SDK samples_  
 There are two scenes included. One shows a  Library UI and the other is a Wifi status and connection interface.
  
## Usage  
The SDK uses `IMXRSystem` to communicate with the system layer. It provides methods, events, properties to observe, query and invoke operations in the ManageXR Admin/System.

To Initialize the `MXRSystem`, call `MXRManager.Init();`. You may then access the `MXRSystem` with `MXRManager.System`.

The `MXRSystem` relays information to your app through a few different value classes: 

- `MXRSystem.RuntimeSettingsSummary` contains all information about the device and its current configuration. This will include information about all of the apps, files, and settings that are deployed to this device. 
   - See `Assets/MXR.SDK/Runtime/Types/RuntimeTypes.cs` for full code documentation of this type and the data included in it. 
   - See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/runtimeSettingsSummary.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor)
   - You may subscribe to realtime changes of this data with `MXRManager.System.OnRuntimeSettingsSummaryChange += OnRuntimeSettingsSummaryChange;`
- `MXRSystem.DeviceStatus` contains all information about the device's current status. This includes the device's serial number and statuses of apps/files that are currently being downloaded (including download progress). 
   - See `Assets/MXR.SDK/Runtime/Types/StatusTypes.cs` for full code documentation of this type and the data included in it. 
   - See `Assets/MXR.SDK/Runtime/Editor/Files/MightyImmersion/deviceStatus.json` for an example of this data in json format. (Note: You can edit this json file and its data will be reflected in the Sample Scene in realtime when run in the editor)
   - You may subscribe to realtime  changes of this data with `MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChange;`
  
See `Assets/MXR.SDK/Samples/Scripts/LibraryPanel.cs` as an example of how to initialize the MXRSystem and subscribe to changes.

Intellisense code comments have been added for the key APIs and models. API reference and further documentation coming soon. 

Please open a Github Issue or contact support@managexr.com for additional support.
  
## Samples  
The repository includes samples that demonstrate basic integration with the Content API, Status API, and Wifi API.  

To install, select the ManageXR Unity SDK in the Package Manager window after you've imported it successfully and click on `Import Into Project`. The samples will be imported under `Assets/Samples` in your Unity project.

Please refer to `README.txt` inside the samples directory for further information about running samples in the Unity editor.

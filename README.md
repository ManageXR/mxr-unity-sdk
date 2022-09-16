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

## Installation  
1. Install the SDK package in your Unity project. The suggested methods are:
  
   ### _Via  Package Manager window_  
   Go to `Window/Package Manager/` click the plus button in the top left corner and click on `Add package from git URL` and paste this `https://github.com/manageXR/mxr-unity-sdk.git#upm`
  
   ### _Via manifest.json_  
   Go to your Unity project path/Packages/manifest.json  
   Add `"com.mxr.unity.sdk" : "https://github.com/managexr/mxr-unity-sdk.git#upm"` under the `dependencies` object

1. Setup MXR files for Unity Editor testing.  
   - Go to `Library/PackageCache/com.mxr.unity.sdk@x.x.x/` directory in your Unity project.    
   - Extract the `Files/` directory at the root of your Unity project. Your project structure should then look like this:  
```
      <Unity Project Directory>  
      └── Assets
      └── Files  
            └── MightyImmersion  
      └── Library  
      └── ...
```
  
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
Please refer to `Samples/README.md` for further information about running samples in the Unity editor.

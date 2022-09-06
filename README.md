# ManageXR Unity SDK
This repository contains the ManageXR Unity SDK that can be used to make custom homescreens in Unity for Android based VR headsets.

## Installation  
First, install the SDK package in your Unity project. The suggested methods are:
  
### Via  Package Manager window  
Go to `Window/Package Manager/` click the plus button in the top left corner and click on `Add package from git URL` and paste this `https://github.com/manageXR/mxr-unity-sdk.git#upm`
  
### Via manifest.json  
Go to your Unity project path/Packages/manifest.json  
Add `"com.mxr.unity.sdk" : https://github.com/managexr/mxr-unity-sdk.git#upm` under the `dependencies` object
  
Next, Copy the `Files/MightyImmersion` folder in your Unity project. The `Files` directory should be next to the `Assets` like in this repository with the `MightyImmersion` directory inside. This will allow you to test the SDK in the editor.
  
## Integration  
The SDK uses `IMXRSystem` to communicate with the system layer. It provides methods, events, properties to observe, query and invoke operations in the ManageXR Admin/System.  
  
Intellisense code comments have been added for the key APIs and models. API reference and further documentation are presently not available. 
  
## Samples  
The repository includes samples that demonstrate basic integration with the Wifi API, Content API and Status API.  
Please refer to `Samples/README.md` for further information about running samples in the Unity editor.
# __MXR SDK Command Simulator__

## Commands overview
The ManageXR web dashboard provides tools to remotely trigger actions on a device, these actions are called devices. [Learn more about Commands here.](https://help.managexr.com/en/articles/5417002-device-commands)  

Commands are triggered by the backend and sent to the MXR Admin App that is installed on your devices. Most of these commands are handled by the MXR Admin App itself.

However some commands are forwarded to the SDK. Such commands need to be handled by a Unity app that integrates with the MXR SDK.

## Types of MXR SDK Commands  
Currently there are two commands that the SDK provides. They are:
- Play Video command
- Pause Video command

### _Play Video Command_
This command is an instruction for the device to start playing a video. The data associated with this command contains the following information:
- `string videoId`: The ID of the video in the device's `RuntimeSettingsSummary` that must be played
- `bool playFromBeginning`: Whether the video should start playing from the beginning. When false, you can treat this like resume video. When true, 

### _Pause Video Command_
This command is an instruction for the device to pause any video it is currently playing. There is no data associated with this command.  

## Why Command Simulator?
Commands are issued from the ManageXR Web Dashboard, sent to the MXR Admin App installed installed on your XR device, and then forwarded to the SDK. The SDK then exposes the commands in the form of events in its API.  

These events are `IMXRSystem.OnPlayVideoCommand` and `IMXRSystem.OnPauseVideoCommand`.  
  
When integrating the SDK in your application, you might want to test the integration of commands in the editor. However, in the editor, the above flow of events isn't possible as there is to MXR Admin App running to enable the communication between the SDK and the web dashboard.  

For this, the SDK provides __MXR SDK CommandSimulator__, an editor window that allows you to process the commands in editor mode as if they have been sent by the MXR Admin App

## Using the Command Simulator
Check out the `Commands Sample` scene included with the SDK.  

Go to `Tools/MXR/MXR SDK Command Simulator` to open the simulator window and start the game.

Two command option buttons will be shown: Play video and Pause video. 

### _Play Video command_  
- Select the `Play Video` button in the simulator window.  

- A UI for configuring the data associated with play video command will be shown. 

- The dropdown allows you to easily select a video so that it's ID can be fetched. A checkbox allows you to specify if the video should be played from the beginning.

- Click the `Invoke Play Video Command` button. Any code subscribed to `IMXRSystem.OnPlayVideoCommand` will be executed.

### _Pause Video Command_  
- Select the `Pause Video` button in the simulator window.

- Since no data is associated with the Pause Video command, no configuration UI will be shown. 

- Click the `Invoke Pause Video Command` button. Any code subscribed to `IMXRSystem.OnPauseVideoCommand` will be executed.

## Notes
⚠️ Simulator initialization  
Refer to `CommandSubscriberExample.cs` to see how `MXRCommandSimulator` is initialized.  
`CommanSubscriberExample.cs` calls `MXRManager.Init()` The `Init` method, when called in editor, automatically creates an `EditorMXRSystem` object and assigns to its `System` property.  
While `MXRCommandSimulator.SetSystem` can accept any `IMXRSystem` object, at runtime it checks if the concrete type is `EditorMXRSystem` before it works.  
So, if you're not using `MXRManager` and want to simulate commands, be sure to create and use `EditorMXRSystem` when running your app in the editor.
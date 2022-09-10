MXR SDK Samples README

Sample Scene contains two panels under Canvas

LIBRARY PANEL
Shows the available apps, videos and WebXR apps/sites deployed on the device. See MXR.SDK/Samples/Scripts/LibraryPanel.cs

In the editor, go to UnityProjectDirectory/Files/MightyImmersion/deviceStatus.json 
and go to UnityProjectDirectory/Files/MightyImmersion/runtimeSettingsSummary.json to simulate
changes.

Once in play mode, select an instantiate RuntimeAppCell object and right click the "RuntimeAppCell (Script)" component
in the inspector, a "Refresh" option will be shown. This allows you to visualize different cell states. Try changing
any value in app or the status object in the component in the inspector and click the "Refresh" context menu, the
cell will automatically update based on the new app and status object.

The above "Refresh" tip applies to VideoCell and WebXRCell as well.

WIFI PANEL
Shows up to date Wifi Connection Status and available Wifi Networks.
Provides UI buttons to trigger Wifi API methods

In the editor, go to UnityProjectDirectory/Files/MightyImmersion/wifiConnectionStatus.json
and UnityProjectDirectory/Files/MightyImmersion/wifiNetworks.json to simulate changes in 
the wifi state.
# PulsoidToOSC
[![pulsoid](https://pulsoid.net/s/github-badge)](https://pulsoid.net/)

PulsoidToOSC is a simple, effective, and universal application that streams heart rate data from Pulsoid to OSC.<br>
The main window is designed to be easily used in VR with applications like OVR Toolkit, allowing you to place it on your hand like a monitor.<br>
Also includes easy-to-use VRChat integration with automated configuration through OSC query, letting you send heart rate data to your avatar or even to the chatbox with a customized message.

Requires [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.8-windows-x64-installer?cid=getdotnetcore)

## Pulsoid Connection

To connect this app to Pulsoid, you will need to obtain an authorization token:
1. Click the "Get Pulsoid token" button - this will open the authorization page in your web browser.
2. Log in and confirm authorization on the page.
3. Token will be automatically entered in the app and you can close the page when it says so.
4. Green checkmark will indicate valid token.

If the token is invalid, repeat the steps above.

**Disclaimer**: Anyone with access to the token can read real-time heart rate data from Pulsoid.



## Auto Start

This option starts the connection to Pulsoid and OSC when the app is opened, saving you one click.

It will NOT start the application with the system.



## OSC Options

You can set up a manual OSC endpoint, which can be any application capable of receiving OSC data, such as VRChat or Resonite.

The endpoint requires a specified IP, Port, and Path where heart rate parameters will be sent.

Default values are set to work with VRChat running on the same PC.



## VRChat

### OSC Query Auto Configuration

When enabled, all data will be sent to VRChat clients running on localhost (the same PC where this app is running) automatically, independent of the OSC settings. If you want to use this app only for VRChat, you can disable sending data to the manual OSC endpoint.

If you want to run this app on a different PC than VRChat but on the same LAN, you can enable "Send to all VRC Clients on LAN".


### Chatbox Messages

This setting provides simple options to send heart rate data to the VRChat chatbox with customized messages.

Messages containing key `<bpm>` will automatically replace this key with the heart rate value. If the message doesn't contain the key, the heart rate will be added at the end of the message.<br>
For example, this message `Heartrate: <bpm> BPM` will appear in the VRChat chatbox as: ***Heartrate: 123 BPM***

Another key is `<trend>` that will be replaced with an arrow indicating trend of how much the heart rate is changing.

Chatbox messages also support new line characters.<br>
`\v` or `/v` will simply start new line.<br>
`\n` or `/n` will start new line by filling the rest of the current line with spaces, thereby making the chatbox as wide as possible.

Messages are sent to the manually defined OSC endpoint and all auto-configured VRChat clients.


### Display and sound on avatar

Ready to use prefab of simple display for VRChat avatar with easy installation with VRCFury.<br>
Requires just one int (8 bits) of synced avatar parameters. The display have just one static mesh of one Quad (2 triangles) and one material slot.<br>
The package also includes Heart Beat Audio addon prefab. Heart beat sound tempo is synced with heart rate value and sounds are randomized, so they will not sound too repetitive.<br>
Download: [Heart Rate Display unitypackage](https://github.com/Honzackcz/PulsoidToOSC/raw/master/external-tools/VRChat/HeartRateDisplay.unitypackage)<br>
This display use edited version of RED_SIM's [Simple Counter Shader](https://www.patreon.com/posts/simple-counter-62864361).


## OSC Parameters

Parameters are combined with your defined OSC path, which you can set in the OSC options.
In the case of VRChat auto-configuration, the OSC path is defined as `/avatar/parameter/`, ensuring access to the parameters within avatars.

An example of a full path combined with a parameter might look like this: `/avatar/parameter/HeartRateInt`

### Default parameters are set up according to this table:

| Parameter Name     | Value Type | Description                              |
| ------------------ | ---------- | ---------------------------------------- |
| `HeartRateInt`     | Int        | Heart rate - Integer [0, 255]            |
| `HeartRate3`       | Int        | Same as HeartRateInt                     |
| `HR`               | Int        | Same as HeartRateInt                     |
| `HeartRateFloat`   | Float      | Heart rate - Float ([0, 255] -> [-1, 1]) |
| `HeartRate`        | Float      | Same as HeartRateFloat                   |
| `FullHRPercent`    | Float      | Same as HeartRateFloat                   |
| `HeartRateFloat01` | Float      | Heart rate - Float ([0, 255] -> [0, 1])  |
| `HeartRate2`       | Float      | Same as HeartRateFloat01                 |
| `HRPercent `       | Float      | Same as HeartRateFloat01                 |
| `HeartBeatToggle`  | Bool       | Toggles with each OSC update             |
| `isHRBeat`         | Bool       | Toggles with each OSC update             |
| `isHRConnected`    | Bool       | True when app is working                 |
| `isHRActive`       | Bool       | True when app is working                 |

These parameters are chosen to support most currently used systems. In practice, the only necessary parameters are `HeartRateInt` for data and the additional `HeartBeatToggle` for reliable detection of timeouts.

In case you want to use selees824's display [【VRCFury|MA 対応】HeartRate OSC](https://booth.pm/en/items/5531594) you will need to add parameters `HR Float [0, 1]` named `hr_percent` and `Bool Active` named `hr_connected` then set maximal range of heart rate float to 200.

All parameters can be easily edited.


### Supported parameter types are:

| Parameter Type   | Value Type | Description                                                                                 |
| ---------------- | ---------- | ------------------------------------------------------------------------------------------- |
| HR Integer       | Int        | Heart rate - Integer [0, 255]                                                               |
| HR Float [-1, 1] | Float      | Heart rate - Float ([0, 255] -> [-1, 1])                                                    |
| HR Float [0, 1]  | Float      | Heart rate - Float ([0, 255] -> [0, 1])                                                     |
| Bool Toggle      | Bool       | Toggles with each OSC update                                                                |
| Bool Active      | Bool       | True when app is working                                                                    |
| Trend [-1, 1]    | Float      | Trend of heart rate change - Float [-1, 1] <br> -1 = decreasing; 0 = stable; 1 = increasing |
| Trend [0, 1]     | Float      | Trend of heart rate change - Float [0, 1] <br> 0 = decreasing; 0.5 = stable; 1 = increasing |


### Parameter adjustments
In Heart rate options is possible to adjust float parameters.

Heart rate float can have adjusted range of heart rate values for systems that tries to improve accuracy by reducing minimal and maximal value of heart rate. By default the float uses full range of 0 - 255 because 8 bit float [-1, 1] should be able to handle all the possible values.

Trend float minimal and maximal values affect sensitivity to how fast heart rate is changing. Higher values will make the trend indication less sensitive.


## Used Libraries

[SharpOSC](https://github.com/ValdemarOrn/SharpOSC) is a small library designed to make interacting with Open Sound Control easy (OSC).

[ModernWPF](https://github.com/Kinnara/ModernWpf) - Modern styles and controls for your WPF applications.

[net-mdns](https://github.com/richardschneider/net-mdns) - A simple Multicast Domain Name Service.

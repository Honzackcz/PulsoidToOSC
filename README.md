PulsoidToOSC
============

Simple, effective and universal application that streams heart rate data from Pulsoid to OSC.<br>
Main window is designed to be easily used in VR with applications like OVR Toolkit as monitor that you can place on hand.<br>
Includes easy to use VRChat integration with automated configuration thanks to OSC query let you send heart rate data to your avatar or even to chatbox with customized message.


## Pulsoid Connection

To connect this app to Pulsoid, you will need to obtain authorization token and enter it.
1. click "Get Pulsoid token" button - this will open authorization page in your web browser
2. login and confirm authorization on the page
3. copy the token - now you can close the page
4. paste the token to the input field and press "Set Pulsoid token" - green checkmark will appear if the token is valid

In case the token is invalid, repeat the steps above again.

Disclaimer - anyone who have access to the token can read realtime data of your heart rate from Pulsoid.



## Auto Start

This will just start the connection to Pulsoid and OSC when the app is opened to save one click.

It will NOT start the application with system.



## OSC Options

There can be set up manual OSC endpoint that can be any application able to receive OSC data like VRChat or Resonite.

Endpoint needs specified IP, Port and Path where heart rate parameters will be send.

Default values are set to work with VRChat running on the same PC.



## VRChat
### OSC Query auto configuration
When enabled, all data will be send to VRChat clients running on localhost (the same PC where this app is running) automatically independently on OSC settings, so if you want to use this app only for VRChat you can also disable sending data to manual OSC endpoint.

In case when you want to run this app on different PC then VRChat that is also on the same LAN you can enable "Send to all VRC Clients on LAN".

### Chatbox messages
This settings provides simple options to send heart rate also to VRChat chatbox with customized messages.

Messages containing \<bpm\> will automatically replace this key by heart rate value. If the message doesn't contain the key, heart rate will be added at the end of the message.<br>
For example this message: ***Heartrate: \<bpm\> BPM***<br>
will in VRChat chatbox looks like this: ***Heartrate: 123 BPM***

Messages are send to manually defined OSC endpoint and all auto configured VRChat clients.



## OSC Parameters

**\<osc-path\>** is your defined path that you can set in options.<br>
In case of VRChat auto configuration, the \<osc-path\> is defined as /avatar/parameter/ this ensure access to the parameters within avatars.

| Parameters                    | Value Type | Description                  |
| ----------------------------- | ---------- | ---------------------------- |
| \<osc-path\>/HeartRateInt     | Int        | Int [0, 255]                 |
| \<osc-path\>/HeartRate3       | Int        | See HeartRateInt             |
| \<osc-path\>/HeartRateFloat   | Float      | Float ([0, 255] -> [-1, 1])  |
| \<osc-path\>/HeartRate        | Float      | See HeartRateFloat           |
| \<osc-path\>/HeartRateFloat01 | Float      | Float ([0, 255] -> [0, 1])   |
| \<osc-path\>/HeartRate2       | Float      | See HeartRateFloat01         |
| \<osc-path\>/HeartBeatToggle  | Bool       | Reverses with each update    |

Example of full path with parameter may looks like this: ***/avatar/parameter/HeartRateInt***

## Used libraries

[SharpOSC](https://github.com/ValdemarOrn/SharpOSC) is a small library designed to make interacting with Open Sound Control easy (OSC).

[ModernWPF](https://github.com/Kinnara/ModernWpf) - Modern styles and controls for your WPF applications.

# BlazorWebView
A [WebKitGTK](https://webkitgtk.org/) WebView, utilizing [Gir.Core](https://gircore.github.io/), for running [Blazor Hybrid](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/) applications on Linux without the need to compile a native library.  It is analagous to the [Winforms BlazorWebView](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.webview.windowsforms.blazorwebview) or the [WPF BlazorWebView](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.webview.wpf), but for Linux instead of Windows.

Blazor Hybrid apps allow one to create Blazor desktop applications that uses the local machine's .NET runtime (not Blazor WASM), has full access to local resources (not sandboxed), and does not require hosting through a web server (e.g. Blazor server). It is just like any other desktop application written in C#, but uses Blazor and web technologies to implement the user interface.

## Why?
Microsoft has decided to not support Maui on Linux, so there is currently no way to create Blazor Hybrid apps on Linux using *only* C#.

## How?
BlazorWebView uses some of the same code as [Steve Sanderson's WebWindow](https://github.com/SteveSandersonMS/WebWindow) and leverages [Microsoft's WebView infrastructure](https://github.com/dotnet/aspnetcore/tree/main/src/Components/WebView) to get Blazor Hybrid working.  However, it differs from WebWindow in that it doesn't require one to compile a native shared library in C++, instead utilizing [Gir.Core](https://gircore.github.io/) to call into the native libraries.   This has the benefit that, as long as the native libraries are installed on the Linux system, one only needs to use the `dotnet` CLI to build and run BlazorWebView.

## Screenshot
<img src="./Screens/screenshot.png" width="75%" height="75%">

## Demonstration

### Using a Linux terminal
```
git clone https://github.com/JinShil/BlazorWebView.git
cd BlazorWebView/WebKitGtk.Test
dotnet run
```

### Using Visual Studio Code
On a Linux computer, simply open this repository in Visual Studio Code and press F5 to start a debug session.

### Ubuntu 24.04 bwrap error fix
Running the demo project may fail with the following error on ubuntu 24.04:

    bwrap: setting up uid map: Permission denied

This is due to Ubuntu developers deciding to use Apparmor to restrict the creation of user namespaces. To resolve this create a file named `/etc/apparmor.d/bwrap` with the following contents:

    abi <abi/4.0>,
    include <tunables/global>

    profile bwrap /usr/bin/bwrap flags=(unconfined) {
      userns,

      # Site-specific additions and overrides. See local/README for details.
      include if exists <local/bwrap>
    }

Followed by a reboot. 
(Note: this fix was found [here](https://etbe.coker.com.au/2024/04/24/ubuntu-24-04-bubblewrap/))

## Usage
See the project in [WebKitGtk.Test](https://github.com/JinShil/BlazorWebView/tree/main/WebKitGtk.Test) for an example illustrating how to create a Blazor Hybrid application using the BlazorWebView.

You may need to install the following packages:
* libadwaita-1-0
* libwebkitgtk-6.0-4

## Status
This project was tested on:
- Windows Subsystem for Linux
- Raspberry Pi Bullseye 64-bit
- Debian Bullseye 64-bit.
- Ubuntu 24.04 LTS desktop amd64

Note that on the latest Raspberry Pi Bookworm OS, you will likely encounter https://github.com/raspberrypi/linux/issues/5750.  You can work around the issue by adding `webView.GetSettings().SetHardwareAccelerationPolicy(WebKit.HardwareAccelerationPolicy.Never);`.

The root .vscode directory has the necessary configuration files to build, deploy, and debug a Raspberry Pi from a Debian Bullseyse 64-bit workstation PC.

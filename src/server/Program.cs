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
    TcpListener tcpListener = new TcpListener(IPAddress.Parse(ipAddress), 8802);
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
            // Execute command
            if ((command.StartsWith("execute") || command.StartsWith("exec") || command.StartsWith("!")) && command.Split(' ').Length >= 3)
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
        //string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{fileName}";
        string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{Path.GetFileNameWithoutExtension(path)}_(recieved){Path.GetExtension(path)}";
        try
        {
            await Writer.WriteLineAsync("dload-ready");
            await Writer.FlushAsync();
            string base64Data = await Reader.ReadLineAsync();
            byte[] fileData = Convert.FromBase64String(base64Data);
            File.WriteAllBytes(savePath, fileData);
            Console.WriteLine("The file is received and saved as: " + savePath);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error when receiving the file: {e.Message}");
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
                Console.WriteLine("File has been sent: " + path);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error when sending the file: {e.Message}");
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
        "Version :: 1.3"
    };

    protected string[] menu =
{
        "COMMANDS :: Available commands show below",
        "",
        "[*] Clear terminal                         CLEAR, CLR",
        "[*] Execute command on remote client       EXECUTE, EXEC, !",
        "[*] Download file on remote client         DOWNLOAD, DLOAD, +",
        "[*] Upload file to remote client           UPLOAD, =",
        "[*] Show connections list                  CONNECTION-LIST, CON-LIST",
        "[*] Remove connection with remote client   CONNECTION-REMOVE, CON-REMOVE",
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
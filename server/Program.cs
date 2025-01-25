using System.Net;
using System.Net.Sockets;

ServerObject server = new ServerObject();
await server.ListenAsync();
class ServerObject
{
    TcpListener tcpListener = new TcpListener(IPAddress.Any, 8802);
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
            string? command = Console.ReadLine();
            if ((command.StartsWith("execute") || command.StartsWith("exec")) && command.Split(' ').Length >= 3)
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
            else if (command.StartsWith("connection"))
            {
                if (command.Split(' ')[1] == "list")
                {
                    foreach (var client in clients)
                    {
                        Console.WriteLine("- " + client.Name + " " + client.Id);
                    }
                }
                else if (command.Split(' ')[1] == "remove" && command.Split(' ').Length >= 3)
                {
                    RemoveConnection(command.Split(' ')[2]);
                }
            }
            else if (command.StartsWith("clear")) Console.Clear();
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
                    if (first == true) Console.WriteLine($"{Name} ({Id})");
                    first = false;
                    Console.WriteLine(message);
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

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }
}
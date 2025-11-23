using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Razrabotka_Prog_APIandSDK.Windows.Notifications;
//using System.Windows.Form;

namespace MainConsoleServer
{
    #region Класс пользователя
    // Пользователь для списка
    // Формат: { [i)] [Namber] [Name] [Ip] [Status] }
    // Пример: { [1)] [001] [Капютор201_кабинета] [172.123.32.1] [пользователь] : [в сети] }
    public class UserIp
    {
        public string Namber { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Ip { get; set; }

        public UserIp(string namber, string name, string ip, string status = "пользователь")
        {
            Namber = namber;
            Name = name;
            Ip = ip;

            // Установка статуса с проверкой допустимых значений
            switch (status)
            {
                case "Администатор":
                    Status = status;
                    break;
                case "Соо-Администатор":
                    Status = status;
                    break;
                case "Пользователь":
                    Status = status;
                    break;
                case "Гость":
                    Status = status;
                    break;
                default:
                    Status = "Пользователь";
                    break;
            }
        }
    }
    #endregion

    #region Основной класс программы
    public class Programm
    {
        static Notifications notifications = new Notifications();
        #region Статические переменные
        static bool AktiveServer = false;                    // Флаг активности сервера
        static TcpListener _listener;                        // TCP listener для приема подключений
        static List<TcpClient> _connectedClients = new List<TcpClient>(); // Список подключенных клиентов
        static List<UserIp> _users = new List<UserIp>();     // Список пользователей
        static string NameServer = string.Empty;             // Имя сервера
        static string IpServer = string.Empty;               // IP адрес сервера
        static string PortServer = string.Empty;               // IP адрес сервера
        static List<UserIp> userIp = new List<UserIp>();     // Дополнительный список пользователей
        #endregion

        #region Точка входа
        static void Main(string[] args)
        {
            Console.WriteLine("Консоль - контроля локальных серверов");
            Console.WriteLine("\t1 - Создать/Настроить. ");
            Console.WriteLine("\t2 - Подключиться. ");

            ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);
            if (consoleKeyInfo.KeyChar == '1')
            {
                StartMenu();
            }
            else if (consoleKeyInfo.KeyChar == '2')
            {
                Console.WriteLine("Введите пораметры для подключения.");
                UserConectin();
            }
            else
            {
                // Неизвестная команда - ничего не делаем
            }
        }
        #endregion

        #region Глобальные сервисы (заглушка)
        static void GlobalSeve()
        {
            // Метод для глобальных сервисов (в разработке)
        }
        #endregion

        #region Управление сервером
        static void ServerControl()
        {

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🔵 Сервер Активен!");
            Console.WriteLine($"Имя: [{NameServer}].  Server: [{IpServer}]:{PortServer}");

            Console.Write('\n');
            Console.WriteLine("========ПАНЕЛЬ  УПРАВЛЕИЯ=========");
            Console.WriteLine(" h [help / помощь] - Подсказка контроля ");
            Console.WriteLine(" q [quit / выйти] - Завершить работу сервера ");
            Console.WriteLine("==================================");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n\n");
            while (AktiveServer)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey();

                    // Обработка команды выхода (q/й)
                    if ((key.KeyChar == 'q' || key.KeyChar == 'Q') || (key.KeyChar == 'й' || key.KeyChar == 'Й'))
                    {
                        int Progress = 0;
                        AktiveServer = false;
                        _listener?.Stop();

                        // Закрытие всех подключенных клиентов
                        foreach (var client in _connectedClients)
                        {
                            Console.WriteLine($"\r {Progress}/{_connectedClients.Count}");
                            client.Close();
                            Console.WriteLine($"{client}: Закрыто");
                            Progress += 1;
                        }

                        Console.WriteLine($"Было: {_connectedClients.Count} / Успешно устоновленно: {_connectedClients.Count}");
                        Console.WriteLine("Сервер остановлен");
                        Console.ReadLine();
                        break;
                    }

                    // Обработка команды помощи (h/р)
                    if ((key.KeyChar == 'H' || key.KeyChar == 'h') || (key.KeyChar == 'р' || key.KeyChar == 'Р'))
                    {
                        Console.WriteLine("> - help");
                        Console.WriteLine("q - Завершить работу Сервера.");
                        Console.WriteLine("u - Открыть панель упровления пользователей");
                        Console.WriteLine("d - Открыть панель управления устройсвом");
                        Console.WriteLine("s - Открыть панель управления Сервера");
                        Console.WriteLine("c - Очистить консоль");
                        Console.WriteLine("t - проверка (отправит всем подлючённым устройсвам сообщение.)");
                    }

                    // Обработка команды информации о сервере (s/ы)
                    if ((key.KeyChar == 'S' || key.KeyChar == 's') || (key.KeyChar == 'ы' || key.KeyChar == 'Ы'))
                    {
                        Console.WriteLine("> - Server / Сервер:");
                        Console.WriteLine($"Имя: {NameServer}");
                        Console.WriteLine($"IP: {IpServer}");
                        Console.WriteLine($"Статус: в Сети");
                        Console.WriteLine($"В даный момент подключены: {_users.Count} устройсв ");
                        Console.WriteLine($"======== Дополнительная Ифнормация ======== ");
                        Console.WriteLine($"Файл: ");
                        Console.WriteLine($"Создан: ");
                    }

                    // Обработка команды управления устройством (d/в)
                    if ((key.KeyChar == 'D' || key.KeyChar == 'd') || (key.KeyChar == 'в' || key.KeyChar == 'В'))
                    {
                        ControlDevaise();
                    }

                    // Обработка команды управления пользователями (u/г)
                    if ((key.KeyChar == 'U' || key.KeyChar == 'u') || (key.KeyChar == 'Г' || key.KeyChar == 'г'))
                    {
                        try
                        {
                            if (_users.Count > 0)
                            {
                                foreach (var item in _users)
                                {
                                    Console.WriteLine($"User: {item.Name} Namber: {item.Namber} Ip: {item.Ip} {item.Status}\n");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"В данный момент, у сервера {NameServer} -> {_users.Count} пользователей");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Не вышло обработать запрос.\n{ex.Message}");
                        }
                    }
                   
                    // Обработка команды управления пользователями (с/с)
                    if ((key.KeyChar == 'C' || key.KeyChar == 'c') || (key.KeyChar == 'С' || key.KeyChar == 'с'))
                    {
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("🔵 Сервер Активен!");
                        Console.WriteLine($"Имя: [{NameServer}].  Server: [{IpServer}]:{PortServer}");

                        Console.Write('\n');
                        Console.WriteLine("========ПАНЕЛЬ  УПРАВЛЕИЯ=========");
                        Console.WriteLine(" h [help / помощь] - Подсказка контроля ");
                        Console.WriteLine(" q [quit / выйти] - Завершить работу сервера ");
                        Console.WriteLine("==================================");
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    if((key.KeyChar == 'T' || key.KeyChar == 't') || (key.KeyChar == 'е' || key.KeyChar == 'Е'))
                    { 
                        string messageToSend = "meg -tmb standart";
                        byte[] data = Encoding.UTF8.GetBytes(messageToSend);

                        foreach (var client in _connectedClients.ToList())
                        {
                            if (client.Connected)
                            {
                                try
                                {
                                    NetworkStream stream = client.GetStream();
                                    stream.Write(data, 0, data.Length);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"❌ Ошибка отправки: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                //Thread.Sleep(100);
            }
        }
        #endregion

        #region Управление устройством
        static void ControlDevaise()
        {
            Console.WriteLine("> - Devaise / Устройсво:");
            Console.WriteLine("c - Завершить работу всех программ на устройсве.");
            Console.WriteLine("n - Отправить уведовлемие на устройсво");
            Console.WriteLine("r - Перезапустить устройсво");
            Console.WriteLine("s - Открыть панель управления Сервера");
        }
        #endregion

        #region Главное меню
        static void StartMenu()
        {
            #region Настройки порта
            // Порт сервера.

            // Так-как порт  локальной сети, а не просто "рекдко используемый".
            // Мы можем своюодно и открыто его хронить, а так-же считать,
            // количесво утсройсв, что в данный момент подключены.
            // int CointServer = File.ReadAllText(@"D:\WindowsScanerSystem\WindowsScanerSystem\TextListServer.txt").Length; // Узнать количесво серверов (это формат хронения: {path}\n)
            int CointServer = 0; // Узнать количесво серверов (это формат хронения: {path}\n)
            #endregion

            // Если нет активных серверов
            if (CointServer == 0)
            {
                while ((true))
                {
                    Console.Clear();
                    Console.WriteLine($"В данный момент, нет активных серверов этой сети, которые считываються нащей программой.");
                    Console.WriteLine($"Желаете создать?");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"1 - Создать ");
                    Console.WriteLine($"2 - Изменить настройки ");
                    Console.WriteLine($"");
                    Console.WriteLine($"0 - Выйти / Закрыть программу. ");
                    Console.ForegroundColor = ConsoleColor.White;
                    ConsoleKeyInfo consoleKeyInfo = Console.ReadKey();

                    if (consoleKeyInfo.KeyChar == '1')
                    {
                        CreateServer().Wait();
                    }
                    else if (consoleKeyInfo.KeyChar == '2')
                    {
                        Settengs();
                    }
                    else if (consoleKeyInfo.KeyChar == '0')
                    {
                        Console.Clear();
                        return;
                    }
                    else
                    {
                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine($"Неверный выбор меню.");
                        Console.ForegroundColor = ConsoleColor.White;
                        Thread.Sleep(1000);
                    }
                }
            }
            else
            {
                // Если есть активные серверы
                Console.WriteLine($"Всего {CointServer} сервегов:");
                string fileContent = File.ReadAllText("D:\\WindowsScanerSystem\\WindowsScanerSystem\\TextListServer.txt");
                string[] PathServer = fileContent.Split('\n');

                Console.WriteLine("Список серверов:");
                for (int i = 0; i < PathServer.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(PathServer[i]))
                    {
                        Console.WriteLine($"[{i + 1}] {PathServer[i].Trim()}");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"1 - Открыть ");
                Console.WriteLine($"2 - Создать ");
                Console.WriteLine($"");
                Console.WriteLine($"0 - Выйти / Закрыть программу. ");
                Console.ReadLine();
            }
        }
        #endregion

        #region Настройки
        static void Settengs()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"1 - Создать новый файл Сервера. ");
                Console.WriteLine($"2 - Изменить уже сущесвующий файл. ");
                Console.WriteLine($"");
                Console.WriteLine($"0 - Выйти. ");
                Console.ForegroundColor = ConsoleColor.White;

                ConsoleKeyInfo SetengsKey = Console.ReadKey();
                if (SetengsKey.KeyChar == '1')
                {
                    SettengsServer("новый");
                }
                else if (SetengsKey.KeyChar == '2')
                {
                    SettengsServer();
                }
                else if (SetengsKey.KeyChar == '0')
                {
                    break;
                }
                else
                {
                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"Неверный выбор меню.");
                    Console.ForegroundColor = ConsoleColor.White;
                    Thread.Sleep(1000);
                }
            }
        }

        static void SettengsServer()
        {
            Console.Clear();
            Console.WriteLine($"Настройка сущесвующего файла сервера.");
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }

        static void SettengsServer(string redact)
        {
            Console.Clear();
            Console.WriteLine($"Настройка " + redact + ". ");
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();
        }
        #endregion

        #region Обработка сообщений
        // Обработка команд от сервера
        static void ProcessServerCommand(string command, string serverName)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📨 Команда от {serverName}: {command}");
            Console.ForegroundColor = ConsoleColor.White;

            // Разделяем команду на части
            string[] commandParts = command.Trim().Split(' ');
            

            try
            {
                if (commandParts[0] == "devaise")
                {
                    if (commandParts[1] == "-r")
                    {

                    }
                    else if (commandParts[1] == "-g")
                    {

                    }
                    else if (commandParts[1] == "-off")
                    {

                    }
                }
                else if (commandParts[0] == "meg") 
                {
                    if (commandParts[1] == "-tmb")
                    {
                        if (commandParts[2] == "standart")
                        {
                            notifications.Show("Сообщение для Админа, Устройсво в Сети!");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка выполнения команды: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        #endregion

        #region Создание сервера
        static async Task CreateServer()
        {
            Console.Clear();
            Console.Write("Введите порт (0 для автоматического выбора): ");
            if (!int.TryParse(Console.ReadLine(), out int port))
            {
                port = 0;
            }

            // Если порт 0, выбираем автоматически
            if (port == 0)
            {
                port = GetAvailablePort();
            }

            string _localIp = GetLocalIPAddress();
            IpServer = _localIp;
            PortServer = port.ToString();
            try
            {
                _listener = new TcpListener(IPAddress.Parse(_localIp), port);
                _listener.Start();
                AktiveServer = true;

                Console.Clear();
                Console.WriteLine($"Сервер запущен!");
                Console.WriteLine($"IP: {_localIp}");
                Console.WriteLine($"Порт: {port}");
                Console.WriteLine($"Сеть: {GetNetworkInfo()}");

                Console.WriteLine($"Дайте Имя Серверу.");
                Console.Write($"> ");
                NameServer = Console.ReadLine();
                if (NameServer.Length <= 0)
                {
                    Console.WriteLine($"Имя Сервера - не может быть 0 Символов");
                }

                // Запускаем прием клиентов в фоновом режиме
                _ = Task.Run(AcceptClients);

                // Переходим в режим контроля сервера
                ServerControl();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
            }
        }
        #endregion

        #region Прием клиентов
        static async Task AcceptClients()
        {
            while (AktiveServer)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _connectedClients.Add(client);

                    var clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string clientIp = clientEndPoint?.Address.ToString();

                    // Создаем пользователя
                    var user = new UserIp(
                        (_users.Count + 1).ToString("D3"),
                        $"Устройство_{_users.Count + 1}",
                        clientIp
                    );
                    _users.Add(user);

                    Console.WriteLine($"✅ Подключен клиент: {clientIp}");

                    // Обрабатываем клиента
                    _ = Task.Run(() => HandleClient(client, user));
                }
                catch (Exception ex)
                {
                    if (AktiveServer)
                        Console.WriteLine($"⚠️ Ошибка подключения: {ex.Message}");
                }
            }
        }
        #endregion

        #region Обработка клиента
        static async Task HandleClient(TcpClient client, UserIp user)
        {
            try
            {
                using (var stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];

                    while (AktiveServer && client.Connected)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"📨 Сообщение от {user.Name}: {message}");
                        if(message == "/d")
                        {
                            _connectedClients.Remove(client);
                            _users.Remove(user);
                            client.Close();
                            Console.WriteLine($"🔌 Клиент отключен: {user.Name}");
                        }
 
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка с клиентом {user.Name}: {ex.Message}");
            }
            finally
            {
               // 
            }
        }
        #endregion

        #region Вспомогательные методы
        // Получение локального IP адреса
        static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip) && (ip.ToString().StartsWith("192.168.") || ip.ToString().StartsWith("10.") || ip.ToString().StartsWith("172.")))?.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // Получение доступного порта
        static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        // Получение информации о сети
        static string GetNetworkInfo()
        {
            try
            {
                var interface_ = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                if (interface_ != null)
                {
                    var ipProps = interface_.GetIPProperties();
                    var gateway = ipProps.GatewayAddresses.FirstOrDefault()?.Address;
                    return $"Шлюз: {gateway}";
                }
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }

            return "Информация недоступна";
        }
        #endregion

        #region Подключение пользователя
        #region Подключение пользователя (Только прослушивание)
        static void UserConectin()
        {
            Console.Clear();
            Console.WriteLine("=== Подключение к серверу ===");

            Console.Write("Введите имя сервера: ");
            string NameServer = Console.ReadLine();

            Console.Write("Введите Ip-Адрес сервера: ");
            string IpServer = Console.ReadLine();

            Console.Write("Введите порт сервера: ");
            if (!int.TryParse(Console.ReadLine(), out int port))
            {
                port = 0;
            }

            // Проверка введенных данных
            if (string.IsNullOrWhiteSpace(NameServer) || string.IsNullOrWhiteSpace(IpServer) || port == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ Ошибка: Не все данные заполнены корректно!");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\n📋 Данные для подключения:");
            Console.WriteLine($"   Имя сервера: {NameServer}");
            Console.WriteLine($"   IP-адрес: {IpServer}");
            Console.WriteLine($"   Порт: {port}");

            // Запрос разрешения на подключение
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n⚠️  Пользователь запрашивает разрешение на подключение к серверу");
            Console.WriteLine($"   Сервер: {NameServer} ({IpServer}:{port})");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("\nНажмите любую клавишу для начала подключения...");
            Console.ReadKey();

            // ЗАПУСКАЕМ ПРОСЛУШИВАНИЕ СИНХРОННО, ЧТОБЫ МЕТОД НЕ ЗАВЕРШИЛСЯ
            StartListeningMode(IpServer, port, NameServer).Wait(); // Добавляем .Wait() для ожидания завершения
        }

        // Режим ТОЛЬКО прослушивания (без отправки сообщений)
        static async Task StartListeningMode(string ip, int port, string serverName) // Меняем void на Task
        {
            try
            {
                Console.WriteLine($"\n🔄 Подключение к {ip}:{port}...");
                Console.WriteLine("📡 Режим: ТОЛЬКО ПРОСЛУШИВАНИЕ");
                Console.WriteLine("⏹️  Для остановки нажмите 'Q'\n");

                using (TcpClient client = new TcpClient())
                {
                    // Подключаемся к серверу
                    await client.ConnectAsync(ip, port);

                    if (client.Connected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Успешно подключено к серверу: {serverName}");
                        Console.ForegroundColor = ConsoleColor.White;

                        NetworkStream stream = client.GetStream();

                        // Запускаем прослушивание команд от сервера
                        await ListenForServerCommands(stream, serverName);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Не удалось подключиться к серверу");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine("\nНажмите любую клавишу для возврата в меню...");
            Console.ReadKey();
        }

        // Прослушивание команд ТОЛЬКО от сервера
        static async Task ListenForServerCommands(NetworkStream stream, string serverName)
        {
            byte[] buffer = new byte[4096];
            bool isListening = true;

            // Запускаем фоновую задачу для отслеживания клавиши выхода
            var exitTask = Task.Run(() =>
            {
                while (isListening)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Q)
                        {
                            isListening = false;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            });

            try
            {
                while (isListening && stream.CanRead)
                {
                    // Ожидаем данные от сервера
                    if (stream.DataAvailable)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessServerCommand(command, serverName);
                    }

                    // Небольшая задержка для уменьшения нагрузки на CPU
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка получения команд: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                isListening = false;
                Console.WriteLine("\n🔌 Отключено от сервера");
            }
        }
        #region Реализация команд
        static void ExecuteShutdown()
        {
            Console.WriteLine("🔄 Выполнение: Выключение компьютера...");
            // System.Diagnostics.Process.Start("shutdown", "/s /t 0");
        }

        static void ExecuteRestart()
        {
            Console.WriteLine("🔄 Выполнение: Перезагрузка компьютера...");
            // System.Diagnostics.Process.Start("shutdown", "/r /t 0");
        }

        static void ExecuteLock()
        {
            Console.WriteLine("🔒 Выполнение: Блокировка компьютера...");
            // System.Diagnostics.Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
        }

        static void ShowMessage(string message)
        {
            Console.WriteLine($"💬 Сообщение: {message}");
            // Можно добавить вывод в MessageBox для Windows Forms
            // MessageBox.Show(message, "Сообщение от сервера");
        }

        static void SendDeviceStatus()
        {
            // Здесь можно собрать и отправить статус устройства
            string status = $"Устройство: {Environment.MachineName}\n" +
                           $"ОС: {Environment.OSVersion}\n" +
                           $"Пользователь: {Environment.UserName}\n" +
                           $"Время: {DateTime.Now:HH:mm:ss}";

            Console.WriteLine($"📊 Отправка статуса устройства:\n{status}");
        }

        static void ExecuteProgram(string programPath)
        {
            Console.WriteLine($"🚀 Запуск программы: {programPath}");
            try
            {
                // System.Diagnostics.Process.Start(programPath);
                Console.WriteLine($"✅ Программа запущена: {programPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка запуска: {ex.Message}");
            }
        }
        #endregion

        // Метод для логирования подключений (опционально)
        static void LogConnectionAttempt(string ip, int port, string serverName, string status)
        {
            try
            {
                string logDirectory = @"D:\WindowsScanerSystem\WindowsScanerSystem\Logs";
                string logFile = Path.Combine(logDirectory, "client_connection_log.txt");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Сервер: {serverName} | IP: {ip}:{port} | Статус: {status}";
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось записать в лог: {ex.Message}");
            }
        }
        #endregion

        // Метод для попытки подключения к серверу
        static async void AttemptConnection(string ip, int port, string serverName)
        {
            try
            {
                int second = 5;
                Console.WriteLine($"\nПопытка подключения к {ip}:{port}...");

                // Создаем TCP-клиент
                using (TcpClient client = new TcpClient())
                {
                    // Пытаемся подключиться с таймаутом 5 секунд
                    var connectTask = client.ConnectAsync(ip, port);
                    var timeoutTask = Task.Delay(second * 1000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Таймаут подключения: сервер не ответил за {second} секунд");
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }

                    // Проверяем, успешно ли подключились
                    if (client.Connected)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✅ Успешно подключено к серверу: {serverName}");
                        Console.ForegroundColor = ConsoleColor.White;

                        // Логирование успешного подключения
                        LogConnectionAttempt(ip, port, serverName, "SUCCESS");

                        // Здесь можно добавить дальнейшую логику работы с подключением
                        // Например, получение потока для обмена данными
                        NetworkStream stream = client.GetStream();

                        Console.WriteLine("📨 Готов к обмену сообщениями...");
                        // Дополнительная логика работы с сервером...
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Не удалось подключиться к серверу");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Ошибка подключения: {ex.Message}");
                Console.ForegroundColor = ConsoleColor.White;

                // Логирование ошибки подключения
                LogConnectionAttempt(ip, port, serverName, $"ERROR: {ex.Message}");
            }
        }
        #endregion
    }
    #endregion
}
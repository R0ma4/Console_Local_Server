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
        #region Статические переменные
        static bool AktiveServer = false;                    // Флаг активности сервера
        static TcpListener _listener;                        // TCP listener для приема подключений
        static List<TcpClient> _connectedClients = new List<TcpClient>(); // Список подключенных клиентов
        static List<UserIp> _users = new List<UserIp>();     // Список пользователей
        static string NameServer = string.Empty;             // Имя сервера
        static string IpServer = string.Empty;               // IP адрес сервера
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
            //Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("🔵 Сервер Активен!");
            Console.WriteLine("Нажмите 'q' для остановки сервера");

            Console.Write('\n');
            Console.WriteLine("========ПАНЕЛЬ  УПРАВЛЕИЯ=========");
            Console.WriteLine(" h [help / помощь] - Подсказка контроля ");
            Console.WriteLine(" q [quit / выйти] - Завершить работу сервера ");
            Console.WriteLine("==================================");

            Console.ForegroundColor = ConsoleColor.White;

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
                    Console.Write('\n');
                }

                //  Thread.Sleep(100);
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
        static void ReadMessege(string messege)
        {
            Console.WriteLine("> " + messege);
            string[] strings = messege.Split(' ');
            string ip = IpServer;

            // Обработка команды информации о сервере
            if (strings[0] == "/Server")
            {
                Console.WriteLine("Команда /Server - информация о сервере");
                // Здесь можно вывести статус сервера
            }
            // Обработка команды настроек
            else if (strings[0] == "/Setengs")
            {
                Console.WriteLine("Команда /Setengs - настройки");
            }
            // Обработка команды пользователей сервера
            else if (strings[0] == "/Server-User")
            {
                Console.WriteLine("Команда /Server-User - пользователи сети");
            }
            // Неизвестная команда
            else
            {
                Console.WriteLine($"{strings[0]} - не верная каманда.");
                Console.WriteLine($"Или");
                Console.WriteLine($"{ip} - Сеть не смогла считать комнажу {strings[0]}");
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
                        ReadMessege(message);
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
            AttemptConnection(IpServer, port, NameServer);
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("\nВыберите действие:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Y - Разрешить подключение");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("N - Запретить подключение");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("\nНажмите q - для завершения.");
            Console.ReadKey();

            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.WriteLine(); // Переход на новую строку

            if (keyInfo.Key == ConsoleKey.Q)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Подключение разрешено!");
                Console.ForegroundColor = ConsoleColor.White;
                // Здесь будет код для фактического подключения к серверу
            }
        }

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

        // Метод для логирования попыток подключения
        static void LogConnectionAttempt(string ip, int port, string serverName, string status)
        {
            try
            {
                string logDirectory = @"D:\WindowsScanerSystem\WindowsScanerSystem\Logs";
                string logFile = Path.Combine(logDirectory, "connection_log.txt");

                // Создаем директорию если ее нет
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // Записываем в лог
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | Сервер: {serverName} | IP: {ip}:{port} | Статус: {status}";
                File.AppendAllText(logFile, logEntry + Environment.NewLine);

                Console.WriteLine($"📝 Запись добавлена в лог: {logFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Не удалось записать в лог: {ex.Message}");
            }
        }
        #endregion
    }
    #endregion
}
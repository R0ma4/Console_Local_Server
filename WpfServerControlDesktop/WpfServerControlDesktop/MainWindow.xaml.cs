using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfServerControlDesktop
{
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
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer ServerAktine;
        private DispatcherTimer UpdateProtocol;
        #region Статические переменные
        //static Notifications notifications = new Notifications(); // 
        static bool AktiveServer = false;                    // Флаг активности сервера
        static TcpListener _listener;                        // TCP listener для приема подключений
        static List<TcpClient> _connectedClients = new List<TcpClient>(); // Список подключенных клиентов
        static List<UserIp> _users = new List<UserIp>();     // Список пользователей
        static string NameServer = string.Empty;             // Имя сервера
        static string IpServer = string.Empty;               // IP адрес сервера
        static string PortServer = string.Empty;               // IP адрес сервера
        #endregion
        public MainWindow()
        {
            InitializeComponent();

            try
            {
               if (!IsFirewallEnabled()) 
               {
                   string stamdart_name = "WpfServerControlDesktop";
                   TextBlockError.Text = "Ошибка прав от Брандмауэра:\n у программы нет прав, на контроль серверов на устройсве!\nПрограмма открыла нужно окно, пожалуйста, выдайте нужные прова!";
                   TextBlockError.Text += $"\nИмя процесса: {stamdart_name} - достуб - \"частный\"";
               
                   OpenFirewallAdvanced();
                   BtnOpenGridCreateServer.IsEnabled = false;
                   BtnOpenGridConectServer.IsEnabled = false;
               }

                if (UpdateProtocol == null)
                {
                    UpdateProtocol = new DispatcherTimer();
                    UpdateProtocol.Interval = TimeSpan.FromMilliseconds(1000);
                    UpdateProtocol.Tick += UpdateProtocol_Tick;
                }
                else 
                {
                    MessageBox.Show("Не вышло настроить стадию обновления","Ошибка насройки, шаг1",MessageBoxButton.OKCancel,MessageBoxImage.Exclamation);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OKCancel,MessageBoxImage.Error);
            }
        }


        public static void OpenFirewallAdvanced()
        {
            try
            {
                // Этот скрипт пытается создать правило, что вызовет системное окно
                string script = @"
            $ruleName = 'MyAppRule'
            $port = 8080
            $appPath = '" + System.Reflection.Assembly.GetExecutingAssembly().Location + @"'
            
            # Проверяем, есть ли уже правило
            $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
            
            if (-not $existingRule) {
                # Пробуем создать правило - это может вызвать системное окно
                New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Protocol TCP -LocalPort $port -Program $appPath -Action Allow
            }
        ";

                Process.Start("powershell.exe", $"-Command \"{script}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

             // Process.Start("ms-settings:windowsfirewall");
             ProcessStartInfo startInfo = new ProcessStartInfo
             {
                 FileName = "control.exe",
              
                 Verb = "runas", // Запуск от имени администратора
                 UseShellExecute = true
             };
             Process.Start("control.exe", "/name Microsoft.WindowsFirewall");
             // классический брандмауэр
             // Process.Start("wf.msc"); //  "Брандмауэр Windows в режиме повышенной безопасности"
        }
        private void UpdateProtocol_Tick(object sender, EventArgs e)
        {
            try
            {
                WindowProgramm.Width = 600; WindowProgramm.Height = 650;
            }
            catch (NullReferenceException rEx) { }
            catch (Exception ex) { }
        }



        #region Сетевая проверка

        // Проверяем, включен ли брандмауэр
        public static bool IsFirewallEnabled()
        {
            try
            {
                return CheckFirewallViaNetsh();
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckFirewallViaNetsh()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = "advfirewall show allprofiles state";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains("ON") &&
                       (output.Contains("Включено") || output.Contains("ON"));
            }
            catch
            {
                return false;
            }
        }

        public static bool HasFirewallRuleForApp(string appPath)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = $"advfirewall firewall show rule name=all dir=in";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains(appPath) ||
                       output.Contains(Process.GetCurrentProcess().ProcessName);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsPortOpenInFirewall(int port, string protocol = "TCP")
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments = $"advfirewall firewall show rule name=all";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return output.Contains($"LocalPort: {port}") &&
                       output.Contains($"Protocol: {protocol}");
            }
            catch
            {
                return false;
            }
        }

        #endregion
      
        private static void ServerAktines(object sender, EventArgs e) { }
        #region Создание и настройка сервера
        void ServerAktive()
        {
            // Показываем активный сервер и скрываем форму создания
            GridServerAktive.Visibility = Visibility.Visible;
            GridServerCreate.Visibility = Visibility.Collapsed;

            // Получаем данные из UI элементов
            string serverName = UserCreateServerName.Text;
            string ipText = UserCreateServerIp.Text;
            string portText = UserCreateServerPort.Text;

            // Получаем порт
            int port;
            if (!int.TryParse(portText, out port))
            {
                // Если порт не указан, выбираем автоматически
                port = GetAvailablePort();
            }

            // Проверяем корректность порта
            if (port < 1 || port > 65535)
            {
                port = GetAvailablePort();
            }

            // Получаем IP адрес
            string _localIp;
            if (string.IsNullOrWhiteSpace(ipText) || ipText == "000.000.0.0")
            {
                _localIp = GetLocalIPAddress();
            }
            else
            {
                _localIp = ipText;
                // Можно добавить валидацию IP адреса
                // if (!IsValidIP(ipText)) { ... }
            }

            // Сохраняем данные
            IpServer = _localIp;
            PortServer = port.ToString();
            NameServer = serverName;

            // Инициализируем таймер (если он еще не создан)
            try
            {
                if (ServerAktine == null)
                {
                    ServerAktine = new DispatcherTimer();
                    ServerAktine.Interval = TimeSpan.FromMilliseconds(1000);
                    ServerAktine.Tick += ServerAktines;
                }
                else
                {
                    // Если таймер уже запущен, останавливаем его
                    if (ServerAktine.IsEnabled)
                        ServerAktine.Stop();

                    ServerAktine.Tick -= ServerAktines;
                    ServerAktine.Tick += ServerAktines;
                }
            }
            catch (System.NullReferenceException nre)
            {
                MessageBox.Show("Ошибка инициализации таймера: " + nre.Message, "Ошибка создания сервера");
                return;
            }

            // Запускаем сервер в отдельном потоке, чтобы не блокировать UI
            Task.Run(() =>
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Parse(_localIp), port);
                    _listener.Start();
                    AktiveServer = true;

                    // Обновляем UI через Dispatcher (так как мы в другом потоке)
                    Dispatcher.Invoke(() =>
                    {
                        StatusInfoUserServer.Text = $"{_localIp}:{port}";
                        StatusUserServer.Text = $"Сервер: Активен";

                        // Запускаем таймер только после успешного запуска сервера
                        ServerAktine.Start();
                    });

                    // Начинаем асинхронно принимать подключения
                    AcceptClientsAsync();
                }
                catch (FormatException fex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Неверный формат IP адреса: {fex.Message}", "Ошибка запуска сервера");
                        GridServerAktive.Visibility = Visibility.Collapsed;
                        GridServerCreate.Visibility = Visibility.Visible;
                    });
                }
                catch (SocketException sex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Сетевая ошибка: {sex.Message}\nКод ошибки: {sex.ErrorCode}", "Ошибка запуска сервера", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                        GridServerAktive.Visibility = Visibility.Collapsed;
                        GridServerCreate.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Ошибка запуска сервера: {ex.Message}", "Ошибка запуска сервера",MessageBoxButton.OKCancel,MessageBoxImage.Error);
                        GridServerAktive.Visibility = Visibility.Collapsed;
                        GridServerCreate.Visibility = Visibility.Visible;
                    });
                }
            });
        }

        // Метод для получения доступного порта
        private int GetAvailablePort()
        {
            // Простая реализация - можно улучшить
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        // Метод для получения локального IP адреса
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Не удалось получить локальный IP адрес");
        }

        // Асинхронный метод для принятия клиентов
        private async void AcceptClientsAsync()
        {
            while (AktiveServer && _listener != null)
            {
                if (IsFirewallEnabled())
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        /// Обработать подключение клиента
                        ProcessClient(client);
                    }
                    catch (ObjectDisposedException)
                    {
                        MessageBox.Show("Работа сверера была прервана", "Сервер был остоновлен",MessageBoxButton.OK,MessageBoxImage.Error);
                        break;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Сервер был остоновлен",MessageBoxButton.OK,MessageBoxImage.Error);
                        // Логировать ошибку, но продолжать принимать подключения
                        Debug.WriteLine($"Ошибка при принятии подключения: {ex.Message}");
                    }
                }
            }
        }

        // Метод обработки клиента
        private void ProcessClient(TcpClient client)
        {
            // Реализация обработки клиента
            // Например, запуск отдельного потока для каждого клиента
            Task.Run(() =>
            {
                try
                {
                    using (client)
                    using (var stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        // Обработка клиента
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка обработки клиента: {ex.Message}");
                }
            });
        }
        #endregion

        #region Клиент
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
                        if (message == "/d")
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
        #endregion

        #region Оброботка комнад сервера

        static void ProcessServerCommand(string command, string serverName)
        {
            Console.WriteLine($"\n📨 Команда от {serverName}: {command}");
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
        
        private void StartServer(object sender, RoutedEventArgs e)
        {
            if (UserCreateServerPassword.Password != UserCreateServerPasswordCheck.Password) { MessageBox.Show("Пороли не совподают!","неверный пароль",MessageBoxButton.OK,MessageBoxImage.Error); return; }

            if (UserCreateServerPassword.Password.Length < 1) { if (UserCreateServerPassword.Password != UserCreateServerPasswordCheck.Password) { MessageBox.Show("Пороли не совподают!", "неверный пароль", MessageBoxButton.OK, MessageBoxImage.Error); return; } }

            ServerAktive();

        }

        private void NavigatorBtn(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            if(button.Name == "BtnOpenGridCreateServer")
            {
                BtnOpenGridCreateServerNavigator.Visibility = Visibility.Collapsed;
                GridServerCreate.Visibility = Visibility.Visible;
            }
            else if(button.Name == "BtnOpenGridConectServer") 
            {
                BtnOpenGridCreateServerNavigator.Visibility = Visibility.Collapsed;
                GridServerConect.Visibility = Visibility.Visible;

            }
        }

        private void ConectServer(object sender, RoutedEventArgs e)
        {
            AcceptClients();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Razrabotka_Prog_APIandSDK.Windows.Notifications
{

    public class Notifications
    {
        #region Windows API Imports

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadImage(IntPtr hInst, string file, uint type,
                                             int width, int height, uint load);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        #endregion

        #region Constants

        // Операции с иконкой
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;

        // Флаги для NOTIFYICONDATA
        private const uint NIF_INFO = 0x00000010;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_REALTIME = 0x00000040;
        private const uint NIF_SHOWTIP = 0x00000080;

        // Типы уведомлений
        private const uint NIIF_NONE = 0x00000000;
        private const uint NIIF_INFO = 0x00000001;
        private const uint NIIF_WARNING = 0x00000002;
        private const uint NIIF_ERROR = 0x00000003;
        private const uint NIIF_USER = 0x00000004;
        private const uint NIIF_NOSOUND = 0x00000010;
        private const uint NIIF_LARGE_ICON = 0x00000020;

        // Константы для загрузки изображений
        private const uint IMAGE_ICON = 1;
        private const uint LR_LOADFROMFILE = 0x0010;
        private const uint LR_DEFAULTSIZE = 0x0040;

        #endregion

        #region Structures

        /// <summary>
        /// Структура данных для работы с иконкой в tree и уведомлениями
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;              // Размер структуры
            public IntPtr hWnd;              // Окно-владелец
            public uint uID;                 // ID иконки
            public uint uFlags;              // Флаги используемых полей
            public uint uCallbackMessage;    // Callback сообщение
            public IntPtr hIcon;             // Handle иконки
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;             // Текст всплывающей подсказки
            public uint dwState;             // Состояние иконки
            public uint dwStateMask;         // Маска состояния
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;            // Текст сообщения уведомления
            public uint uTimeoutOrVersion;   // Время показа в миллисекундах
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;       // Заголовок уведомления
            public uint dwInfoFlags;         // Тип уведомления и флаги
            public Guid guidItem;            // GUID для идентификации
            public IntPtr hBalloonIcon;      // Дополнительная иконка
        }

        #endregion

        #region Private Fields

        private NOTIFYICONDATA data;
        private List<Action> buttonCallbacks = new List<Action>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Время отображения уведомления в секундах
        /// </summary>
        public int Seconds { get; set; } = 4;

        /// <summary>
        /// Путь к пользовательской иконке
        /// </summary>
        public string UserIconPath { get; set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Загружает пользовательскую иконку из файла
        /// </summary>
        /// <param name="iconPath">Путь к файлу иконки</param>
        /// <returns>Handle загруженной иконки или IntPtr.Zero при ошибке</returns>
        private IntPtr LoadCustomIcon(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
            {
                Console.WriteLine("❌ Путь к иконке не указан");
                return IntPtr.Zero;
            }

            if (!File.Exists(iconPath))
            {
                Console.WriteLine($"❌ Файл иконки не существует: {iconPath}");
                return IntPtr.Zero;
            }

            try
            {
                IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0,
                                       LR_LOADFROMFILE | LR_DEFAULTSIZE);

                if (hIcon == IntPtr.Zero)
                {
                    Console.WriteLine($"❌ Не удалось загрузить иконку: {iconPath}");
                }
                else
                {
                    Console.WriteLine($"✅ Иконка загружена: {iconPath}");
                }

                return hIcon;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки иконки: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        /// <summary>
        /// Настраивает параметры уведомления в зависимости от типа
        /// </summary>
        private void ConfigureNotificationType(NotificationType type, ref NOTIFYICONDATA data, ref IntPtr hIcon)
        {
            switch (type)
            {
                case NotificationType.Info:
                    data.dwInfoFlags = NIIF_INFO;
                    break;

                case NotificationType.Error:
                    data.dwInfoFlags = NIIF_ERROR;
                    break;

                case NotificationType.Warning:
                    data.dwInfoFlags = NIIF_WARNING;
                    break;

                case NotificationType.Mute:
                    data.dwInfoFlags = NIIF_INFO | NIIF_NOSOUND; // Информация без звука
                    break;

                case NotificationType.User:
                    if (!string.IsNullOrEmpty(UserIconPath))
                    {
                        hIcon = LoadCustomIcon(UserIconPath);
                        if (hIcon != IntPtr.Zero)
                        {
                            data.dwInfoFlags = NIIF_USER;
                            data.hIcon = hIcon;
                            data.uFlags |= NIF_ICON;
                        }
                        else
                        {
                            data.dwInfoFlags = NIIF_INFO;
                        }
                    }
                    else
                    {
                        data.dwInfoFlags = NIIF_INFO;
                    }
                    break;

                case NotificationType.None:
                    data.dwInfoFlags = NIIF_NONE;
                    break;

                default:
                    data.dwInfoFlags = NIIF_INFO;
                    break;
            }
        }

        void NotificationsStandart(string message, string title, NotificationType type)
        {
            IntPtr hIcon = IntPtr.Zero;

            try
            {
                // Инициализация структуры данных
                data = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = IntPtr.Zero,
                    uID = 1,
                    uFlags = NIF_INFO,
                    szInfo = message,
                    szInfoTitle = title,
                    uTimeoutOrVersion = (uint)(Seconds * 1000)
                };

                // Настройка типа уведомления
                ConfigureNotificationType(type, ref data, ref hIcon);

                // Показ уведомления
                Shell_NotifyIcon(NIM_ADD, ref data);
                Shell_NotifyIcon(NIM_DELETE, ref data);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[🔔 {title}: {message}]\n❌ Ошибка: {ex.Message}");
                Console.WriteLine($"\t{ex.StackTrace}, {ex.InnerException}, {ex.Data}");

            }
            finally
            {
                // Освобождение ресурсов иконки
                if (hIcon != IntPtr.Zero)
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        #endregion

        #region Public Methods  

        /// <summary>
        /// Добавляет текстовую кнопку в уведомление
        /// </summary>
        /// <param name="buttonText">Текст кнопки</param>
        public void AddButton(string buttonText)
        {
            if (string.IsNullOrEmpty(data.szInfo))
            {
                data.szInfo = $"[{buttonText}]";

            }
            else
            {
                data.szInfo = $"{data.szInfo}   [{buttonText}]";
            }
        }

        /// <summary>
        /// Добавляет кнопку с обработчиком нажатия (заглушка)
        /// </summary>
        public void AddButton(string text, Action onClick)
        {
            Console.WriteLine("⚠️  Интерактивные кнопки не поддерживаются в текущей реализации или может работать не стадильно");
            buttonCallbacks.Add(onClick);
        }

        /// <summary>
        /// Показывает уведомление с указанным типом
        /// </summary>
        /// <param name="title">Заголовок уведомления</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="type">Тип уведомления</param>
        public void Show(string message, string title, NotificationType type)
        {
            NotificationsStandart(message, title, type);
        }

        /// <summary>
        /// Показывает стандартное информационное уведомление
        /// </summary>
        /// <param name="title">Заголовок уведомления</param>
        /// <param name="message">Текст сообщения</param>
        public void Show(string message, string title)
        {
            NotificationsStandart(message, title, NotificationType.None);
        }
        /// <summary>
        /// Показывает стандартное информационное уведомление
        /// </summary>
        /// <param name="title">Заголовок уведомления</param>
        /// <param name="message">Текст сообщения</param>
        public void Show(string message)
        {
            NotificationsStandart(message, null, NotificationType.None);
        }
        #endregion
    }

    /// <summary>
    /// Типы уведомлений
    /// </summary>
    public enum NotificationType
    {
        /// <summary>📝 Информационное сообщение</summary>
        Info,
        /// <summary>❌ Сообщение об ошибке</summary>
        Error,
        /// <summary>⚠️ Предупреждение</summary>
        Warning,
        /// <summary>🔇 Уведомление без звука</summary>
        Mute,
        /// <summary>👤 С пользовательской иконкой</summary>
        User,
        /// <summary>❓ Без иконки</summary>
        None
    }


}
using System.Diagnostics;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using Windows.Media.Playback;
using Windows.UI.Notifications.Management;

namespace read_notification
{
    internal static class Program
    {
        static HttpClient client = new HttpClient();
        static SoundPlayer player;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Visible = true;
            ProcessStartInfo psi = new ProcessStartInfo 
            {
                FileName = "c:\\Users\\keich\\AppData\\Local\\Programs\\VOICEVOX\\vv-engine\\run.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            Process? engine = Process.Start(psi);
            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                engine?.Kill();
                engine?.Dispose();
            };
            await ListenNotifications();
            Application.Run();
        }
        static async Task ListenNotifications()
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            var listener = UserNotificationListener.Current;
                        try
            {
                UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();
                if (accessStatus == UserNotificationListenerAccessStatus.Allowed)
                {
                    listener.NotificationChanged += async (sender, args) =>
                    {
                        if (args.ChangeKind == 0)
                        {
                            uint notificationId = args.UserNotificationId;
                            List<string> notificationTexts = listener.GetNotification(notificationId).Notification.Visual.Bindings[0].GetTextElements().Select(te => te.Text).ToList();
                            foreach (string text in notificationTexts)
                            {
                                Debug.WriteLine($" - {text}");
                            }
                            Stream audio = await GetAudio(notificationTexts);
                            PlayAudio(audio);
                        }

                    };
                }
            }catch(Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }
        static async Task<Stream> GetAudio(List<String> texts)
        {
            //var query = await client.GetFromJsonAsync<JsonElement>("http://localhost:50021/audio_query?text=" + string.Join("%20", texts)+"+speaker=3");
            var query = await client.PostAsync("http://localhost:50021/audio_query?speaker=3&text=" + string.Join("%20", texts),new StringContent(string.Empty));
            var responce = await client.PostAsync("http://localhost:50021/synthesis?speaker=1",new StringContent(query.Content.ReadAsStringAsync().Result,Encoding.UTF8,"application/json"));
            Stream audio = await responce.Content.ReadAsStreamAsync();
            return audio;
        }
        static void PlayAudio(Stream audio)
        {
            player = new SoundPlayer(audio);
            player.Play();
        }
}
}
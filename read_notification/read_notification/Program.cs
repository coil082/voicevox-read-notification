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
                Debug.WriteLine("Application is exiting...");
                engine?.Kill(entireProcessTree: true);
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
                            List<string> texts = notificationTexts.Select((te) => { 
                                if(te.Length <= 100) return te;
                                return te.Substring(0, 100) + "以下略"; 
                            }).ToList();
                            foreach (string text in texts)
                            {
                                Debug.WriteLine($" - {text}");
                            }
                            Stream audio = await GetAudio(texts);
                            PlayAudio(audio);
                        }

                    };
                }
            } catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }
        static async Task<Stream> GetAudio(List<String> texts)
        {
            try
            {
                var query = await client.PostAsync("http://localhost:50021/audio_query?speaker=3&text=" + string.Join("%20", texts), new StringContent(string.Empty));
                string queryJson = await query.Content.ReadAsStringAsync();
                var responce = await client.PostAsync("http://localhost:50021/synthesis?speaker=3", new StringContent(queryJson, Encoding.UTF8, "application/json"));
                Stream audio = await responce.Content.ReadAsStreamAsync();
                return audio;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"1 Error: {ex.Message}");
                return Stream.Null;
            }
        }
        static void PlayAudio(Stream audio)
        {
            if (audio == Stream.Null)
            {
                Debug.WriteLine("Audio stream is null. Cannot play audio.");
                return;
            }
            player = new SoundPlayer(audio);
            player.Play();
        }
    }
}
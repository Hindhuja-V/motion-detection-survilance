using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MotionDetectionSurvilance.Web
{
    public class NetworkManager
    {
        const string PORT = "8081";

        private const uint BufferSize = 8192;

        public event EventHandler<Settings> UpdateSettings;

        private StreamSocketListener listener;

        public async void Start()
        {
            try
            {
                listener = new StreamSocketListener();

                //listener.ConnectionReceived += async (sender, args) => { await OnConnection(sender, args); };
                listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
                listener.ConnectionReceived += OnConnection;

                await listener.BindServiceNameAsync(PORT);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private async void OnConnection(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var request = new StringBuilder();



            using (var input = args.Socket.InputStream)
            {
                var data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                var dataRead = BufferSize;

                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(
                         buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(
                                                  data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            Uri uri = GetQuery(request);

            if (uri.LocalPath.ToLower() == "/sub")
            {
                subscribeNotification(uri);
            }

            string query = uri.Query;
            ProcessQuery(query);

            using (var output = args.Socket.OutputStream)
            {
                using (var response = output.AsStreamForWrite())
                {

                    var outputText = await SendOutput(uri.LocalPath);

                    var html = Encoding.UTF8.GetBytes(outputText);
                    using (var bodyStream = new MemoryStream(html))
                    {
                        var header = $"HTTP/1.1 200 OK\r\nContent-Length: {bodyStream.Length}\r\nAccess-Control-Allow-Origin:*\r\nConnection: close\r\n\r\n";
                        var headerArray = Encoding.UTF8.GetBytes(header);
                        await response.WriteAsync(headerArray,
                                                  0, headerArray.Length);

                        await bodyStream.CopyToAsync(response);
                        await response.FlushAsync();
                    }
                }
            }
        }

        private async void subscribeNotification(Uri uri)
        {
            const string key = "subscription.txt";
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            var formattedText = FormatQuery(uri.Query);

            var data = new SubscribeNotificationData() { endpoint = formattedText["endpoint"], auth = formattedText["auth"], p256dh = formattedText["p256dh"] };
            List<SubscribeNotificationData> list;
            try
            {
                string oldSubs = await FileIO.ReadTextAsync(await localFolder.GetFileAsync(key));
                list = JsonConvert.DeserializeObject<List<SubscribeNotificationData>>(oldSubs);
            }
            catch (Exception)
            {

                list = new List<SubscribeNotificationData>();
            }
            list.Add(data);

            var file = await localFolder.CreateFileAsync(key, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(list));
        }

        private async Task<string> SendOutput(string localPath)
        {
            if (localPath.ToLower() == "/image")
            {
                return await SendImage();
            }

            return "<!DOCTYPE html><html><head> <meta charset='utf-8'> <meta http-equiv='X-UA-Compatible' content='IE=edge'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> <title>Motion Detection Surveillance</title> <link rel='stylesheet' href='https://fonts.googleapis.com/icon?family=Material+Icons'> <link rel='stylesheet' href='https://code.getmdl.io/1.2.1/material.indigo-pink.min.css'> <script defer src='https://code.getmdl.io/1.2.1/material.min.js'></script> <link rel='stylesheet' href='https://ankur198.github.io/MotionDetectionSurvilance/styles/index.css'> <link rel='stylesheet' href='https://ankur198.github.io/MotionDetectionSurvilance/styles/style.css'> <link rel='stylesheet' href='styles/style.css'></head><body> <header> <h1>Motion Detection Surveillance</h1> </header> <main> <p class='is-invisible'> <button disabled class='js-push-btn mdl-button mdl-js-button mdl-button--raised mdl-js-ripple-effect'> Enable Notification </button> </p><section class='subscription-details js-subscription-details is-invisible'> <p></p><pre><code class='js-subscription-json'></code></pre> </section> </main> <section class='myContent'> <div class='controls'> <input type='button' value='Subscribe' class='mdl-button mdl-js-button mdl-button--raised mdl-js-ripple-effect' onclick='SubToGithub()'> <form onsubmit='return false'> <div> <label class='mdl-switch mdl-js-switch mdl-js-ripple-effect' for='notificationEnable'> <input type='checkbox' id='notificationEnable' class='mdl-switch__input' onchange='let val=checked?1:0; sendData(\"/?NotificationEnable=\"+ val)'> <span class='mdl-switch__label'>Enable</span> </label> </div><div> <label for='NotificationAt'> Notification At <input class='mdl-slider mdl-js-slider' type='range' id='NotificationAt' min='100' max='1000' value='100' onchange='sendData(\"/?NotificationAt=\"+value)' tabindex='0'> </label> </div><div> <label for='Noise'>Noise</label> <input type='range' name='Noise' min='0' max='200' id='Noise' onchange='sendData(\"/?Noise=\"+value)' class='mdl-slider mdl-js-slider'> </div><div> <label for='Multiplier'>Multiplier</label> <input type='range' name='Multiplier' min='100' max='5000' id='Multiplier' onchange='sendData(\"/?Multiplier=\"+value)' class='mdl-slider mdl-js-slider'> </div></form> <div id='buttons'> <input type='button' value='Start Image' onclick='startPrev()' class='mdl-button mdl-js-button mdl-button--raised mdl-js-ripple-effect'> <input type='button' value='Stop Image' onclick='stopPrev()' class='mdl-button mdl-js-button mdl-button--raised mdl-js-ripple-effect'> <div> <label>Ip</label> <input type='text' id='ip' onchange='url=value' value='http://192.168.1.104:8081' class='mdl-textfield__input'> </div><p id='status'>Preview Stopped</p></div></div><div class='image'><img src='' id='img' alt=''></div></section> <script src='https://ankur198.github.io/MotionDetectionSurvilance/scripts/main.js'></script> <script src='https://code.getmdl.io/1.2.1/material.min.js'></script> <script>setIpFromHost(); </script></body></html>";
        }

        public static async Task<string> SendImage()
        {
            try
            {
                if (MainPage.oldImg == null)
                {
                    return "";
                }

                var stream = new InMemoryRandomAccessStream();

                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                encoder.SetSoftwareBitmap(MainPage.oldImg);
                await encoder.FlushAsync();

                var ms = new MemoryStream();
                stream.AsStream().CopyTo(ms);
                var tdata = ms.ToArray();

                var x = Convert.ToBase64String(tdata);

                ms.Dispose();
                stream.Dispose();
                return x;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return "";
            }
        }


        private void ProcessQuery(string query)
        {
            var fquery = FormatQuery(query);

            string[] possibleKeys = Enum.GetNames(typeof(SettingName));

            foreach (var possibleKey in possibleKeys)
            {
                if (fquery.Keys.Contains(possibleKey))
                {
                    int value;

                    if (int.TryParse(fquery[possibleKey], out value))
                    {
                        var s = new Settings() { SettingName = (SettingName)Enum.Parse(typeof(SettingName), possibleKey), Value = value };

                        UpdateSettings?.Invoke(this, s);
                    }
                }
            }
        }

        private static IDictionary<string, string> FormatQuery(string query)
        {

            query = query.TrimStart('?');
            query = query.Replace("&&", "&");
            var individualQueries = query.Split("&");

            var formattedQueries = new Dictionary<string, string>();

            foreach (var individualQuery in individualQueries)
            {
                if (individualQuery.Length == 0)
                {
                    continue;
                }

                var y = individualQuery.Split("=");
                formattedQueries.Add(y[0], y[1]);
            }

            return formattedQueries;
        }

        private static Uri GetQuery(StringBuilder request)
        {
            var requestLines = request.ToString().Split(' ');

            var url = requestLines.Length > 1
                              ? requestLines[1] : string.Empty;

            var uri = new Uri("http://localhost" + url);
            return uri;
        }
    }
}

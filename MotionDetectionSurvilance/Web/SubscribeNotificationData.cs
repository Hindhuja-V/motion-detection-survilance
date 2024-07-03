using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebPush;
using Windows.Storage;

namespace MotionDetectionSurvilance.Web
{
    class SubscribeNotificationData
    {
        public string endpoint { get; set; }
        public string p256dh { get; set; }
        public string auth { get; set; }

        public void SendNotification()
        {
            var pushEndpoint = endpoint;
            var p256dh = this.p256dh;
            var auth = this.auth;

            var subject = @"mailto:ankur.nigam198@gmail.com";
            const string publicKey = @"BEu09qCcFIreSF2qnR2W8pAKcFAn6wpJVFaKKx0BICpxevmLyGnrxxZFNOV0rJOyZifkgdxIxjhtNsYWREPJBNg";
            const string privateKey = @"NbLMH1eHsktglOLgiBLsD2L1eklzY1vrtlHWliAV0SU";

            var subscription = new PushSubscription(pushEndpoint, p256dh, auth);
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);
            //var gcmAPIKey = @"[your key here]";

            var webPushClient = new WebPushClient();
            try
            {
                //webPushClient.SendNotification(subscription);
                //var payload = new { message = "Haww koi hila", image = await NetworkManager.SendImage() };
                webPushClient.SendNotification(subscription, " koi hila", vapidDetails);
                //webPushClient.SendNotification(subscription, "payload", gcmAPIKey);
            }
            catch (WebPushException exception)
            {
                Debug.WriteLine("Http STATUS code" + exception.StatusCode);
            }
            catch (Exception)
            {
                Debug.WriteLine("Notification failed");
            }
        }

        public static void sendNotificationToAll()
        {
            Task t = new Task(async () =>
            {
                const string key = "subscription.txt";
                StorageFolder localSettings = ApplicationData.Current.LocalFolder;

                string rawData;

                try
                {
                    rawData = await FileIO.ReadTextAsync(await localSettings.GetFileAsync(key));
                }
                catch (Exception)
                {
                    return;
                }
                var listSubs = JsonConvert.DeserializeObject<List<SubscribeNotificationData>>(rawData);

                var tf = new TaskFactory();
                foreach (var item in listSubs)
                {
                    await tf.StartNew(() => item.SendNotification());
                }
            });
            t.Start();
        }
    }
}

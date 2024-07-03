using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MotionDetectionSurvilance.Web
{
    class EmailData
    {
        const string keyEmails = "subEmails";
        static string image = "";
        static string html = "";
        public static EmailData[] EmailList { get => getEmailList(); set => UpdateEmailList(value); }
        public static async void SendEmailToAll()
        {
            if (EmailList == null)
            {
                return;
            }
            image = await NetworkManager.SendImage();

            var attachment = new SendGrid.Helpers.Mail.Attachment();
            attachment.Content = image;
            attachment.ContentId = "MyImage";
            attachment.Filename = "image.jpg";

            html = "<h1>Found Some movement</h1><br><hr><img src='cid:MyImage'/>";

            foreach (var email in EmailList)
            {
                email.SendEmailSendGrid(attachment, html);
            }
        }


        public EmailData(string EmailTo)
        {
            this.EmailTo = EmailTo;
        }
        public string EmailTo { get; private set; }

        private async void SendEmailSendGrid(SendGrid.Helpers.Mail.Attachment attachment, string htmlContent)
        {
            var apiKey = new Windows.ApplicationModel.Resources.ResourceLoader("ApiKey").GetString("SendGrid");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(new Windows.ApplicationModel.Resources.ResourceLoader("ApiKey").GetString("FromEmail"), "Ankur Nigam");
            var subject = "Motion Detected!";
            var to = new EmailAddress(this.EmailTo);
            var plainTextContent = "Found Movement";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            msg.AddAttachment(attachment);
            try
            {
                var response = await client.SendEmailAsync(msg);
                Debug.WriteLine($"Email Status: {response.StatusCode}");
            }
            catch (Exception)
            {
                Debug.WriteLine($"Email Status: Failed");
            }
        }

        private static EmailData[] getEmailList()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var raw = localSettings.Values[keyEmails];

            if (raw == null)
            {
                return new EmailData[0];
            }
            return JsonConvert.DeserializeObject<EmailData[]>(raw as string);
        }

        private static void UpdateEmailList(EmailData[] emailDatas)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[keyEmails] = JsonConvert.SerializeObject(emailDatas);
        }
    }
}

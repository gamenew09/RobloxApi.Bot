using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxApi.Bot
{
    public enum EMessagesPage
    {
        Inbox = 0,
        Sent = 1,
        Archive = 3
    }

    public class PrivateMessage
    {

        public static explicit operator PrivateMessage(int messageId)
        {
            PrivateMessage msg = new PrivateMessage(null);
            msg._ID = messageId;
            return msg;
        }

        public static explicit operator int(PrivateMessage message)
        {
            return message.ID;
        }

        private BotUser _BotUser;

        internal PrivateMessage(BotUser recip)
        {
            _BotUser = recip;
        }

        public SendPrivateMessage CreateReply()
        {
            if (_BotUser == null || IsRecipientOurBot) // Make sure that the message is actually for us.
                return null; // Should we throw an exception instead?
            SendPrivateMessage pm = new SendPrivateMessage(_BotUser);
            pm.ReplyMessage = this;
            pm.IncludePreviousMessage = true;
            return pm;
        }

        internal int _ID;
        internal User _Recipient, _Sender;
        internal bool _IsRead, _IsReportAbuseDisplayed, _IsSystemMessage;
        internal DateTime _Created, _Updated;
        internal string _Body, _Subject;

        internal static PrivateMessage FromJObject(JObject obj, BotUser user)
        {
            PrivateMessage msg = new PrivateMessage(user);
            msg._ID = obj.Value<int?>("Id") ?? 0;

            msg._Sender = new User(obj["Sender"]["UserId"].Value<int>(), obj["Sender"]["UserName"].Value<string>());
            msg._Recipient = new User(obj["Recipient"]["UserId"].Value<int>(), obj["Recipient"]["UserName"].Value<string>());

            msg._Body = obj.Value<string>("Body");
            msg._Subject = obj.Value<string>("Subject");

            msg._IsRead = obj.Value<bool?>("IsRead") ?? false;
            msg._IsReportAbuseDisplayed = obj.Value<bool?>("IsReportAbuseDisplayed") ?? false;
            msg._IsSystemMessage = obj.Value<bool?>("IsSystemMessage") ?? false;

            string createdDT = obj.Value<string>("Created");
            string updatedDT = obj.Value<string>("Updated");
            

            
            //Console.WriteLine(info.DateTimeFormat.AbbreviatedMonthNames[0]);
            msg._Created = GetDateTimeFromString(createdDT);
            msg._Updated = GetDateTimeFromString(updatedDT);
            return msg;
        }

        private static DateTime GetDateTimeFromString(string dateTime)
        {
            return DateTime.Parse(dateTime.Replace("|", ""), CultureInfo.GetCultureInfo("en-US"));
        }

        /// <summary>
        /// The bot user that this message was sent to.
        /// </summary>
        public User Recipient
        {
            get { return _Recipient; }
        }

        public User Sender
        {
            get { return _Sender; }
        }

        /// <summary>
        /// Is the bot in the <see cref="_BotUser"/> the recipient?
        /// </summary>
        public bool IsRecipientOurBot
        {
            get { return _Recipient.ID != _BotUser.CurrentUser.ID; }
        }

        /// <summary>
        /// Is the bot in the <see cref="_BotUser"/> the sender?
        /// </summary>
        public bool IsSenderOurBot
        {
            get { return _Sender.ID != _BotUser.CurrentUser.ID; }
        }

        /// <summary>
        /// Is the Report Abuse link displayed for this message?
        /// </summary>
        public bool IsReportAbuseDisplayed
        {
            get { return _IsReportAbuseDisplayed; }
        }

        /// <summary>
        /// Is the message from the ROBLOX system?
        /// </summary>
        public bool IsSystemMessage
        {
            get { return _IsSystemMessage; }
        }

        /// <summary>
        /// The ID of the PrivateMessage.
        /// </summary>
        public int ID
        {
            get { return _ID; }
        }

        /// <summary>
        /// Did the bot user read this message?
        /// </summary>
        public bool IsRead
        {
            get { return _IsRead; }
        }

        public DateTime Created
        {
            get { return _Created; }
        }

        public DateTime Updated
        {
            get { return _Updated; }
        }

    }

    /// <summary>
    /// A private message object that is meant for sending a pm to someone.
    /// </summary>
    public class SendPrivateMessage
    {

        private BotUser _Sender;

        internal SendPrivateMessage(BotUser sender)
        {
            _Sender = sender;
        }

        public PrivateMessage ReplyMessage
        {
            get;
            set;
        }

        public bool IncludePreviousMessage
        {
            get;
            set;
        }

        public string Body
        {
            get;
            set;
        }

        public string Subject
        {
            get;
            set;
        }

        public User Recipient
        {
            get;
            set;
        }

        /// <summary>
        /// Sends the message to the <see cref="Recipient"/>
        /// </summary>
        /// <returns>Did the message send?</returns>
        public async Task<bool> Send() // We may be able to get the PrivateMessage object when we send the message.
        {
            await _Sender.userData.GrabCSRFToken(); // Get a new CSRF token for the next web request.
            if (ReplyMessage != null) // There are two different apis, we may be able to use https://www.roblox.com/messages/api/send-message, but for now we are doing it as follows.
            {
                HttpWebRequest request = _Sender.userData.CreateWebRequest("https://www.roblox.com/messages/api/send-message");
                request.Method = "POST";

                JObject obj = new JObject();
                obj["body"] = Body;
                obj["subject"] = Subject;
                obj["includePreviousMessage"] = IncludePreviousMessage;
                obj["recipientId"] = Recipient.ID;
                obj["replyMessageId"] = ReplyMessage.ID;

                byte[] dataBytes = Encoding.UTF8.GetBytes(obj.ToString());

                request.ContentType = "application/json;charset=UTF-8";
                request.ContentLength = dataBytes.Length;

                using (Stream stream = request.GetRequestStream())
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);

                HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                obj = JObject.Parse(data);

                return obj.Value<bool?>("success") ?? false;
            }
            else
            {
                HttpWebRequest request = _Sender.userData.CreateWebRequest("https://www.roblox.com/messages/api/send-message");
                request.Method = "POST";

                byte[] dataBytes = Encoding.UTF8.GetBytes(
                    string.Format("subject={0}&body={1}&recipientid={2}&cacheBuster={3}",
                        Uri.EscapeDataString(Subject),
                        Uri.EscapeDataString(Body),
                        Recipient.ID,
                        ToUnixTime(DateTime.Now))
                );

                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = dataBytes.Length;

                using (Stream stream = request.GetRequestStream())
                    await stream.WriteAsync(dataBytes, 0, dataBytes.Length);

                HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                JObject obj = JObject.Parse(data);

                return obj.Value<bool?>("success") ?? false;
            }
        }

        private static long ToUnixTime(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date - epoch).TotalSeconds);
        }

    }
}

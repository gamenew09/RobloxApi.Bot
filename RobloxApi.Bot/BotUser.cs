using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RobloxApi;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxApi.Bot
{

    public class BotUser : IDisposable
    {

        internal BotUserData userData;

        /// <summary>
        /// Constructor for BotUser specifying username to login as.
        /// </summary>
        /// <param name="userName">The user to login as.</param>
        public BotUser(string userName)
        {
            Username = userName;
        }

        private User _CurrentUser;

        /// <summary>
        /// The user that this bot is logged in as.
        /// </summary>
        public User CurrentUser
        {
            get { return _CurrentUser; }
        }

        /// <summary>
        /// The Friends of the currently logged in user.
        /// </summary>
        public FriendList Friends
        {
            get { return _CurrentUser.FriendList; }
        }

        private int _Robux;

        /// <summary>
        /// The amount of robux that is on this account.
        /// </summary>
        public int Robux
        {
            get { return _Robux; }
        }

        /// <summary>
        /// Gets UnreadMessageCount and FriendRequestCount
        /// </summary>
        /// <returns></returns>
        public async Task<UserIncomingItems> GetIncomingItems()
        {
            try
            {
                string data = await GetStringFromRequest("https://api.roblox.com/incoming-items/counts", true);

                return JsonConvert.DeserializeObject<UserIncomingItems>(data);
            }
            catch { return null; }
        }

        /// <summary>
        /// Sets the Robux property with the correct amount of Robux.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateRobux()
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/currency/balance");

            try
            {
                HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    data = await reader.ReadToEndAsync();
                }

                JObject obj = JObject.Parse(data);

                _Robux = obj.Value<int?>("robux") ?? 0;
                return true;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public SendPrivateMessage CreatePrivateMessage()
        {
            return new SendPrivateMessage(this);
        }

        private async Task<User[]> GetFollowerPage(int page)
        {
            JArray data = await userData.ParseJArrayFromURLResponse(string.Format("https://api.roblox.com/users/followers?userId={0}&page={1}", 
                _CurrentUser.ID, page));
            List<User> users = new List<User>();
            foreach(JToken token in data)
            {
                //User user = new User(token["Id"].Value<int?>() ?? -1);
                //user._Username = token["Username"].Value<string>();

                users.Add(await User.FromID(token["Id"].Value<int?>() ?? 0));
            }
            return users.ToArray(); // Does this work
        }

        public async Task<User[]> GetFollowers()
        {
            List<User> users = new List<User>();
            try // There could be a better way, but this will do for now.
            {
                int page = 1;
                while (page <= 10000) // 500000k follower max per user. You can change this if you want.
                {
                    User[] pg = await GetFollowerPage(page);
                    if (pg.Length == 0)
                        break;
                    for (int i = 0; i < pg.Length; i++)
                        users.Add(pg[i]);
                    page++;
                }
            }
            catch(Exception ex) { Console.WriteLine(ex); }
            return users.ToArray();
        }

        // http://api.roblox.com/docs#Friends

        async Task<string> GetStringFromRequest(HttpWebRequest req, bool throwException = false)
        {
            try
            {
                WebResponse resp = await req.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return data;
            }
            catch (WebException ex)
            {
                if (throwException)
                    throw ex;

                string data;
                using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return data;
            }
        }

        async Task<JToken> GetJTokenFromRequest(HttpWebRequest req, bool throwException = false)
        {
            try
            {
                WebResponse resp = await req.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return JToken.Parse(data);
            }
            catch (WebException ex)
            {
                if (throwException)
                    throw ex;

                WebResponse resp = ex.Response;

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return JToken.Parse(data);
            }
        }

        async Task<string> GetStringFromRequest(string url, bool throwException = false)
        {
            HttpWebRequest req = userData.CreateWebRequest(url);

            try
            {
                WebResponse resp = await req.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return data;
            }
            catch (WebException ex)
            {
                if (throwException)
                    throw ex;

                string data;
                using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return data;
            }
        }

        async Task<JToken> GetJTokenFromRequest(string url, bool throwException = false)
        {
            HttpWebRequest req = userData.CreateWebRequest(url);

            try
            {
                WebResponse resp = await req.GetResponseAsync();

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return JToken.Parse(data);
            }
            catch(WebException ex)
            {
                if (throwException)
                    throw ex;

                WebResponse resp = ex.Response;

                string data;
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                return JToken.Parse(data);
            }
        }

        /// <summary>
        /// Accept a friend request from a user.
        /// </summary>
        /// <param name="requester">The user who requested the friendship.</param>
        /// <returns>Did accepting succeed?</returns>
        public async Task<bool> AcceptFriendRequest(User requester)
        {
            //return ((JObject)await GetJTokenFromRequest(string.Format("https://api.roblox.com/user/accept-friend-request?requesterUserId={0}", requester.ID)))
            //    .Value<bool?>("success") == true;

            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/accept-friend-request");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("requesterUserId={0}", requester.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Decline a friend request from a user.
        /// </summary>
        /// <param name="requester">The user who requested the friendship.</param>
        /// <returns>Did declining succeed?</returns>
        public async Task<bool> DeclineFriendRequest(User requester)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/decline-friend-request");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("requesterUserId={0}", requester.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Request a friendship with a User.
        /// </summary>
        /// <param name="requester">The user to friend.</param>
        /// <returns>Did requesting succeed?</returns>
        public async Task<bool> RequestFriendship(User recipient)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/request-friendship");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("recipientUserId={0}", recipient.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Unfriends an user.
        /// </summary>
        /// <param name="friend">The user to unfriend.</param>
        /// <returns>Did the unfriending succeed?</returns>
        public async Task<bool> Unfriend(User friend)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/unfriend");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("friendUserId={0}", friend.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Is the bot following the user?
        /// </summary>
        /// <param name="user">The user to check if we are following.</param>
        /// <returns>Are we following the user?</returns>
        public async Task<bool> IsFollowing(User user)
        {
            try
            {
                return ((JObject)await GetJTokenFromRequest(string.Format("https://api.roblox.com/user/following-exists?userId={0}&followerUserId={1}", user.ID, CurrentUser.ID), true))
                    .Value<bool?>("isFollowing") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Follow an user.
        /// </summary>
        /// <param name="followUser">The user to follow.</param>
        /// <returns>Did the follow succeed?</returns>
        public async Task<bool> Follow(User followUser)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/follow");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("followedUserId={0}", followUser.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Unfollow an user.
        /// </summary>
        /// <param name="followUser">The user to unfollow.</param>
        /// <returns>Did the unfollow succeed?</returns>
        public async Task<bool> Unfollow(User followUser)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/user/unfollow");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("followedUserId={0}", followUser.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;
            try
            {
                return ((JObject)await GetJTokenFromRequest(request, true))
                .Value<bool?>("success") == true;
            }
            catch { return false; }
        }

        private string _Username;
        private bool _LoggedIn;

        /// <summary>
        /// Is this bot logged in?
        /// </summary>
        public bool LoggedIn
        {
            get { return _LoggedIn; }
        }

        /// <summary>
        /// The username to login under
        /// </summary>
        public string Username
        {
            get
            {
                if (_LoggedIn)
                    return CurrentUser.Username;
                return _Username;
            }
            set
            {
                if (!_LoggedIn)
                    _Username = value;
                else
                    throw new InvalidOperationException("You cannot set Username while logged in.");
            }
        }

        [Obsolete("Always returns false since 403 is given on request.")]
        public async Task<bool> AwardBadgeTo(User user, Asset badge, Asset place)
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/assets/award-badge");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("userId={0}&badgeId={1}&placeId={2}", user.ID, badge.ID, place.ID));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(formBytes, 0, formBytes.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                string owner = "";

                if (badge.UserCreator != null)
                    owner = badge.UserCreator.Username;
                else if (badge.GroupCreator != null)
                    owner = badge.GroupCreator.Name;
                else
                    return false;

                if(data.Contains(string.Format("{0} won {1}'s \"{2}\" award!", user.Username, owner, badge.Name)))
                {
                    return true;
                }
            }
            catch(WebException ex)
            {
                string data;
                using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();
            }

            return false;
        }

        async Task GetUserLoggedIn()
        {
            HttpWebRequest request = userData.CreateWebRequest("https://www.roblox.com/Home");
            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);

                HtmlNode node = doc.GetElementbyId("nav-profile");
                string url = node.GetAttributeValue("href", "");

                // https://www.roblox.com/users/85131845/profile
                // 29

                string id = url.Substring(29);
                id = id.Substring(0, id.IndexOf('/'));

                _CurrentUser = await User.FromID(int.Parse(id));

                _LoggedIn = true;
            }
            catch { }
        }

        /// <summary>
        /// Logs in as the value of <see cref="Username"/> with the password provided.
        /// 
        /// </summary>
        /// <param name="password">The password to login with.</param>
        /// <returns>The result of the login.</returns>
        public async Task<ELoginResponse> Login(string password)
        {
            userData = new Bot.BotUserData();
            userData.CookieContainer = new CookieContainer();
            userData.LastCSRFToken = await userData.GrabCSRFToken(); // Do we need this
            

            // 302 Found: Successful Login?
            // We should also have a .ROBLOSECURITY cookie.

            HttpWebRequest request = userData.CreateWebRequest("https://www.roblox.com/newlogin");
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("username={0}&password={1}&IdentificationCode=", Uri.EscapeDataString(Username), Uri.EscapeDataString(password)));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;

            request.AllowAutoRedirect = false;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(formBytes, 0, formBytes.Length);
            postStream.Flush();
            postStream.Close();

            string data = "";

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                Cookie[] cookie = new Cookie[response.Cookies.Count];
                response.Cookies.CopyTo(cookie, 0);
                if (cookie.Where((x) => x.Name == ".ROBLOSECURITY").Count() > 0)
                {
                    // We are logged in set the CurrentUser property!

                    await GetUserLoggedIn(); // Make sure we get the user currently logged in.

                    return ELoginResponse.Success;
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;

                /*
                if (response.StatusCode == HttpStatusCode.Forbidden) // Generic ROBLOX login error, get the error type then return.
                {
                    string data;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        data = await reader.ReadToEndAsync();
                    }
                    Console.WriteLine(data);
                    JObject jobj = JObject.Parse(data);

                    switch (jobj.Value<string>("message"))
                    {
                        case "Captcha":
                            return ELoginResponse.Captcha;
                        case "Credentials":
                            return ELoginResponse.InvalidCredentials;
                        case "Privileged":
                            return ELoginResponse.PrivilegedUser;
                        case "TwoStepVerification":
                            return ELoginResponse.TwoStepVerification;
                        case "PasswordResetRequired":
                            return ELoginResponse.PasswordResetRequired;
                        case "TooManyAttempts":
                            return ELoginResponse.TooManyAttempts;
                    }
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest) // Bad Request will default to InvalideCredentials, could always use a different enumeration.
                {
                    return ELoginResponse.InvalidCredentials;
                }
                */

                if (response.StatusCode == HttpStatusCode.NotFound) // Did the endpoint 404? Assume ROBLOX disabled the endpoint.
                {
                    return ELoginResponse.EndpointDisabled;
                }
            }

            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(data);

                HtmlNode validationErrors = doc.GetElementbyId("loginForm").ChildNodes.Where((n) => n.GetAttributeValue("class", null) == "validation-summary-errors").First().FirstChild;

                foreach (HtmlNode node in validationErrors.ChildNodes)
                {
                    string text = node.InnerText;

                    // TODO: Figure out error text for Captchas and Two-Step Verification.

                    if (text == "Your username or password is incorrect. Please check them and try again.")
                    {
                        return ELoginResponse.InvalidCredentials;
                    }
                }
            }
            catch { }

            return ELoginResponse.ServerError; // It's just a server error.
        }

        /// <summary>
        /// Blocks the user from communicating with the bot.
        /// </summary>
        /// <param name="user">The user to block</param>
        /// <returns>Did blocking succced.</returns>
        public async Task<bool> Block(User user)
        {
            await userData.GrabCSRFToken();
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/userblock/block");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("userId={0}", Uri.EscapeDataString(user.ID.ToString())));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(formBytes, 0, formBytes.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                JObject json = JObject.Parse(data);
                return json["success"].ToObject<bool>();
            }
            catch { return false; }
        }

        /// <summary>
        /// Unblocks the user.
        /// </summary>
        /// <param name="user">The user to unblock</param>
        /// <returns>Did unblocking succeed?</returns>
        public async Task<bool> Unblock(User user)
        {
            await userData.GrabCSRFToken();
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/userblock/unblock");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("userId={0}", Uri.EscapeDataString(user.ID.ToString())));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(formBytes, 0, formBytes.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                JObject json = JObject.Parse(data);
                return json["success"].ToObject<bool>();
            }
            catch { return false; }
        }

        /*
        /// <summary>
        /// Logs in as the value of <see cref="Username"/> with the password provided.
        /// This should no longer be used. This function when used will cause C# not to compile.
        /// </summary>
        /// <param name="password">The password to login with.</param>
        /// <returns>The result of the login.</returns>
        [Obsolete("ROBLOX has disabled the endpoint that this method uses. Use Login, method will renamed soon.", true)]
        public async Task<ELoginResponse> LoginOld(string password)
        {
            userData = new Bot.BotUserData();
            userData.CookieContainer = new CookieContainer();
            userData.LastCSRFToken = "";

            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/login/v2");

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] formBytes = ascii.GetBytes(string.Format("username={0}&password={1}", Uri.EscapeDataString(Username), Uri.EscapeDataString(password)));

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = formBytes.Length;

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(formBytes, 0, formBytes.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                string data;
                using (var reader = new StreamReader(response.GetResponseStream()))
                    data = await reader.ReadToEndAsync();

                JObject obj = JObject.Parse(data);

                // We are logged in set the CurrentUser property!

                _CurrentUser = await User.FromID(obj.Value<int>("userId"));

                _LoggedIn = true;

                return ELoginResponse.Success;
            }
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;

                if (response.StatusCode == HttpStatusCode.Forbidden) // Generic ROBLOX login error, get the error type then return.
                {
                    string data;
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        data = await reader.ReadToEndAsync();
                    }
                    JObject jobj = JObject.Parse(data);

                    switch (jobj.Value<string>("message"))
                    {
                        case "Captcha":
                            return ELoginResponse.Captcha;
                        case "Credentials":
                            return ELoginResponse.InvalidCredentials;
                        case "Privileged":
                            return ELoginResponse.PrivilegedUser;
                        case "TwoStepVerification":
                            return ELoginResponse.TwoStepVerification;
                        case "PasswordResetRequired":
                            return ELoginResponse.PasswordResetRequired;
                        case "TooManyAttempts":
                            return ELoginResponse.TooManyAttempts;
                    }
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest) // Bad Request will default to InvalideCredentials, could always use a different enumeration.
                {
                    return ELoginResponse.InvalidCredentials;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound) // Did the endpoint 404? Assume ROBLOX disabled the endpoint.
                {
                    return ELoginResponse.EndpointDisabled;
                }
            }

            return ELoginResponse.ServerError; // It's just a server error.
        }
        */

        /// <summary>
        /// Logs the current user out.
        /// </summary>
        /// <returns>Did logout succed?</returns>
        public async Task<bool> Logout()
        {
            HttpWebRequest request = userData.CreateWebRequest("https://api.roblox.com/sign-out/v1");
            request.Method = "POST";

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                _LoggedIn = false;
                return true;
            }
            catch(WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;

                switch(response.StatusCode)
                {
                    case HttpStatusCode.Forbidden: // Csrf token failed, get a new one then logout.
                        userData.SetCSRFTokenFromResponse(response);
                        return await Logout();
                    default:
                        return false;
                }
            }
        }
        
        public void Dispose()
        {
            Logout().Wait(); // Make sure we logout, this function does block as its called.
        }
        
        public async Task<PrivateMessage[]> GetPagedMessages(int page, EMessagesPage messageTab = EMessagesPage.Inbox, int pageSize = 20)
        {
            JObject obj = (JObject)await userData.ParseJTokenFromURLResponse(string.Format("https://www.roblox.com/messages/api/get-messages?messageTab={0}&pageNumber={1}&pageSize={2}", (int)messageTab, page, pageSize));
            List<PrivateMessage> pms = new List<PrivateMessage>();
            foreach (JToken token in obj.Value<JArray>("Collection"))
                pms.Add(PrivateMessage.FromJObject((JObject)token, this));

            return pms.ToArray();
        }

    }

    public class UserIncomingItems
    {
        [JsonProperty("unreadMessageCount")]
        public int UnreadMessageCount;
        [JsonProperty("friendRequestsCount")]
        public int FriendRequestsCount;
    }

    internal class BotUserData
    {

        public string LastCSRFToken;

        public CookieContainer CookieContainer;

        /// <summary>
        /// Gets the CSRF/XSRF token for the next request.
        /// </summary>
        /// <returns>CSRF/XSRF token</returns>
        public async Task<string> GrabCSRFToken()
        {
            HttpWebRequest request = CreateWebRequest("https://www.roblox.com/home");

            HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

            string data;
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                data = await reader.ReadToEndAsync();

            string xsrfTokenStart = "Roblox.XsrfToken.setToken('";

            int startIndex = data.IndexOf(xsrfTokenStart) + xsrfTokenStart.Length;
            int endIndex = data.Substring(startIndex).IndexOf("');"); // Make sure we find the ending "');" after the startIndex, not before.

            LastCSRFToken = data.Substring(startIndex, endIndex);

            return LastCSRFToken; // We could always remove this but who knows.
        }

        public HttpWebRequest CreateWebRequest(string url)
        {
            HttpHelper.InitHttpHelper();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.CookieContainer = CookieContainer;
            request.UserAgent = "CSharp.RobloxAPI.Bot";

            if(LastCSRFToken != "")
            {
                // Let roblox know we have a csrf token.
                request.Headers.Set("X-Csrf-Token", LastCSRFToken);
            }

            return request;
        }

        public async Task<JToken> ParseJTokenFromURLResponse(string url)
        {
            HttpWebRequest request = CreateWebRequest(url);

            HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

            string data;
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                data = await reader.ReadToEndAsync();

            return JToken.Parse(data);
        }

        public async Task<JArray> ParseJArrayFromURLResponse(string url)
        {
            HttpWebRequest request = CreateWebRequest(url);

            HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync();

            string data;
            using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                data = await reader.ReadToEndAsync();

            return JArray.Parse(data);
        }

        public void SetCSRFTokenFromResponse(HttpWebResponse response)
        {
            LastCSRFToken = response.Headers["X-Csrf-Token"] ?? "";
        }

    }

    /// <summary>
    /// Response from <see cref="BotUser.Login(string)"/>.
    /// </summary>
    public enum ELoginResponse
    {
        /// <summary>
        /// Successful login.
        /// </summary>
        Success,
        /// <summary>
        /// The user logging in has to fill out a captcha!
        /// </summary>
        Captcha,
        /// <summary>
        /// The credentials entered are invalid.
        /// </summary>
        InvalidCredentials,
        /// <summary>
        /// The user being logged into is blocked from using the api.roblox.com/login/v1 endpoint.
        /// </summary>
        PrivilegedUser,
        /// <summary>
        /// Two Step Verification required over e-mail.
        /// </summary>
        TwoStepVerification,
        /// <summary>
        /// ROBLOX wants the user to reset their password before logging in.
        /// </summary>
        PasswordResetRequired,
        /// <summary>
        /// Too many failed atttempts to login, preventing the login attempts.
        /// </summary>
        TooManyAttempts,
        /// <summary>
        /// Endpoint is disabled by ROBLOX.
        /// </summary>
        EndpointDisabled,
        /// <summary>
        /// A generic server error occured.
        /// </summary>
        ServerError
    }

}

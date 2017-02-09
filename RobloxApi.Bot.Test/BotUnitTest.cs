using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobloxApi.Bot;
using System.Threading.Tasks;
using RobloxApi;
using System.Reflection;

namespace RobloxApi.Bot.Test
{
    [TestClass]
    public class BotUnitTest
    {
        public LoginFile GetLoginInformation()
        {
            return new LoginFile(@"H:\loginrbx.json");
        }

        [TestMethod]
        public void LoginAsUser()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                ELoginResponse response;

                User bUser = null;

                using (BotUser user = new BotUser(file.Username))
                {
                    response = await user.Login(file.Password);

                    if (response == ELoginResponse.Success)
                        bUser = user.CurrentUser;
                }

                Console.WriteLine("Response Result: {0} User: {1}", response, bUser);

                Assert.IsTrue(response == ELoginResponse.Success);
            }).Wait();
        }

        [TestMethod]
        public void SendPrivateMessageToUser()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                ELoginResponse response;

                User bUser = null;

                using (BotUser user = new BotUser(file.Username))
                {
                    response = await user.Login(file.Password);

                    if (response == ELoginResponse.Success)
                        bUser = user.CurrentUser;

                    Assert.IsTrue(response == ELoginResponse.Success);

                    SendPrivateMessage msg = user.CreatePrivateMessage();

                    msg.Recipient = (User)5762824; // We don't need to give all of the data for the user, just the ID.
                    msg.Body = "Test Message, using RobloxApi.Bot.";
                    msg.Subject = "Test Message - Title";

                    bool sendResult = await msg.Send();

                    Assert.IsTrue(sendResult);

                    Console.WriteLine("Message Send: {0}", sendResult);
                }
            }).Wait();
        }

        [TestMethod]
        public void ReplyToPrivateMessage()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                ELoginResponse response;

                User bUser = null;

                using (BotUser user = new BotUser(file.Username))
                {
                    response = await user.Login(file.Password);

                    if (response == ELoginResponse.Success)
                        bUser = user.CurrentUser;

                    Assert.IsTrue(response == ELoginResponse.Success);

                    SendPrivateMessage msg = user.CreatePrivateMessage();

                    msg.ReplyMessage = (PrivateMessage)1536131974; // Hardcoded for now, add a way to get sent, inbox, news, and archive messages.
                    msg.IncludePreviousMessage = true;

                    msg.Recipient = (User)5762824; // We don't need to give all of the data for the user, just the ID.
                    msg.Body = "Test Message, using RobloxApi.Bot.";
                    msg.Subject = "Test Message - Title";

                    bool sendResult = await msg.Send();

                    Assert.IsTrue(sendResult);

                    Console.WriteLine("Message Send: {0}", sendResult);
                }
            }).Wait();
        }

        [TestMethod]
        public void GetPrivateMessages()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                ELoginResponse response;

                User bUser = null;

                using (BotUser user = new BotUser(file.Username))
                {
                    response = await user.Login(file.Password);

                    if (response == ELoginResponse.Success)
                        bUser = user.CurrentUser;

                    Assert.IsTrue(response == ELoginResponse.Success);

                    PrivateMessage[] msgs = await user.GetPagedMessages(0);

                    foreach(PrivateMessage msg in msgs)
                    {
                        Console.WriteLine("----");
                        Type ty = typeof(PrivateMessage);
                        foreach (PropertyInfo info in ty.GetProperties())
                        {
                            Console.WriteLine("{0} = {1}", info.Name, info.GetGetMethod().Invoke(msg, new object[] { }));
                        }
                    }
                }
            }).Wait();
        }

        [TestMethod]
        public void BotIsFollowing()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                using (BotUser user = new BotUser(file.Username))
                {
                    ELoginResponse response = await user.Login(file.Password);

                    Assert.IsTrue(response == ELoginResponse.Success);

                    User followingUser = await User.FromID(5762824);

                    Assert.IsNotNull(followingUser);

                    bool isFollowing = await user.IsFollowing(followingUser);

                    Assert.IsTrue(isFollowing);
                }
            }).Wait();
        }

        [TestMethod]
        public void BotGetFollowers()
        {
            LoginFile file = GetLoginInformation();

            Task.Run(async () =>
            {
                using (BotUser user = new BotUser(file.Username))
                {
                    ELoginResponse response = await user.Login(file.Password);

                    Assert.IsTrue(response == ELoginResponse.Success);

                    User[] users = await user.GetFollowers();

                    Assert.IsNotNull(users);
                    Assert.IsTrue(users.Length > 0);

                    Console.WriteLine("Follower Count: {0}", users.Length);
                }
            }).Wait();
        }

        [TestMethod]
        public void BotGetIncomingItems()
        {
            Task.Run(async () =>
            {
                LoginFile file = GetLoginInformation();

                using (BotUser user = new BotUser(file.Username))
                {
                    ELoginResponse response = await user.Login(file.Password);

                    Assert.IsTrue(response == ELoginResponse.Success);

                    UserIncomingItems incomingItems = await user.GetIncomingItems();

                    Assert.IsNotNull(incomingItems);

                    Console.WriteLine("Incoming Friend Requests: {0} Unread Message Count: {1}", incomingItems.FriendRequestsCount, incomingItems.UnreadMessageCount);
                }
            }).Wait();
        }

        [TestMethod]
        public void BotGetRobux()
        {
            Task.Run(async () =>
            {
                LoginFile file = GetLoginInformation();

                using (BotUser user = new BotUser(file.Username))
                {
                    ELoginResponse response = await user.Login(file.Password);

                    Assert.IsTrue(response == ELoginResponse.Success);

                    Assert.IsTrue(await user.UpdateRobux());

                    Assert.IsTrue(user.Robux >= 0);

                    Console.WriteLine("Robux for user {0}: {1}", user.CurrentUser, user.Robux);
                }
            }).Wait();
        }
    }
}

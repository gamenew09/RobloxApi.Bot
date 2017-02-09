using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobloxApi.Bot;
using System.Threading.Tasks;
using RobloxApi;

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

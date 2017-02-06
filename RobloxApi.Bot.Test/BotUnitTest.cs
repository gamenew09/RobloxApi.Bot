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

            ELoginResponse response;

            User bUser = null;

            using (BotUser user = new BotUser(file.Username))
            {
                Task<ELoginResponse> responseTask = user.Login(file.Password);

                responseTask.Wait();

                response = responseTask.Result;

                if (response == ELoginResponse.Success)
                    bUser = user.CurrentUser;
            }

            Console.WriteLine("Response Result: {0} User: {1}", response, bUser);

            Assert.IsTrue(response == ELoginResponse.Success);
        }

        [TestMethod]
        public void BotIsFollowing()
        {
            LoginFile file = GetLoginInformation();

            using (BotUser user = new BotUser(file.Username))
            {
                Task<ELoginResponse> responseTask = user.Login(file.Password);

                responseTask.Wait();

                Assert.IsTrue(responseTask.Result == ELoginResponse.Success);

                Task<User> userTask = User.FromID(5762824);

                userTask.Wait();

                Assert.IsNotNull(userTask.Result);

                User followingUser = userTask.Result;

                Task<bool> isFollowingTask = user.IsFollowing(followingUser);

                isFollowingTask.Wait();

                Assert.IsTrue(isFollowingTask.Result);
            }
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

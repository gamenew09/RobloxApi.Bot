using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxApi.Bot.Test
{
    public static class TestConstants
    {

        /// <summary>
        /// <para>Path to a json file that similar to this:</para>
        /// {
	    ///     "Username": "username",
	    ///     "Password": "password"
        /// }
        /// 
        /// <para>The reason why you provide a file is to prevent an accidental leak of user info. I would strongly not recommend you put this file within the project folder.</para>
        /// </summary>
        public const string LoginCredentialsPath = "";

    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxApi.Bot.Test
{
    public class LoginFile
    {

        private string _Username;
        private string _Password;

        public string Username
        {
            get { return _Username; }
        }

        public string Password
        {
            get { return _Password; }
        }

        public LoginFile(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                JObject parsedObject = JObject.Parse(reader.ReadToEnd());

                _Username = parsedObject.Value<string>("Username");
                _Password = parsedObject.Value<string>("Password");
            }
        }

    }
}

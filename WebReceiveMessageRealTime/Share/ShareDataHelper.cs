using Ezy.Module.Facebook.Share;
using Ezy.Module.MSSQLRepository.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Ezy.Module.Facebook.Package.MSSQL;

namespace WebReceiveMessageRealTime.Share
{
    public static class ShareDataHelper
    {
        private static string sConnectionString = string.Empty;
        public static string GetConnectionStringFB()
        {
            if (sConnectionString == string.Empty)
            {
                sConnectionString = DataConnectionManager.GetSimpleConnectionString("Setting_Facebook.txt");
            }
            return sConnectionString;
        }
        private static GetFBConversationEngine engine;
        public static GetFBConversationEngine GetFBEngine()
        {
            if (engine == null)
            {
                try
                {
                    var fileContents = System.IO.File.ReadAllText(HttpContext.Current.Server.MapPath(@"~/App_Data/Setting_PageFacebook.txt"));
                    if (fileContents != null && fileContents != string.Empty)
                    {
                        var dictData = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContents);
                        var connectionString = GetConnectionStringFB();
                        engine = new GetFBConversationEngine()
                        {
                            access_token = dictData["access_token"],
                            connectionString = connectionString,
                            pageId = dictData["pageId"],
                            package = new PackageMSSQL(connectionString)
                        };
                        engine.StartEngine();
                    }
                }
                catch (Exception ex)
                {
#warning Cần lưu log
                }
            }
            return engine;
        }
    }
}
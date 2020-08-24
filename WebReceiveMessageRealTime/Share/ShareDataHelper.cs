using Ezy.Module.Facebook.Share;
using Ezy.Module.MSSQLRepository.Connection;
using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using Ezy.Module.Facebook.Package.MSSQL;
using Tesseract;
using System.IO;
using System.Web.Hosting;
using System.Drawing;
using Alliance.Auto.Bank.Client;
using System.Text;
using System.Linq;
using System.Globalization;

namespace WebReceiveMessageRealTime.Share
{
    public static class ShareDataHelper
    {
        private static string sConnectionString = string.Empty;
        private static string tessPath = HostingEnvironment.MapPath(@"~/tessdata");
        private static string baseStringPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "BaseString.txt");
        private static string[] baseStringList = File.ReadAllText(baseStringPath).Replace("\r", "").Replace(",", "").Split('\n');
        public static string GetConnectionStringFB()
        {
            if (sConnectionString == string.Empty)
            {
                sConnectionString = DataConnectionManager.GetSimpleConnectionString("Setting_Facebook.txt");
            }
            return sConnectionString;
        }
        public static System.Drawing.Image DownloadImageFromUrl(string imageUrl)
        {
            System.Drawing.Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                System.IO.Stream stream = webResponse.GetResponseStream();

                image = System.Drawing.Image.FromStream(stream);

                webResponse.Close();
            }
            catch (Exception ex)
            {
                return null;
            }

            return image;
        }
        private static string SignalDownloadExcel()
        {
            var sMessage = SocketClient_UDP.StartClient();
            return sMessage;
        }
        public static string CheckIsTransfer_Img(string Url, out string sMessage)
        {
            bool flag = false;
            sMessage = string.Empty;
            string result = string.Empty;
            try
            {
                var img = DownloadImageFromUrl(Url);
                Bitmap bmp = (Bitmap)img;
                Pix imgPix = PixConverter.ToPix(bmp);
                using (TesseractEngine Orc = new TesseractEngine(tessPath, "eng", EngineMode.Default))
                {
                    var res = Orc.Process(imgPix);
                    var text = StringNormalize(res.GetText());
                    result = text;
                    foreach (var sKey in baseStringList)
                    {
                        var key = StringNormalize(sKey);
                        if (text.Contains(key))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                if (flag == true) sMessage = SignalDownloadExcel();
            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
            }
            return result;
        }
        private static string RemoveDiacritics(string text)
        {
            return string.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }
        private static string StringNormalize(string s)
        {
            var result = string.Empty;
            s = RemoveDiacritics(s);
            s = s.ToLower();
            return s;
        }
        public static string CheckIsTransfer_Msg(string Msg)
        {
            var sMessage = string.Empty;
            bool flag = false;
            Msg = StringNormalize(Msg);
            foreach (var sKey in baseStringList)
            {
                var key = StringNormalize(sKey);
                if (Msg.Contains(key))
                {
                    flag = true;
                    break;
                }
            }
            if (flag == true) sMessage = SignalDownloadExcel();
            return sMessage;
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
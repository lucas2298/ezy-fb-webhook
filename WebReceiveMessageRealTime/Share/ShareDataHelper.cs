using Ezy.Module.Facebook.Share;
using Ezy.Module.MSSQLRepository.Connection;
using System;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using Ezy.Module.Facebook.Package.MSSQL;
using System.IO;
using System.Web.Hosting;
using System.Drawing;
using Alliance.Auto.Bank.Client;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using Winsoft.Ocr;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using WebReceiveMessageRealTime.Data;
using WebReceiveMessageRealTime.Models;

namespace WebReceiveMessageRealTime.Share
{
    public static class ShareDataHelper
    {
        private static string baseStringPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "BaseString.txt");
        private static string[] baseStringList = File.ReadAllText(baseStringPath).Replace("\r", "").Replace(",", "").Split('\n');

        public static Image DownloadImageFromUrl(string imageUrl)
        {
            Image image = null;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 10000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                Stream stream = webResponse.GetResponseStream();

                image = Image.FromStream(stream);

                webResponse.Dispose();
            }
            catch
            {
            }

            return image;
        }
        private static string SignalDownloadExcel()
        {
            var sMessage = SocketClient_UDP.StartClient();
            return sMessage;
        }
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        private static bool GetTextInImage(Ocr ocr, out string imageText)
        {
            bool flag = false;
            imageText = string.Empty;
            if (!ocr.Active)
            {
                ocr.DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinsoftOcr");
                ocr.Active = true;
            }
            imageText = StringNormalize(ocr.Text);
            foreach (var sKey in baseStringList)
            {
                var key = StringNormalize(sKey);
                if (imageText.Contains(key))
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }
        private static string Join2StringNoDup(string string_1, string string_2)
        {
            List<string> listString = new List<string>(string_1.Split(' '));
            var _listString = string_2.Split(' ').ToList();
            _listString.ForEach(c =>
            {
                if (!listString.Contains(c)) listString.Add(c);
            });
            string_1 = string.Join(" ", listString);
            string_1 = string_1.Trim();
            return string_1;
        }
        private static bool GetTextInImage(Ocr ocr_vie, Ocr ocr_eng, out string imageText)
        {
            bool flag = false;
            bool flagVie = false;
            bool flagEng = false;
            imageText = string.Empty;
            var imageTextVie = string.Empty;
            var imageTextEng = string.Empty;
            flagVie = GetTextInImage(ocr_vie, out imageTextVie);
            flagEng = GetTextInImage(ocr_eng, out imageTextEng);
            imageText = Join2StringNoDup(imageTextVie, imageTextEng);
            flag = (flagVie == true || flagEng == true);
            return flag;
        }
        public static bool CheckIsTransfer_Img(string Url, out string imageText, out string sMessage)
        {
            sMessage = string.Empty;
            bool flag = false;
            imageText = string.Empty;
            var imageTextZoom = string.Empty;
            #region Old code
            // Sử dụng tesseract ocr
            /*
            try
            {
                //var img = DownloadImageFromUrl(Url);
                var img = Image.FromFile(@"D:\WebReceiveMessageRealTime\WebReceiveMessageRealTime\Image\test.png");
                Bitmap bmp = (Bitmap)img;
                using (TesseractEngine Orc = new TesseractEngine(tessPath, "vie"))
                {
                    string res = string.Empty;
                    using (var page = Orc.Process(bmp, PageSegMode.AutoOnly))
                        res = page.GetText();
                    var text = StringNormalize(res);
                    imageText = text;
                    foreach (var sKey in baseStringList)
                    {
                        var key = StringNormalize(sKey);
                        if (text.Contains(key))
                        {
                            flag = true;
                            break;
                        }
                    }
                    imageText = imageText.Replace("\n", " ").Replace("\r", " ");
                    RegexOptions options = RegexOptions.None;
                    Regex regex = new Regex("[ ]{2,}", options);
                    imageText = regex.Replace(imageText, " ");
                }
                if (flag == true)
                {
                    sMessage = SignalDownloadExcel();
                }
            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
            }
            */
            #endregion
            // Sử dụng winsoft ocr
            try
            {
                var img = DownloadImageFromUrl(Url);
                if (img != null)
                {
                    var imgZoom = ResizeImage(img, img.Width + 400, img.Height + 100);
                    // First setting
                    var ocr_vie = new Ocr()
                    {
                        DataPath = null,
                        Language = Winsoft.Ocr.Language.Vietnamese,
                        LanguageCode = "vie",
                        PdfFileName = "",
                        PdfTitle = "",
                        PictureFileName = null,
                    };
                    var ocr_eng = new Ocr()
                    {
                        DataPath = null,
                        Language = Winsoft.Ocr.Language.English,
                        LanguageCode = "eng",
                        PdfFileName = "",
                        PdfTitle = "",
                        PictureFileName = null,
                    };
                    // Normal img
                    ocr_vie.Picture = img;
                    ocr_eng.Picture = img;
                    flag = GetTextInImage(ocr_vie, ocr_eng, out imageText);
                    // Zoom img
                    ocr_vie.Picture = imgZoom;
                    ocr_eng.Picture = imgZoom;
                    if (!flag) flag = GetTextInImage(ocr_vie, ocr_eng, out imageTextZoom);
                    else GetTextInImage(ocr_vie, ocr_eng, out imageTextZoom);
                    imageText = Join2StringNoDup(imageText, imageTextZoom);
                    if (flag) SignalDownloadExcel();
                    img.Dispose();
                }
                else
                {
                    sMessage = "Can not download image";
                }
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    sMessage += ex.Message + "\n";
                    ex = ex.InnerException;
                }
            }
            return flag;
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
            s = s.Replace("\n", " ").Replace("\r", " ");
            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);
            s = regex.Replace(s, " ");
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
        public static void UpdateEngine(SettingModel setting)
        {
            var pageId = setting.PageId;
            var connectionString = DataConnectionManager.GetSimpleConnectionString(setting.Setting_Core);
            var db = new AllianceConnect(connectionString);
            var page = db.GetPageInfoByPageId(Convert.ToInt64(pageId));
            if (page != null)
            {
                engine.access_token = page.AccessToken;
                engine.connectionString = connectionString;
                engine.pageId = setting.PageId;
                engine.package = new PackageMSSQL(connectionString);
            }
        }
        public static GetFBConversationEngine GetFBEngine(SettingModel setting, out string sMessage)
        {
            sMessage = string.Empty;
            if (engine == null)
            {
                try
                {
                    var pageId = setting.PageId;
                    var connectionString = DataConnectionManager.GetSimpleConnectionString(setting.Setting_Core);
                    var db = new AllianceConnect(connectionString);
                    var page = db.GetPageInfoByPageId(Convert.ToInt64(pageId));
                    if (page != null)
                    {
                        engine = new GetFBConversationEngine()
                        {
                            access_token = page.AccessToken,
                            connectionString = connectionString,
                            pageId = page.PageId.ToString(),
                            package = new PackageMSSQL(connectionString)
                        };
                        engine.StartEngine();
                    }
                    else throw new Exception($"No Fanpage found with PageId {page.Id}");
                }
                catch (Exception ex)
                {
                    sMessage = ex.Message;
                }
            }
            return engine;
        }
    }
}
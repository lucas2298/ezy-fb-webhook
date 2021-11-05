using Ezy.Module.Facebook.Share;
using Ezy.Module.Library.Utilities;
using Ezy.Module.MSSQLRepository.Connection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using WebReceiveMessageRealTime.Data;
using WebReceiveMessageRealTime.Models;
using WebReceiveMessageRealTime.Share;

namespace WebReceiveMessageRealTime.Controllers
{
    #region Facebook
    [RoutePrefix("api/Facebook")]
    public class FacebookController : ApiController
    {
        private static string jsonDataFacebook { get; set; }

        #region Setting
        private SettingModel[] _Settings = null;
        private SettingModel[] Settings
        {
            get
            {
                if (_Settings == null)
                {
                    var sSetting = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "Facebook.json"));
                    _Settings = JsonConvert.DeserializeObject<SettingModel[]>(sSetting);
                }
                return _Settings;
            }
        }
        #endregion

        [HttpGet]
        public HttpResponseMessage Get()
        {
            string verify_token_real = "WebhookMessageEvent";
            var mode = System.Web.HttpContext.Current.Request.QueryString["hub.mode"];
            var verify_token = System.Web.HttpContext.Current.Request.QueryString["hub.verify_token"];
            var challenge = System.Web.HttpContext.Current.Request.QueryString["hub.challenge"];
            if (mode == "subscribe" && verify_token == verify_token_real)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(challenge)
                };
                return response;
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }
        }

        #region Engine
        private GetFBConversationEngine fbEngine
        {
            get
            {
                var result = ShareDataHelper.GetFBEngine(setting, out string sMessage);
                if (!string.IsNullOrEmpty(sMessage))
                {
                    var db = new AllianceConnect(connectionString_Core);
                    db.AddLog(new FB_Log()
                    {
                        Method = "fbEngine",
                        Message = sMessage,
                        Log_CreatedDate = DateTime.Now
                    });
                }
                return result;
            }
        }
        #endregion

        #region Setting
        private SettingModel setting = null;
        private string connectionString_Core
        {
            get
            {
                return DataConnectionManager.GetSimpleConnectionString(setting.Setting_Core);
            }
        }
        private string connectionString_Business
        {
            get
            {
                return DataConnectionManager.GetSimpleConnectionString(setting.Setting_Business);
            }
        }
        #endregion


        private static List<ImageDetailModel> listNewCus = new List<ImageDetailModel>();

        [HttpPost]
        public HttpResponseMessage Post(object data)
        {
            string sMessage = string.Empty;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            Task.Run(() =>
            {
                string dataString = string.Empty;
                try
                {
                    #region Configs
                    var saveDb = ConfigurationManager.AppSettings["FB_RealTimeSaveDB"];
                    var isPushToEngine = ConfigurationManager.AppSettings["PushCustomerToEngine"];
                    #endregion

                    if (data != null)
                        dataString = data.ToString();
                    jsonDataFacebook = dataString;
                    var jsonObject = JsonConvert.DeserializeObject<MessageReceiveModel>(dataString);
                    if (jsonObject != null && jsonObject.entry != null && jsonObject.entry.Count() > 0)
                    {
                        var dict_entry = DictionaryHelper.BuildDictionary(jsonObject.entry, c => c.id, true);

                        foreach (var pageId in dict_entry.Keys)
                        {
                            setting = Settings.FirstOrDefault(c => c.PageId == pageId);
                            var db_Core = new AllianceConnect(connectionString_Core);
                            // customers lưu tất cả các id khách hàng để gửi về lại cho engine
                            string customers = string.Empty;
                            var listImageDetail = new List<ImageDetailModel>();
                            #region Data Processing
                            var entrys = dict_entry[pageId];
                            foreach (var entry in entrys)
                            {
                                long time = Convert.ToInt64(entry.time / 1000);
                                DateTime dateTime = new DateTime(1970, 1, 1, 7, 0, 0, 0);
                                dateTime = dateTime.AddSeconds(time);
                                var message = entry.messaging[0].message.text;
                                if (message != string.Empty && message != null)
                                {
                                    var item = new FB_MessengerRealtime()
                                    {
                                        SenderId = Convert.ToInt64(entry.messaging[0].sender.id),
                                        RecipientId = Convert.ToInt64(entry.messaging[0].recipient.id),
                                        TimeSend = dateTime,
                                        Message = message
                                    };
                                    customers += item.SenderId.ToString() + ',';
                                    sMessage = ShareDataHelper.CheckIsTransfer_Msg(message);
                                    if (saveDb == "1")
                                    {
                                        db_Core.Add_FB_MessengerRealtime(item);
                                        db_Core.SaveChanges();
                                    }
                                }
                                if (entry.messaging[0].message.Attachments != null && entry.messaging[0].message.Attachments.Count > 0)
                                {
                                    var attachements = entry.messaging[0].message.Attachments.ToArray();
                                    var listItem = new List<FB_MessengerRealtime>();
                                    if (attachements != null && attachements.Length > 0)
                                    {
                                        foreach (var sItem in attachements)
                                        {
                                            if (sItem.type == "image" && sItem.payload.sticker_id == null)
                                            {
                                                var item = new FB_MessengerRealtime()
                                                {
                                                    SenderId = Convert.ToInt64(entry.messaging[0].sender.id),
                                                    RecipientId = Convert.ToInt64(entry.messaging[0].recipient.id),
                                                    TimeSend = dateTime,
                                                    ImageUrl = sItem.payload.url
                                                };
                                                listItem.Add(item);
                                                listImageDetail.Add(new ImageDetailModel()
                                                {
                                                    Url = item.ImageUrl,
                                                    TimeReceiveFromSource = item.TimeSend,
                                                    SenderId = item.SenderId.ToString()
                                                });
                                            }
                                        }
                                        if (listItem != null && listItem.Count > 0)
                                        {
                                            var senderId = listItem[0].SenderId.ToString();
                                            if (!customers.Contains(senderId))
                                                customers += senderId + ',';
                                            if (saveDb == "1")
                                            {
                                                db_Core.AddRange_FB_MessengerRealtime(listItem);
                                                db_Core.SaveChanges();
                                            }
                                        }
                                    }
                                }

                                //Vì dịch ảnh sang text nên sẽ lâu, gây ra hiện tượng timeout cho một request của facebook
                                //=> Facebook không nhận được tín hiệu 200 nên sẽ gửi tín hiệu liên tục
                                //=> Dùng task để chạy ngầm, đảm bảo không bị timeout khi fb request
                                #region Image Processing
                                List<FBConversationDetail_Image> listImageText = new List<FBConversationDetail_Image>();
                                var db_Business = new PosDataConnect(connectionString_Business);
                                // Nếu là KH mới thì sẽ phải lấy tin nhắn về, dẫn tới trường hợp không tìm được KH và mất luôn thông tin lần gửi này
                                if (listNewCus != null && listNewCus.Count > 0)
                                {
                                    listImageDetail.AddRange(listNewCus);
                                    listNewCus = new List<ImageDetailModel>();
                                }
                                foreach (var sItem in listImageDetail)
                                {
                                    string imageText = string.Empty;
                                    bool flag = ShareDataHelper.CheckIsTransfer_Img(sItem.Url, out imageText, out sMessage);
                                    if (string.IsNullOrEmpty(sMessage))
                                    {
                                        var senderId = Convert.ToInt64(sItem.SenderId);
                                        var customer = db_Core.sp_Fb_GetCusBySenderId_Run(senderId);
                                        if (customer != null)
                                        {
                                            var _item = new FBConversationDetail_Image()
                                            {
                                                ConversationId = customer.ConversationId,
                                                CustomerFbName = customer.CustomerName,
                                                LinkToChat = customer.LinkToChat,
                                                ImageText = imageText,
                                                IsBankTransfer = flag,
                                                Log_CreatedDate = DateTime.Now,
                                                TimeReceiveFromSource = sItem.TimeReceiveFromSource
                                            };
                                            var fbImage = db_Core.Add_FBConversationDetail_Image(_item);
                                            db_Core.SaveChanges();
                                            var supplier = db_Business.GetSupplier(_item.LinkToChat);
                                            if (supplier != null && supplier.Length > 0)
                                            {
                                                _item.CusMoneyNotTransferBefore = db_Business.GetCusMoneyNotTransferBefore(supplier, _item.TimeReceiveFromSource, fbImage.Id, db_Core);
                                                if (_item.CusMoneyNotTransferBefore > 0)
                                                {
                                                    if (!flag) _item.IsLikeBankTransfer = true;
                                                }
                                                db_Core.SaveChanges();
                                            }
                                        }
                                        else listNewCus.Add(sItem);
                                    }
                                    else db_Core.AddLog(new FB_Log()
                                    {
                                        Method = "POST - CheckIsTransfer_Img",
                                        Message = sMessage,
                                        Log_CreatedDate = DateTime.Now,
                                        Param = sItem.Url + "\n" + imageText
                                    });
                                }
                                if (isPushToEngine == "1" && customers != string.Empty && fbEngine != null)
                                {
                                    while (fbEngine.IsDoJobRunning)
                                    {
                                        Thread.Sleep(1000);
                                    }
                                    if (fbEngine.pageId != setting.PageId)
                                        ShareDataHelper.UpdateEngine(setting);
                                    fbEngine.PushCustomer(customers);
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                    else
                        throw new Exception("No data found");
                }
                catch (Exception ex)
                {
                    #region Log
                    sMessage = ex.Message;
                    while (ex.InnerException != null)
                    {
                        sMessage += " \n " + ex.InnerException.Message;
                        ex = ex.InnerException;
                    }
                    var db = new AllianceConnect(connectionString_Core);
                    db.AddLog(new FB_Log()
                    {
                        Method = "POST - FBSendData",
                        Message = sMessage,
                        Log_CreatedDate = DateTime.Now,
                        Param = dataString
                    });
                    #endregion
                }
            });
            return response;
        }

        #region Get lastest message user has sent
        [Route("GetDataString")]
        [HttpGet]
        public HttpResponseMessage GetDataString()
        {
            if (string.IsNullOrEmpty(jsonDataFacebook)) jsonDataFacebook = "Chưa có dữ liệu";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonDataFacebook)
            };
            return response;
        }
        #endregion

        #region Delete
        [Route("DeleteCustomer")]
        [HttpPost]
        public HttpResponseMessage DeleteCustomer(object data)
        {
            var jsonString = data.ToString();
            var sSetting = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "Fb_ImageConfig.json"));
            var dict_Setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(sSetting);
            var temp = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            var guid = Guid.NewGuid().ToString();
            var url = dict_Setting["DeleteCallBackUrl"] + $"/api/Facebook/GetDeletedCustomer/{guid}";

            var oJson = new
            {
                url = url,
                confirmation_code = guid
            };

            if (temp != null && temp.ContainsKey("user_id"))
            {
                var user_id = temp["user_id"] as string;
                url = dict_Setting["DeleteCallBackUrl"] + $"/api/Facebook/GetDeletedCustomer/{user_id}";
                oJson = new
                {
                    url = url,
                    confirmation_code = user_id
                };
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(oJson))
            };
            return response;
        }

        [Route("GetDeletedCustomer/{Id}")]
        [HttpPost]
        public HttpResponseMessage GetDeletedCustomer()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new { Message = "Your data has been deleted" }))
            };
            return response;
        }
        #endregion
    }
    #endregion

    #region Zalo
    [RoutePrefix("api/Zalo")]
    public class ZaloController : ApiController
    {
        public static string jsonStringZalo { get; set; }

        public static string connectionString
        {
            get { return string.Empty; }
        }

        [Route("Webhook")]
        [HttpPost]
        public HttpResponseMessage Post(object data)
        {
            var saveDb = ConfigurationManager.AppSettings["FB_RealTimeSaveDB"];
            var isPushToEngine = ConfigurationManager.AppSettings["PushCustomerToEngine"];
            var dataString = data.ToString();
            jsonStringZalo = dataString;
            string sMessage = string.Empty;
            var db = new AllianceConnect(connectionString);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {

            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
                while (ex.InnerException != null)
                {
                    sMessage += " \n " + ex.InnerException.Message;
                    ex = ex.InnerException;
                }
                db.AddLog(new FB_Log()
                {
                    Method = "POST - FBSendData",
                    Message = sMessage,
                    Log_CreatedDate = DateTime.Now
                });
            }
            return response;
        }
    }
    #endregion
}
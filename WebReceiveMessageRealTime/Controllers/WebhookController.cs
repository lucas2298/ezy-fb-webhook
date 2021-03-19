using Ezy.Module.Facebook.Share;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebReceiveMessageRealTime.Models;
using WebReceiveMessageRealTime.Share;

namespace WebReceiveMessageRealTime.Controllers
{
    [RoutePrefix("api/Webhook")]
    public class WebhookController : ApiController
    {
        public static string jsonString { get; set; }
        public static List<ImageDetailModel> listNewCus = new List<ImageDetailModel>();
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
        public static string connectionString
        {
            get { return ShareDataHelper.GetConnectionStringFB(); }
        }
        public static GetFBConversationEngine fbEngine
        {
            get { return ShareDataHelper.GetFBEngine(); }
        }
        public static string ajumaConnectionString
        {
            get { return ShareDataHelper.GetConnectionStringAjuma(); }
        }
        [HttpPost]
        public HttpResponseMessage Post(object data)
        {
            var SaveDb = ConfigurationManager.AppSettings["FB_RealTimeSaveDB"];
            var isPushToEngine = ConfigurationManager.AppSettings["PushCustomerToEngine"];
            var dataString = data.ToString();
            jsonString = dataString;
            string sMessage = string.Empty;
            var db = new DatabaseConnect(connectionString);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<MessageReceiveModel>(dataString);
                // customers lưu tất cả các id khách hàng để gửi về lại cho engine
                string customers = string.Empty;
                var listImageDetail = new List<ImageDetailModel>();
                foreach (var dataObjects in jsonObject.entry)
                {
                    long time = Convert.ToInt64(dataObjects.time / 1000);
                    DateTime dateTime = new DateTime(1970, 1, 1, 7, 0, 0, 0);
                    dateTime = dateTime.AddSeconds(time);
                    var message = dataObjects.messaging[0].message.text;
                    if (message != string.Empty && message != null)
                    {
                        var item = new FB_MessengerRealtime()
                        {
                            SenderId = Convert.ToInt64(dataObjects.messaging[0].sender.id),
                            RecipientId = Convert.ToInt64(dataObjects.messaging[0].recipient.id),
                            TimeSend = dateTime,
                            Message = message
                        };
                        customers += item.SenderId.ToString() + ',';
                        sMessage = ShareDataHelper.CheckIsTransfer_Msg(message);
                        if (SaveDb == "1")
                        {
                            db.Add_FB_MessengerRealtime(item);
                            db.SaveChanges();
                        }
                    }
                    var attachements = dataObjects.messaging[0].message.Attachments.ToArray();
                    if (attachements != null && attachements.Length > 0)
                    {
                        var listItem = new List<FB_MessengerRealtime>();
                        if (attachements != null && attachements.Length > 0)
                        {
                            foreach (var sItem in attachements)
                            {
                                if (sItem.type == "image" && sItem.payload.sticker_id == null)
                                {
                                    var item = new FB_MessengerRealtime()
                                    {
                                        SenderId = Convert.ToInt64(dataObjects.messaging[0].sender.id),
                                        RecipientId = Convert.ToInt64(dataObjects.messaging[0].recipient.id),
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
                                customers += listItem[0].SenderId.ToString() + ',';
                                if (SaveDb == "1")
                                {
                                    db.AddRange_FB_MessengerRealtime(listItem);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
                //Vì dịch ảnh sang text nên sẽ lâu, gây ra hiện tượng timeout cho một request của facebook
                //=> Facebook không nhận được tín hiệu 200 nên sẽ gửi tín hiệu liên tục
                //=> Dùng task để chạy ngầm, đảm bảo không bị timeout khi fb request
                Task.Run(() =>
                {
                    List<FBConversationDetail_Image> listImageText = new List<FBConversationDetail_Image>();
                    var dbAjuma = new AjumaDataConnect(ajumaConnectionString);
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
                            var customer = db.sp_Fb_GetCusBySenderId_Run(senderId);
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
                                var fbImage = db.Add_FBConversationDetail_Image(_item);
                                db.SaveChanges();
                                var supplier = dbAjuma.GetSupplier(_item.LinkToChat);
                                if (supplier != null && supplier.Length > 0)
                                {
                                    _item.CusMoneyNotTransferBefore = dbAjuma.GetCusMoneyNotTransferBefore(supplier, _item.TimeReceiveFromSource, fbImage.Id);
                                    if (_item.CusMoneyNotTransferBefore > 0)
                                    {
                                        if (!flag) _item.IsLikeBankTransfer = true;
                                    }
                                    db.SaveChanges();
                                }
                                //listImageText.Add(_item);
                            }
                            else listNewCus.Add(sItem);
                        }
                        else db.AddLog(new FB_Log()
                        {
                            Method = "POST - CheckIsTransfer_Img",
                            Message = sMessage + "\n" + sItem.Url + imageText,
                            Log_CreatedDate = DateTime.Now
                        });
                        //if (listImageText != null && listImageText.Count > 0)
                        //{
                        //    db.AddRange_FBConversationDetail_Image(listImageText);
                        //    db.SaveChanges();
                        //}
                    }
                });
                if (isPushToEngine == "1")
                    if (customers != string.Empty)
                        fbEngine.PushCustomer(customers);
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
        [Route("GetDataString")]
        [HttpGet]
        public HttpResponseMessage GetDataString()
        {
            if (string.IsNullOrEmpty(jsonString)) jsonString = "Chưa có dữ liệu";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonString)
            };
            return response;
        }
    }
}
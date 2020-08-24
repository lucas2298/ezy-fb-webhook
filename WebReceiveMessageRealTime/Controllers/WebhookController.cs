﻿using Ezy.Module.Facebook.Share;
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
        [HttpPost]
        public HttpResponseMessage Post(object data)
        {
            var SaveDb = ConfigurationManager.AppSettings["FB_RealTimeSaveDB"];
            var dataString = data.ToString();
            jsonString = dataString;
            string sMessage = string.Empty;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<MessageReceiveModel>(dataString);
                // customers lưu tất cả các id khách hàng để gửi về lại cho engine
                string customers = string.Empty;
                var db = new DatabaseConnect(connectionString);
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
                        db.Add_FB_MessengerRealtime(item);
                    }
                    else
                    {
                        var attachements = dataObjects.messaging[0].message.Attachments;
                        List<FBConversationDetail_Image> listImageText = new List<FBConversationDetail_Image>();
                        var listItem = new List<FB_MessengerRealtime>();
                        if (attachements != null && attachements.Count > 0)
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
                                    string imageText;
                                    bool flag = ShareDataHelper.CheckIsTransfer_Img(sItem.payload.url, out imageText, out sMessage);
                                    var customer = db.sp_FB_GetListConversationIds_Run(dataObjects.messaging[0].sender.id);
                                    if (customer != null)
                                    {
                                        var _item = new FBConversationDetail_Image()
                                        {
                                            ConversationId = customer.ConversationId,
                                            CustomerFbName = customer.CustomerName,
                                            LinkToChat = customer.LinkToChat,
                                            ImageText = imageText,
                                            IsBankTransfer = flag
                                        };
                                        listImageText.Add(_item);
                                    }
                                }
                            }
                            if (listImageText != null && listImageText.Count > 0)
                            {
                                db.AddRange_FBConversationDetail_Image(listImageText);
                                db.SaveChanges();
                            }
                            if (listItem != null && listItem.Count > 0)
                            {
                                db.AddRange_FB_MessengerRealtime(listItem);
                                customers += listItem[0].SenderId.ToString() + ',';
                            }
                        }
                    }
                    if (SaveDb == "1")
                    {
                        db.SaveChanges();
                    }
                }
                if (customers != string.Empty)
                    fbEngine.PushCustomer(customers);
            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
            }
            if (!string.IsNullOrEmpty(sMessage)) response.Content = new StringContent(sMessage);
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
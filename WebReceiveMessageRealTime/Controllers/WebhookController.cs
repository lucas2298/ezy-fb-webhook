using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WebReceiveMessageRealTime.Models;

namespace WebReceiveMessageRealTime.Controllers
{
    [RoutePrefix("api/v1/Webhook")]
    public class WebhookController : ApiController
    {
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
        [HttpPost]
        public HttpResponseMessage Post([FromBody] string Data)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Success")
            };
            return response;
        }
    }
}
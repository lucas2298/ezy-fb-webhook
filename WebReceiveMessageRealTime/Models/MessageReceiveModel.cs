using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebReceiveMessageRealTime.Models
{
    public class RecipientModel
    {
        public string id { get; set; }
    }
    public class Sender
    {
        public string id { get; set; }
    }
    public class MessageModel
    {
        public string text { get; set; }
        public List<Attachments> Attachments { get; set; }
    }
    public class MessagingModel
    {
        public Sender sender { get; set; }
        public RecipientModel recipient { get; set; }
        public MessageModel message { get; set; }
    }
    public class Payload
    {
        public string url { get; set; }
        public long? sticker_id { get; set; }
    }
    public class Attachments
    {
        public string type { get; set; }
        public Payload payload { get; set; }
    }
    public class Entry
    {
        public double time { get; set; }
        public List<MessagingModel> messaging { get; set; }
    }
    public class MessageReceiveModel
    {
        public List<Entry> entry { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebReceiveMessageRealTime.Models
{
    public class ImageDetailModel
    {
        public string Url { get; set; }
        public string SenderId { get; set; }
        public DateTime? TimeReceiveFromSource { get; set; }
    }
}
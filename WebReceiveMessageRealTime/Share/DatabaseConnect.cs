using Ezy.Module.MSSQLRepository.Connection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebReceiveMessageRealTime
{
    public partial class SOLIDDB_DEVEntities
    {
        public SOLIDDB_DEVEntities(string connectionString)
            : base(connectionString)
        {
        }
    }
}
namespace WebReceiveMessageRealTime.Share
{
    public class DatabaseConnect
    {
        public string stringMetadata = "FB_MessengerRealtime.csdl|res://*/FB_MessengerRealtime.ssdl|res://*/FB_MessengerRealtime.msl";
        private static string sConnect = string.Empty;
        public DatabaseConnect(string connectionString)
        {
            sConnect = DataConnectionManager.GetDataConnectionString_With_ConnectionString(connectionString, stringMetadata);
            db = new SOLIDDB_DEVEntities(sConnect);
        }
        public string GetsConnect()
        {
            return sConnect;
        }
        ~DatabaseConnect()
        {
            db.Dispose();
        }
        private SOLIDDB_DEVEntities db;
        public void Add_FB_MessengerRealtime(FB_MessengerRealtime item)
        {
            db.FB_MessengerRealtime.Add(item);
        }
        public void AddRange_FB_MessengerRealtime(List<FB_MessengerRealtime> listItem)
        {
            db.FB_MessengerRealtime.AddRange(listItem);
        }
        public sp_FB_GetListConversationIds_Result sp_FB_GetListConversationIds_Run(string customerId)
        {
            var data = db.sp_FB_GetListConversationIds(customerId).LastOrDefault();
            return data;
        }
        public void Add_FBConversationDetail_Image(FBConversationDetail_Image item)
        {
            db.FBConversationDetail_Image.Add(item);
        }
        public void AddRange_FBConversationDetail_Image(List<FBConversationDetail_Image> item)
        {
            db.FBConversationDetail_Image.AddRange(item);
        }
        public void SaveChanges()
        {
            db.SaveChanges();
        }
    }
}
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
    public class DatabaseConnect
    {
        public string stringMetadata = "FB_MessengerRealtime.csdl|res://*/FB_MessengerRealtime.ssdl|res://*/FB_MessengerRealtime.msl";
        public DatabaseConnect(string connectionString)
        {
            var sConnect = DataConnectionManager.GetDataConnectionString_With_ConnectionString(connectionString, stringMetadata);
            db = new SOLIDDB_DEVEntities(sConnect);
        }
        ~DatabaseConnect()
        {
            db.Dispose();
        }
        private SOLIDDB_DEVEntities db;
        public void Add(FB_MessengerRealtime item)
        {
            db.FB_MessengerRealtime.Add(item);
        }
        public void AddRange(List<FB_MessengerRealtime> listItem)
        {
            db.FB_MessengerRealtime.AddRange(listItem);
        }
        public void SaveChanges()
        {
            db.SaveChanges();
        }
    }
}
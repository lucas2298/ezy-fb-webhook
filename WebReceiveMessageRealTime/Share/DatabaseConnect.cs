using Ezy.Module.MSSQLRepository.Connection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web;
using WebReceiveMessageRealTime.Models;

namespace WebReceiveMessageRealTime
{
    public partial class SOLIDDB_DEVEntities
    {
        public SOLIDDB_DEVEntities(string connectionString)
            : base(connectionString)
        {
        }
    }
    public partial class Ajuma_devEntities
    {
        public Ajuma_devEntities(string connectionString) : base(connectionString)
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
    public class AjumaDataConnect
    {
        public string stringMetadata = "AjumaDb.csdl|res://*/AjumaDb.ssdl|res://*/AjumaDb.msl";
        private static string sConnect = string.Empty;
        public AjumaDataConnect(string connectionString)
        {
            sConnect = DataConnectionManager.GetDataConnectionString_With_ConnectionString(connectionString, stringMetadata);
            db = new Ajuma_devEntities(sConnect);
        }
        public string GetsConnect()
        {
            return sConnect;
        }
        ~AjumaDataConnect()
        {
            db.Dispose();
        }
        private Ajuma_devEntities db;
        /// <summary>
        /// Lấy khách hàng đã được mapping với facebook
        /// </summary>
        /// <param name="LinkToChat"></param>
        /// <returns></returns>
        public Supplier GetSupplier(string LinkToChat)
        {
            return db.Suppliers.ToArray().Where(c => c.FacebookURL == LinkToChat).LastOrDefault();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="supplierId"></param>
        /// <param name="TimeReceiveFromSource"></param>
        /// <returns></returns>
        public decimal? GetCusMoneyNotTransferBefore(string supplierId, DateTime? TimeReceiveFromSource)
        {
            decimal? CusMoneyNotTransferBefore = 0.0m;
            sp_GetBillTransfer_Json_Param param = new sp_GetBillTransfer_Json_Param()
            {
                SupplierId = supplierId,
                HasMapping = false
            };
            var result = new List<sp_GetBillTransfer_Json_Result>();
            var sParamJson = JsonConvert.SerializeObject(param);
            ObjectParameter output = new ObjectParameter("jsonOutput", "");
            db.sp_GetBillTransfer_Json(sParamJson, output);
            var jsonString = output.Value.ToString();
            if (!string.IsNullOrEmpty(jsonString))
            {
                result = JsonConvert.DeserializeObject<List<sp_GetBillTransfer_Json_Result>>(jsonString);
            }
            CusMoneyNotTransferBefore = result.Where(c => c.EntDate <= TimeReceiveFromSource).Sum(c => c.TotalMoney);
            return CusMoneyNotTransferBefore;
        }
    }
}
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
        public sp_Fb_GetCusBySenderId_Result sp_Fb_GetCusBySenderId_Run(long senderId)
        {
            var data = db.sp_Fb_GetCusBySenderId(senderId).FirstOrDefault();
            return data;
        }
        public FBConversationDetail_Image Add_FBConversationDetail_Image(FBConversationDetail_Image item)
        {
            return db.FBConversationDetail_Image.Add(item);
        }
        public void AddRange_FBConversationDetail_Image(List<FBConversationDetail_Image> item)
        {
            db.FBConversationDetail_Image.AddRange(item);
        }
        public void AddRange_FBConversationDetail_ImageMoment(FBConversationDetail_ImageBillNotPaidMoment[] item)
        {
            db.FBConversationDetail_ImageBillNotPaidMoment.AddRange(item);
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
        public long[] GetSupplier(string LinkToChat)
        {
            return db.Suppliers.ToArray().Where(c => c.FacebookURL == LinkToChat && c.IsDeleted == false).Select(c => c.Id).ToArray();
        }
        /// <summary>
        /// Hàm sẽ tính số tiền mà khách còn nợ.
        /// - Tính đến thời điểm khách gửi ảnh chuyển khoản
        /// </summary>
        /// <param name="supplierId"></param>
        /// <param name="TimeReceiveFromSource"></param>
        /// <returns></returns>
        public decimal? GetCusMoneyNotTransferBefore(long[] supplierId, DateTime? TimeReceiveFromSource, long fbImageId)
        {
            decimal? CusMoneyNotTransferBefore = 0.0m;
            try
            {
                sp_GetBillTransfer_Json_Param param = new sp_GetBillTransfer_Json_Param()
                {
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
                var imageMomentList = new List<FBConversationDetail_ImageBillNotPaidMoment>();
                var dbFB = new DatabaseConnect(ShareDataHelper.GetConnectionStringFB());
                foreach (var sId in supplierId)
                {
                    CusMoneyNotTransferBefore += result.Where(c => {
                        var flag = false;
                        if (c.EntDate <= TimeReceiveFromSource && c.SupplierId == sId)
                        {
                            flag = true;
                            imageMomentList.Add(new FBConversationDetail_ImageBillNotPaidMoment()
                            {
                                FBConversationDetail_ImageId = fbImageId,
                                BillId = c.BillId,
                            });
                        }
                        return flag;
                    }).Sum(c => c.TotalMoney);
                }
                if (imageMomentList != null && imageMomentList.Count() > 0)
                {
                    dbFB.AddRange_FBConversationDetail_ImageMoment(imageMomentList.ToArray());
                    dbFB.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                CusMoneyNotTransferBefore = -1;
            }
            return CusMoneyNotTransferBefore;
        }
    }
}
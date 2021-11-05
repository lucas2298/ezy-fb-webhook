using Ezy.Module.MSSQLRepository.Connection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Web;
using WebReceiveMessageRealTime.Models;
using WebReceiveMessageRealTime.Data;
using WebReceiveMessageRealTime.Share;

namespace WebReceiveMessageRealTime.Data
{
    public partial class AllianceEntities
    {
        public AllianceEntities(string connectionString) : base(connectionString)
        {
        }
    }
    public partial class PosEntities
    {
        public PosEntities(string connectionString) : base(connectionString)
        {
        }
    }
}
namespace WebReceiveMessageRealTime.Data
{
    public class AllianceConnect
    {
        public string stringMetadata = "Data.AllianceDataModel.csdl|res://*/Data.AllianceDataModel.ssdl|res://*/Data.AllianceDataModel.msl";
        private static string sConnect = string.Empty;
        public AllianceConnect(string connectionString)
        {
            sConnect = DataConnectionManager.GetDataConnectionString_With_ConnectionString(connectionString, stringMetadata);
            db = new AllianceEntities(sConnect);
        }
        public string GetsConnect()
        {
            return sConnect;
        }
        ~AllianceConnect()
        {
            db.Dispose();
        }
        private AllianceEntities db;

        #region Method
        public FB_PageInfo GetPageInfoByPageId(long pageId)
        {
            return db.FB_PageInfo.FirstOrDefault(c => c.PageId == pageId);
        }
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
        public void AddLog(FB_Log item)
        {
            db.FB_Log.Add(item);
            db.SaveChanges();
        }
        public void SaveChanges()
        {
            db.SaveChanges();
        }
        #endregion
    }

    public class PosDataConnect
    {
        public string stringMetadata = "Data.PosDataModel.csdl|res://*/Data.PosDataModel.ssdl|res://*/Data.PosDataModel.msl";
        private static string sConnect = string.Empty;
        public PosDataConnect(string connectionString)
        {
            sConnect = DataConnectionManager.GetDataConnectionString_With_ConnectionString(connectionString, stringMetadata);
            db_Business = new PosEntities(sConnect);
        }
        public string GetsConnect()
        {
            return sConnect;
        }
        ~PosDataConnect()
        {
            db_Business.Dispose();
        }
        private PosEntities db_Business;
        #region Method
        /// <summary>
        /// Lấy khách hàng đã được mapping với facebook
        /// </summary>
        /// <param name="LinkToChat"></param>
        /// <returns></returns>
        public long[] GetSupplier(string LinkToChat)
        {
            return db_Business.Suppliers.ToArray().Where(c => c.FacebookURL == LinkToChat && c.IsDeleted == false).Select(c => c.Id).ToArray();
        }
        /// <summary>
        /// Hàm sẽ tính số tiền mà khách còn nợ.
        /// - Tính đến thời điểm khách gửi ảnh chuyển khoản
        /// </summary>
        /// <param name="supplierId"></param>
        /// <param name="TimeReceiveFromSource"></param>
        /// <returns></returns>
        public decimal? GetCusMoneyNotTransferBefore(long[] supplierId, DateTime? TimeReceiveFromSource, long fbImageId, AllianceConnect db_Core)
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
                db_Business.sp_GetBillTransfer_Json(sParamJson, output);
                var jsonString = output.Value.ToString();
                if (!string.IsNullOrEmpty(jsonString))
                {
                    result = JsonConvert.DeserializeObject<List<sp_GetBillTransfer_Json_Result>>(jsonString);
                }
                var imageMomentList = new List<FBConversationDetail_ImageBillNotPaidMoment>();
                foreach (var sId in supplierId)
                {
                    CusMoneyNotTransferBefore += result.Where(c =>
                    {
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
                    db_Core.AddRange_FBConversationDetail_ImageMoment(imageMomentList.ToArray());
                    db_Core.SaveChanges();
                }
            }
            catch
            {
                CusMoneyNotTransferBefore = -1;
            }
            return CusMoneyNotTransferBefore;
        }
        #endregion
    }
}
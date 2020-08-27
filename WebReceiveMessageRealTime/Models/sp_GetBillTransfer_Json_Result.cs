using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebReceiveMessageRealTime.Models
{
    public class sp_GetBillTransfer_Json_Result
    {
        public long BillId { get; set; }
        public string IONumber { get; set; }
        public DateTime EntDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public decimal TotalMoney { get; set; }
        public long? PaymentTypeId { get; set; }
        public string Note { get; set; }
        public DateTime? LastTransferDate { get; set; }
        public bool StatusBill { get; set; }
        public bool IsTransferBank { get; set; }
        public long CounterId { get; set; }
        public string CounterName { get; set; }
        public string BillOptionJson { get; set; }
    }
}
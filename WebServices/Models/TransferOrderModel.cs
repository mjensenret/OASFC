using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebServices.Models
{
    public class TransferOrderArrivalModel
    {
        public int transferOrderArrivalId { get; set; }
        public object transferOrder { get; set; }
        //public int equipmentId { get; set; }
        //public decimal? scaleInQuantity { get; set; }
        public decimal? scaleInWeight { get; set; }
        //public decimal? scaleInVolume { get; set; }
        public string scaleInDate { get; set; }
        //public decimal? scaleOutQuantity { get; set; }
        //public decimal? scaleOutVolume { get; set; }
        public decimal? scaleOutWeight { get; set; }
        public string scaleOutDate { get; set; }
        //public decimal? netTransferQuantity { get; set; }
        //public decimal? netTransferVolume { get; set; }
        public decimal? netTransferWeight { get; set; }
        public decimal? grossTransferWeight { get; set; }
        public int sequenceNumber { get; set; }
        //public decimal? netTotalTransferQuantity { get; set; }
        //public decimal? netTotalTransferWeight { get; set; }
        //public decimal? netTotalTransferVolume { get; set; }
        //public object loadedEmptyStatus { get; set; }
        //public bool isVoided { get; set; }
        //public long? interfaceDate { get; set; }
        //public long? inputDate { get; set; }
        //public long? modifiedDate { get; set; }
        //public string inputBy { get; set; }
        //public string modifiedBy { get; set; }
    }

    //public class TransferOrderPlanningModel
    //{
    //    public int transferOrderPlanningId { get; set; }
    //    public int transferOrderId { get; set; }
    //    //public int equipmentTripId { get; set; }
    //    //public int productId { get; set; }
    //    //public int equipmentId { get; set; }
    //    //public decimal? planVolume { get; set; }
    //    ////public int planWeight { get; set; }
    //    ////public int availableWeight { get; set; }
    //    ////public double availableVolume { get; set; }
    //    ////public object arrivalWeight { get; set; }
    //    ////public object arrivalVolume { get; set; }
    //    ////public int sequenceNumber { get; set; }
    //    ////public string sealNumber { get; set; }
    //    //public bool isVoided { get; set; }
    //    //public bool isSelected { get; set; }
    //    ////public long inputDate { get; set; }
    //    //public long modifiedDate { get; set; }
    //    //public string inputBy { get; set; }
    //    //public string modifiedBy { get; set; }
    //}

    public class TransferOrderModel
    {
        public int transferOrderId { get; set; }
        public int transferTypeId { get; set; }
        public object transferSubTypeId { get; set; }
        //public int customerId { get; set; }
        //public int productId { get; set; }
        //public int shipperId { get; set; }
        //public int consigneeId { get; set; }
        //public int carrierId { get; set; }
        //public object consigneeAreaId { get; set; }
        //public object billToId { get; set; }
        public int? transferOrderLoaderId { get; set; }
        public int? departureEquipmentId { get; set; }
        public int? departureEquipmentTypeId { get; set; }
        //public int weightUnitOfMeasureId { get; set; }
        //public int volumeUnitOfMeasureId { get; set; }
        //public int productWeightUnitOfMeasureId { get; set; }
        //public int siteId { get; set; }
        //public object productAliasId { get; set; }
        //public string shipperOrderNumber { get; set; }
        //public object freightCharge { get; set; }
        //public object poNumber { get; set; }
        //public object workOrderComment { get; set; }
        //public object bolComment { get; set; }
        //public object consigneeOrderNumber { get; set; }
        public string departureEquipmentName { get; set; }
        //public object departureSealNumbers { get; set; }
        //public object processBy { get; set; }
        public string status { get; set; }
        //public object strength { get; set; }
        //public double orderWeight { get; set; }
        //public double orderVolume { get; set; }
        //public object scaleInWeight { get; set; }
        //public long scheduledDate { get; set; }
        //public long orderedDate { get; set; }
        //public long deliveryDate { get; set; }
        public string loadStartDate { get; set; }
        public string loadEndDate { get; set; }
        public bool isVoided { get; set; }
        public bool isCompleted { get; set; }
        public bool isValidated { get; set; }
        public List<TransferOrderArrivalModel> transferOrderArrivals { get; set; }
        public bool isSplit
        {
            get
            {
                if (transferOrderArrivals.Count() > 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }
        public int arrivalCount
        {
            get
            {
                return transferOrderArrivals.Count();
            }
        }


        private DateTime convertDateTime(long date)
        {
            DateTime dt = JsonConvert.DeserializeObject<DateTime>(date.ToString());
            return dt;
        }
    }
}

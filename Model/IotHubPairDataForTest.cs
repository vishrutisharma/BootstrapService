using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class IotHubPairDataForTest : TableEntity
    {
        public IotHubPairDataForTest()
        {

        }

        public IotHubPairDataForTest(string iothubpairdetails_partitionkey, string iothubpairdetails_rowkey)
        {
            PartitionKey = iothubpairdetails_partitionkey;  //Epredia_RevosTissueProcessor_AssociatedIoTHub
            RowKey = iothubpairdetails_rowkey;  // DeviceId
        }
        public string DeviceId { get; set; }
        public string SecureString { get; set; }
        public string AssignedIoTHubName { get; set; }
        public string CertificateExpiryDate { get; set; }
        public string PfxFile { get; set; }
        public string UserLinkStatus { get; set; }
        public string DeviceSendStatus { get; set; }
    }
}

using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class IotHubPairDetails : TableEntity
    {
        public IotHubPairDetails()
        {

        }

        public IotHubPairDetails(string iothubpairdetails_partitionkey,string iothubpairdetails_rowkey)
        {
            PartitionKey = iothubpairdetails_partitionkey;  //Epredia_RevosTissueProcessor_AssociatedIoTHub
            RowKey = iothubpairdetails_rowkey;  // DeviceId
        }
        public string SecureString { get; set; }
        public string AssignedIoTHubName { get; set; }
        public string CertificateExpiryDate { get; set; }
        public string PfxFile { get; set; }
    }
}

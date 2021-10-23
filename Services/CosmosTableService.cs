using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BootstrapService.Models;
using BootstrapService.Model;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Web.Http;
using System.Net;

namespace BootstrapService.Services
{
    public class CosmosTableService : ICosmosTableService
    {
        private CloudTableClient _devCloudTableClient;
        private CloudTableClient _qaCloudTableClient;
        private string _tableName;
        public CloudTable devTable;
        public CloudTable qaTable;
        public static string devDeviceMetadataURL = "https://eprediatissueprocessorapi-dev.azurewebsites.net/epredia/api";
        public static string qaDeviceMetadataURL = "https://eprediatissueprocessorapi-test.azurewebsites.net/epredia/api";

        public CosmosTableService(CloudTableClient devCloudTableClient, string tableName, CloudTableClient qaCloudTableClient)
        {
            _devCloudTableClient = devCloudTableClient;
            _qaCloudTableClient = qaCloudTableClient;
            _tableName = tableName;
            devTable = devCloudTableClient.GetTableReference(tableName);
            qaTable = qaCloudTableClient.GetTableReference(tableName);
        }

        public async Task<ProvisionedDeviceIoTMetadata> InsertProvisionedDeviceData(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timezone, string serviceRegion, string commercialRegion, string hubRegion, string environment)
        {
            ProvisionedDeviceIoTMetadata insertedDevice = new ProvisionedDeviceIoTMetadata();
            if (_iotHubConnectionInfo.AssignedIoTHubName != null && _iotHubConnectionInfo.PfxFile != null && _iotHubConnectionInfo.SecureString != null)
            {
                ProvisionedDeviceIoTMetadata provisionedDevice = new ProvisionedDeviceIoTMetadata("epredia_" + deviceType.ToLower() + "_associatediothub", deviceId)
                {
                    AssociatedIoTHub = _iotHubConnectionInfo.AssignedIoTHubName,
                    CertificateEndDate = _iotHubConnectionInfo.CertificateExpiryDate.ToString(),
                    CertificateStartDate = DateTime.UtcNow.ToString(),
                    DeviceType = deviceType,
                    DeviceId = deviceId
                };
                string deviceName = "";
                if (BootstrapConfigurationService.r2.IsMatch(deviceId.ToLower()) || BootstrapConfigurationService.r3.IsMatch(deviceId.ToUpper()))
                {
                    string[] substr = deviceId.Split('-');
                    deviceName = substr[0];
                }
                else
                {
                    deviceName = deviceId;
                }
                ProvisionedDeviceMetadata provisionDevice = new ProvisionedDeviceMetadata()
                {
                    deviceName = deviceName,
                    deviceTimeZone = timezone,
                    softwareVersion = softwareVersion,
                    firmwareVersion = firmwareVersion,
                    deviceType = deviceType,
                    commercialRegion = commercialRegion != null ? commercialRegion : "00",
                    serviceRegion = serviceRegion != null ? serviceRegion : "00",
                    hubRegion = hubRegion != null ? hubRegion : "01",
                    deviceSerialNumber = deviceId,
                    deviceStatus = "Available"
                };
                try
                {
                    CloudTable table;
                    switch (environment)
                    {
                        case "1":
                            table = devTable;
                            break;
                        case "2":
                            table = qaTable;
                            break;
                        default:
                            table = devTable;
                            break;
                    }
                    await InsertIotHubDetails(_iotHubConnectionInfo, deviceId, deviceType, environment);
                    // Create the InsertOrReplace table operation
                    TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(provisionedDevice);
                    // Execute the operation.
                    TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                    insertedDevice = result.Result as ProvisionedDeviceIoTMetadata;
                    if (result.RequestCharge.HasValue)
                    {
                        Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                    }
                    await InsertDeviceMetadataAsync(deviceId, deviceType, provisionDevice, environment);
                    return insertedDevice;
                }
                catch (StorageException e)
                {
                    Program.logger.LogCritical(e.Message);
                    throw;
                }
            }
            else
                return insertedDevice;
        }

        public async Task InsertDeviceMetadataAsync(string deviceId, string deviceType, ProvisionedDeviceMetadata input, string environment)
        {
            try
            {
                string deviceMetadataURL = "";
                if (environment == "1")
                {
                    deviceMetadataURL = devDeviceMetadataURL;
                }
                else
                {
                    deviceMetadataURL = qaDeviceMetadataURL;
                }
                HttpResponseMessage responseMessage = await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(deviceMetadataURL + "/" + deviceId + "/" + deviceType + "/DeviceMetadata?requestType=devicemetadata", input).ConfigureAwait(false);
                responseMessage.EnsureSuccessStatusCode();
                string content = (await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                Program.logger.LogCritical(ex.Message);
            }

        }

        public async Task<IotHubPairDetails> InsertIotHubDetails(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string environment)
        {
            IotHubPairDetails insertedDevice = new IotHubPairDetails();
            if (_iotHubConnectionInfo.AssignedIoTHubName != null && _iotHubConnectionInfo.PfxFile != null && _iotHubConnectionInfo.SecureString != null)
            {
                IotHubPairDetails provisionedDevice = new IotHubPairDetails("epredia_deviceiothubpair", deviceType.ToLower() + "_" + deviceId)
                {
                    AssignedIoTHubName = _iotHubConnectionInfo.AssignedIoTHubName,
                    CertificateExpiryDate = _iotHubConnectionInfo.CertificateExpiryDate.ToString(),
                    SecureString = _iotHubConnectionInfo.SecureString,
                    PfxFile = _iotHubConnectionInfo.PfxFile
                };

                try
                {
                    CloudTable table;
                    switch (environment)
                    {
                        case "1":
                            table = devTable;
                            break;
                        case "2":
                            table = qaTable;
                            break;
                        default:
                            table = devTable;
                            break;
                    }
                    // Create the InsertOrReplace table operation
                    TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(provisionedDevice);
                    // Execute the operation.
                    TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                    insertedDevice = result.Result as IotHubPairDetails;
                    if (result.RequestCharge.HasValue)
                    {
                        Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                    }
                    return insertedDevice;
                }
                catch (StorageException e)
                {
                    Program.logger.LogCritical(e.Message);
                    throw;
                }
            }
            else
                return insertedDevice;
        }
        public async Task<IotHubPairDataForTest> InsertIotHubDetailsForTestDevices(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string environment, string testGuid)
        {
            IotHubPairDataForTest insertedDevice = new IotHubPairDataForTest();
            string _partitionKey;
            if (testGuid == null || testGuid == "")
            {
                _partitionKey = "epredia_performancetest_blank";
            }
            else
            {
                _partitionKey = "epredia_performancetest_" + testGuid;
            }
            
            if (_iotHubConnectionInfo.AssignedIoTHubName != null && _iotHubConnectionInfo.PfxFile != null && _iotHubConnectionInfo.SecureString != null)
            {
                IotHubPairDataForTest provisionedDevice = new IotHubPairDataForTest(_partitionKey, deviceId)               
                {
                    DeviceId = deviceId,
                    AssignedIoTHubName = _iotHubConnectionInfo.AssignedIoTHubName,
                    CertificateExpiryDate = _iotHubConnectionInfo.CertificateExpiryDate.ToString(),
                    SecureString = _iotHubConnectionInfo.SecureString,
                    PfxFile = _iotHubConnectionInfo.PfxFile,
                    UserLinkStatus = "unlinked",
                    DeviceSendStatus = "unknown"
                };

                try
                {
                    CloudTable table;
                    switch (environment)
                    {
                        case "1":
                            table = devTable;
                            break;
                        case "2":
                            table = qaTable;
                            break;
                        default:
                            table = devTable;
                            break;
                    }
                   // TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(provisionedDevice);
                    TableOperation insertOrMergeOperation = TableOperation.InsertOrReplace(provisionedDevice);

                    TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                    insertedDevice = result.Result as IotHubPairDataForTest;
                    if (result.RequestCharge.HasValue)
                    {
                        Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                    }
                    return insertedDevice;
                }
                catch (StorageException e)
                {
                    Program.logger.LogCritical(e.Message);
                    throw;
                }
            }
            else
                return insertedDevice;
        }

        public async Task<ProvisionedDeviceIoTMetadata> InsertProvisionedDeviceDataForTestDevices(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timezone, string serviceRegion, string commercialRegion, string hubRegion, string environment, string testGuid)
        {
            ProvisionedDeviceIoTMetadata insertedDevice = new ProvisionedDeviceIoTMetadata();
            if (_iotHubConnectionInfo.AssignedIoTHubName != null && _iotHubConnectionInfo.PfxFile != null && _iotHubConnectionInfo.SecureString != null)
            {
                ProvisionedDeviceIoTMetadata provisionedDevice = new ProvisionedDeviceIoTMetadata("epredia_" + deviceType.ToLower() + "_associatediothub", deviceId)
                {
                    AssociatedIoTHub = _iotHubConnectionInfo.AssignedIoTHubName,
                    CertificateEndDate = _iotHubConnectionInfo.CertificateExpiryDate.ToString(),
                    CertificateStartDate = DateTime.UtcNow.ToString(),
                    DeviceType = deviceType,
                    DeviceId = deviceId
                };
                string deviceName = "";
                if (BootstrapConfigurationService.r2.IsMatch(deviceId.ToLower()) || BootstrapConfigurationService.r3.IsMatch(deviceId.ToUpper()))
                {
                    string[] substr = deviceId.Split('-');
                    deviceName = substr[0];
                }
                else
                {
                    deviceName = deviceId;
                }
                ProvisionedDeviceMetadata provisionDevice = new ProvisionedDeviceMetadata()
                {
                    deviceName = deviceName,
                    deviceTimeZone = timezone,
                    softwareVersion = softwareVersion,
                    firmwareVersion = firmwareVersion,
                    deviceType = deviceType,
                    commercialRegion = commercialRegion != null ? commercialRegion : "00",
                    serviceRegion = serviceRegion != null ? serviceRegion : "00",
                    hubRegion = hubRegion != null ? hubRegion : "01",
                    deviceSerialNumber = deviceId,
                    deviceStatus = "Available"
                };
                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                await InsertIotHubDetailsForTestDevices(_iotHubConnectionInfo, deviceId, deviceType, environment, testGuid);
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(provisionedDevice);
                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                insertedDevice = result.Result as ProvisionedDeviceIoTMetadata;
                if (result.RequestCharge.HasValue)
                {
                    Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                }
                await InsertDeviceMetadataAsync(deviceId, deviceType, provisionDevice, environment);
                return insertedDevice;
            }
            else
                return insertedDevice;
        }

        public async Task<IotHubPairDetails> GetDeviceIotHubPairDetails(string deviceId, string deviceType, string environment)
        {
            try
            {
                string PartitionKey = "epredia_deviceiothubpair";
                TableQuery<IotHubPairDetails> queryDefinition = new TableQuery<IotHubPairDetails>()
                    .Where(TableQuery.CombineFilters(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                 PartitionKey),
                       TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal,
                                  deviceType.ToLower() + "_" + deviceId.ToLower())));
                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                return queryData.Results[0];
            }
            catch (Exception ex)
            {
                Program.logger.LogCritical(ex.Message);
                return new IotHubPairDetails();
            }
        }
        public async Task DeleteIotHubPairByID(string deviceId, string deviceType, string env)
        {
            try
            {
                CloudTable table;
                switch (env)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                IotHubPairDetails iotHubPair = new IotHubPairDetails();
                iotHubPair = await GetDeviceIotHubPairDetails(deviceId, deviceType, env);
                TableOperation deleteOperation = TableOperation.Delete(iotHubPair);
                await table.ExecuteAsync(deleteOperation);

            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }
        public async Task<List<Metadata>> GetDeviceTypeList()
        {
            try
            {
                string PartitionKey = "epredia_devicetype";
                TableQuery<DeviceTypeInformationEntity> queryDefinition = new TableQuery<DeviceTypeInformationEntity>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey));
                List<DeviceTypeInformationEntity> deviceTypes = await GetDeviceTypes(queryDefinition);
                List<Metadata> deviceTypeList = new List<Metadata>();
                foreach (DeviceTypeInformationEntity item in deviceTypes)
                {
                    Metadata deviceType = new Metadata();
                    deviceType.Id = item.RowKey;
                    deviceType.Value = getLocalizedValue(item.LocalizedName, "en-us");
                    deviceTypeList.Add(deviceType);
                }
                return deviceTypeList;
            }
            catch (Exception ex)
            {
                Program.logger.LogInformation("Exception occured in getting device type list");
                Program.logger.LogCritical(ex.Message);
                return new List<Metadata>();
            }
        }

        public async Task<List<DeviceTypeInformationEntity>> GetDeviceTypes(TableQuery<DeviceTypeInformationEntity> queryDefinition)
        {
            try
            {
                var queryData = await devTable.ExecuteQuerySegmentedAsync(queryDefinition, null);
                List<DeviceTypeInformationEntity> deviceTypes = new List<DeviceTypeInformationEntity>();
                foreach (DeviceTypeInformationEntity deviceTypeInformationEntity in queryData)
                {
                    deviceTypes.Add(deviceTypeInformationEntity);
                }
                return deviceTypes;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }

        public static string getLocalizedValue(string localizedTitle, string languagePreference)
        {
            var jsonObj = JObject.Parse(localizedTitle);
            JToken value = null;
            if (jsonObj != null)
            {
                value = jsonObj.GetValue(languagePreference);
            }
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public async Task<string> GetIotHubEndpoint(string hubRegion, string environment)
        {
            string PartitionKey = "epredia_iothubdetails";
            TableQuery<IotHubDetailEntity> queryDefinition = new TableQuery<IotHubDetailEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, PartitionKey));
            List<IotHubDetailEntity> deviceTypes = await GetIotHubEndPoints(queryDefinition, environment);
            int region = Convert.ToInt32(hubRegion.Trim());
            foreach (IotHubDetailEntity iotHub in deviceTypes)
            {
                if (iotHub.RegionId == region.ToString())
                {
                    return iotHub.IotHubDetail;
                }
            }
            return string.Empty;
        }

        public async Task<List<IotHubDetailEntity>> GetIotHubEndPoints(TableQuery<IotHubDetailEntity> queryDefinition, string environment)
        {
            try
            {
                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                List<IotHubDetailEntity> iotHubs = new List<IotHubDetailEntity>();
                foreach (IotHubDetailEntity deviceTypeInformationEntity in queryData)
                {
                    iotHubs.Add(deviceTypeInformationEntity);
                }
                return iotHubs;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }

        public async Task<List<IotHubPairDataForTest>> GetProvisionedDeviceCountForTestId(string testGuid, string environment)
        {
            try
            {
                TableQuery<IotHubPairDataForTest> queryDefinition = new TableQuery<IotHubPairDataForTest>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "epredia_performancetest_" + testGuid));

                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                return queryData.Results;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }
        public async Task<IotHubPairDataForTest> GetProvisionedDeviceCountForDeviceId(string testGuid, string deviceId, string environment)
        {
            try
            {
                TableQuery<IotHubPairDataForTest> queryDefinition = new TableQuery<IotHubPairDataForTest>().Where(TableQuery.CombineFilters(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                 "epredia_performancetest_" + testGuid),
                       TableOperators.And,
                        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal,
                                  deviceId)));

                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                return queryData.Results[0];
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }
        public async Task<string> UpdateDeviceProvisionedForTest(IotHubPairDataForTest input, string env)
        {
            try
            {
                CloudTable table;
                switch (env)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                TableOperation updateOperation = TableOperation.InsertOrMerge(input);

                TableResult result = await table.ExecuteAsync(updateOperation);
                IotHubPairDataForTest userGroupEntityDB = result.Result as IotHubPairDataForTest;
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<string> DeleteDeviceProvisionedForTest(IotHubPairDataForTest input, string env)
        {
            try
            {
                CloudTable table;
                switch (env)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                TableOperation deleteOperation = TableOperation.Delete(input);
                await table.ExecuteAsync(deleteOperation);
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<PerformanceTestGuidEntity> InsertPerformanceTestGuid(string environment, string testGuid)
        {
            PerformanceTestGuidEntity performanceTestGuid = new PerformanceTestGuidEntity("epredia_performancetest", testGuid)
            {
                Status = "TestInProgress"
            };

            try
            {
                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(performanceTestGuid);
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);

                if (result.RequestCharge.HasValue)
                {
                    Console.WriteLine("Request Charge of InsertOrMerge Operation: " + result.RequestCharge);
                }
                return result.Result as PerformanceTestGuidEntity;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw;
            }
        }
        public async Task<string> UpdatePerformanceTestGuid(PerformanceTestGuidEntity input, string env)
        {
            try
            {
                CloudTable table;
                switch (env)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                TableOperation updateOperation = TableOperation.InsertOrMerge(input);

                TableResult result = await table.ExecuteAsync(updateOperation);
                PerformanceTestGuidEntity userGroupEntityDB = result.Result as PerformanceTestGuidEntity;
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public async Task<List<PerformanceTestGuidEntity>> GetPerformanceTestGuid(string environment)
        {
            try
            {
                TableQuery<PerformanceTestGuidEntity> queryDefinition = new TableQuery<PerformanceTestGuidEntity>().Where(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                 "epredia_performancetest"));

                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                return queryData.Results;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }

        public async Task GetAllRecordsMatchingPartitionKey(string environment)
        {
            try
            {
                TableQuery<PerformanceTestGuidEntity> queryDefinition = new TableQuery<PerformanceTestGuidEntity>().Where(
                               TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal,
                                 "epredia_performancetest"));

                CloudTable table;
                switch (environment)
                {
                    case "1":
                        table = devTable;
                        break;
                    case "2":
                        table = qaTable;
                        break;
                    default:
                        table = devTable;
                        break;
                }
                var queryData = await table.ExecuteQuerySegmentedAsync(queryDefinition, null);
                return;
            }
            catch (StorageException e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }
        }
    }
}
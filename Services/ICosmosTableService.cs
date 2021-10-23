using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BootstrapService.Models;
using BootstrapService.Model;

namespace BootstrapService.Services
{
    public interface ICosmosTableService
    {
        Task<ProvisionedDeviceIoTMetadata> InsertProvisionedDeviceData(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timezone, string serviceRegion, string commercialRegion, string hubRegion, string environment);
        Task<List<Metadata>> GetDeviceTypeList();
        Task DeleteIotHubPairByID(string deviceId, string deviceType, string env);
        Task<IotHubPairDetails> GetDeviceIotHubPairDetails(string deviceId, string deviceType, string environment);
        Task<IotHubPairDetails> InsertIotHubDetails(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string environment);
        Task<string> GetIotHubEndpoint(string hubRegion, string environment);
        Task<ProvisionedDeviceIoTMetadata> InsertProvisionedDeviceDataForTestDevices(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timezone, string serviceRegion, string commercialRegion, string hubRegion, string environment, string testGuid);
        Task<IotHubPairDataForTest> InsertIotHubDetailsForTestDevices(IothubConnectionInfo _iotHubConnectionInfo, string deviceId, string deviceType, string environment, string testGuid);
        Task<List<IotHubPairDataForTest>> GetProvisionedDeviceCountForTestId(string testGuid, string environment);
        Task<string> DeleteDeviceProvisionedForTest(IotHubPairDataForTest input, string env);
        Task<string> UpdateDeviceProvisionedForTest(IotHubPairDataForTest input, string env);
        Task<IotHubPairDataForTest> GetProvisionedDeviceCountForDeviceId(string testGuid, string deviceId, string environment);
        Task<PerformanceTestGuidEntity> InsertPerformanceTestGuid(string environment, string testGuid);
        Task<List<PerformanceTestGuidEntity>> GetPerformanceTestGuid(string environment);
        Task<string> UpdatePerformanceTestGuid(PerformanceTestGuidEntity input, string env);
    }
}

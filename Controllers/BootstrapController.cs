using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using BootstrapService.Services;
using System.Net.Http;
using BootstrapService.Model;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Localization;

namespace BootstrapService
{
    [Route("api/[controller]")]
    [ApiController]
    public class BootstrapController : ControllerBase
    {
        private readonly IBootstrapConfigurationService _bootstrapConfigurationService;
        private ICosmosTableService _cosmosTableService;

        public BootstrapController(IBootstrapConfigurationService bootstrapConfigurationService, ICosmosTableService cosmosTableService)
        {
            _bootstrapConfigurationService = bootstrapConfigurationService;
            _cosmosTableService = cosmosTableService;
        }
        // GET: api/Bootstrap
        [HttpGet]
        public string Get()
        {
            return "BootStrap Service is Running";
        }

        // GET: api/Bootstrap/getRegisteredDevices
        [HttpGet("getEnrolledDevices")]
        public List<string> GetEnrolledDevices(string Environment)
        {
            string environment = Environment == null ? "1" : Environment;
            List<string> devices = _bootstrapConfigurationService.GetEnrolledDevices(environment);
            return devices;
        }

        // GET: api/Bootstrap/getDeviceTypes
        [HttpGet("getDeviceTypes")]
        public List<Metadata> GetDeviceType()
        {
            Task<List<Metadata>> deviceTypes = _cosmosTableService.GetDeviceTypeList();
            return deviceTypes.Result;
        }

        // POST: api/Bootstrap/provisionDevice
        [HttpPost("provisionDevice")]
        public async Task<IothubConnectionInfo> EnrollDeviceToDPSAsync([FromBody] JwtInfo jwtInfo, string Environment)
        {
            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();

            try
            {
                string encryptedRandomKey = jwtInfo.EncryptedRandomKey;
                string encryptedToken = jwtInfo.EncryptedToken;
                DecryptedData decryptedData = _bootstrapConfigurationService.DecryptTokenAndRandomKey(encryptedRandomKey, encryptedToken);
                var claimsValues = _bootstrapConfigurationService.ReadToken(decryptedData.DecryptedToken);

                string deviceId;
                if (claimsValues.ContainsKey("deviceId"))
                    deviceId = claimsValues["deviceId"].ToLower();
                else if (claimsValues.ContainsKey("device_id"))
                    deviceId = claimsValues["device_id"].ToLower();
                else
                    return iothubConnectionInfo;

                string deviceType;
                if (claimsValues.ContainsKey("deviceType"))
                    deviceType = claimsValues["deviceType"].ToLower();
                else if (claimsValues.ContainsKey("device_type"))
                    deviceType = claimsValues["device_type"].ToLower();
                else
                    return iothubConnectionInfo;

                string softwareVersion;
                if (claimsValues.ContainsKey("softwareVersion"))
                    softwareVersion = claimsValues["softwareVersion"];
                else if (claimsValues.ContainsKey("software_version"))
                    softwareVersion = claimsValues["software_version"];
                else
                    return iothubConnectionInfo;

                string firmwareVersion;
                if (claimsValues.ContainsKey("firmwareVersion"))
                    firmwareVersion = claimsValues["firmwareVersion"];
                else if (claimsValues.ContainsKey("firmware_version"))
                    firmwareVersion = claimsValues["firmware_version"];
                else
                    return iothubConnectionInfo;

                string timezone;
                    if (claimsValues.ContainsKey("timeZone"))
                    timezone = claimsValues["timeZone"];
                else if (claimsValues.ContainsKey("timezone"))
                    timezone = claimsValues["timezone"];
                else
                    return iothubConnectionInfo;
                string serviceRegion;
                if (claimsValues.ContainsKey("serviceRegion"))
                    serviceRegion = claimsValues["serviceRegion"];
                else if (claimsValues.ContainsKey("service_region"))
                    serviceRegion = claimsValues["service_region"];
                else
                    return iothubConnectionInfo;

                string commercialRegion;
                if (claimsValues.ContainsKey("commercialRegion"))
                    commercialRegion = claimsValues["commercialRegion"];
                else if (claimsValues.ContainsKey("commercial_region"))
                    commercialRegion = claimsValues["commercial_region"];
                else
                    return iothubConnectionInfo;

                string hubRegion;
                if (claimsValues.ContainsKey("hubRegion"))
                    hubRegion = claimsValues["hubRegion"];
                else if (claimsValues.ContainsKey("hub_region"))
                    hubRegion = claimsValues["hub_region"];
                else
                    return iothubConnectionInfo;
                string environment = Environment == null ? "1" : Environment;
                string iothubendpoint = await _cosmosTableService.GetIotHubEndpoint(hubRegion, environment);
                if(iothubendpoint == "" || iothubendpoint == null)
                {
                    return iothubConnectionInfo;
                }
                iothubConnectionInfo = await _bootstrapConfigurationService.ProvisionDeviceAsync(deviceId, environment, iothubendpoint);
                await _cosmosTableService.InsertProvisionedDeviceData(iothubConnectionInfo, deviceId, deviceType, softwareVersion, firmwareVersion, timezone, serviceRegion, commercialRegion, hubRegion, environment);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return iothubConnectionInfo;
        }

        // POST: api/Bootstrap/provisionDeviceV2
        [HttpPost("provisionDeviceV2")]
        public async Task<IothubConnectionInfo> EnrollDeviceToDPSAsyncV2([FromBody] JwtInfo jwtInfo, string Environment)
        {
            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();

            try
            {
                string encryptedRandomKey = jwtInfo.EncryptedRandomKey;
                string encryptedToken = jwtInfo.EncryptedToken;
                DecryptedData decryptedData = _bootstrapConfigurationService.DecryptTokenAndRandomKeyV2(encryptedRandomKey, encryptedToken);
                var claimsValues = _bootstrapConfigurationService.ReadToken(decryptedData.DecryptedToken);
                string deviceId;
                if (claimsValues.ContainsKey("deviceId"))
                    deviceId = claimsValues["deviceId"].ToLower();
                else if (claimsValues.ContainsKey("device_id"))
                    deviceId = claimsValues["device_id"].ToLower();
                else
                    return iothubConnectionInfo;
                if (deviceId.Contains("\0"))
                {
                    deviceId = deviceId.Replace("\0", String.Empty);
                }
                string deviceType;
                if (claimsValues.ContainsKey("deviceType"))
                    deviceType = claimsValues["deviceType"].ToLower();
                else if (claimsValues.ContainsKey("device_type"))
                    deviceType = claimsValues["device_type"].ToLower();
                else
                    return iothubConnectionInfo;
                string softwareVersion;
                if (claimsValues.ContainsKey("deviceSoftwareVersion"))
                    softwareVersion = claimsValues["deviceSoftwareVersion"];
                else if (claimsValues.ContainsKey("software_version"))
                    softwareVersion = claimsValues["software_version"];
                else
                    return iothubConnectionInfo;
                string firmwareVersion;
                if (claimsValues.ContainsKey("deviceFirmwareVersion"))
                    firmwareVersion = claimsValues["deviceFirmwareVersion"];
                else if (claimsValues.ContainsKey("firmware_version"))
                    firmwareVersion = claimsValues["firmware_version"];
                else
                    return iothubConnectionInfo;
                string timezone;
                string serviceregion;
                string commercialregion;
                string hubregion;
                if (claimsValues.ContainsKey("deviceTimeZone"))
                    timezone = claimsValues["deviceTimeZone"];
                else if (claimsValues.ContainsKey("timezone"))
                    timezone = claimsValues["timezone"];
                else
                    timezone = "+05:30";
                //if (claimsValues.ContainsKey("deviceTimeZone")) timezone = claimsValues["deviceTimeZone"];
                //if (claimsValues.ContainsKey("serviceRegion")) serviceregion = claimsValues["serviceRegion"];
                if (claimsValues.ContainsKey("serviceRegion"))
                    serviceregion = claimsValues["serviceRegion"];
                else if (claimsValues.ContainsKey("service_region"))
                    serviceregion = claimsValues["service_region"];
                else
                    serviceregion = "00";
                //if (claimsValues.ContainsKey("commercialRegion")) commercialregion = claimsValues["commercialRegion"];
                if (claimsValues.ContainsKey("commercialRegion"))
                    commercialregion = claimsValues["commercialRegion"];
                else if (claimsValues.ContainsKey("commercial_region"))
                    commercialregion = claimsValues["commercial_region"];
                else
                    commercialregion = "00";
                //if (claimsValues.ContainsKey("hubRegion")) hubregion = claimsValues["hubRegion"];
                if (claimsValues.ContainsKey("hubRegion"))
                    hubregion = claimsValues["hubRegion"];
                else if (claimsValues.ContainsKey("hub_region"))
                    hubregion = claimsValues["hub_region"];
                else
                    hubregion = "01";
                string environment = Environment == null ? "1" : Environment;
                string iothubendpoint = await _cosmosTableService.GetIotHubEndpoint(hubregion, environment);
                if (iothubendpoint == "" || iothubendpoint == null)
                {
                    return iothubConnectionInfo;
                }
                iothubConnectionInfo = await _bootstrapConfigurationService.ProvisionDeviceAsync(deviceId, environment, iothubendpoint);
                await _cosmosTableService.InsertProvisionedDeviceData(iothubConnectionInfo, deviceId, deviceType, softwareVersion, firmwareVersion, timezone, serviceregion, commercialregion, hubregion, environment);
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return iothubConnectionInfo;
        }

        // POST: api/Bootstrap/deleteDevice
        [HttpPost("deleteDevice")]
        public async Task<string> DeleteDevices([FromBody] JwtInfo jwtInfo, string Environment)
        {
            try
            {
                string encryptedRandomKey = jwtInfo.EncryptedRandomKey;
                string encryptedToken = jwtInfo.EncryptedToken;
                DecryptedData decryptedData = _bootstrapConfigurationService.DecryptTokenAndRandomKey(encryptedRandomKey, encryptedToken);
                var claimsValues = _bootstrapConfigurationService.ReadToken(decryptedData.DecryptedToken);
                string deviceId;
                if (claimsValues.ContainsKey("deviceId"))
                    deviceId = claimsValues["deviceId"].ToLower();
                else if (claimsValues.ContainsKey("device_id"))
                    deviceId = claimsValues["device_id"].ToLower();
                else
                    return "Incorrect Token";

                if (deviceId.Contains("\0"))
                {
                    deviceId = deviceId.Replace("\0", String.Empty);
                }
                string deviceType;
                if (claimsValues.ContainsKey("deviceType"))
                    deviceType = claimsValues["deviceType"].ToLower();
                else if (claimsValues.ContainsKey("device_type"))
                    deviceType = claimsValues["device_type"].ToLower();
                else
                    return "Incorrect Token";

                string environment = Environment == null ? "1" : Environment;
                _bootstrapConfigurationService.DeleteDeviceAsync(deviceId, environment);
               
                HttpRequestMessage httpRequestMessage;
                if (environment == "1")
                    httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLDev + "/" + deviceId + "/" + deviceType + "/deProvisionDevice");
                else
                    httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLTest + "/" + deviceId + "/" + deviceType + "/deProvisionDevice");
                HttpResponseMessage result = await DeviceSimulatorHelperService.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                return $"Deleted Device {deviceId} from DPS and IoTHub.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Incorrect Token";
            }
        }
    }
}

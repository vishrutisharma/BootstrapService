using BootstrapService.Model;
using BootstrapService.Model.ActivitySchemaModels;
using BootstrapService.Model.DataLogSplunkSchemaModels;
using BootstrapService.Model.FaultSchemaModels;
using BootstrapService.Model.ValveSchemaModels;
using BootstrapService.Model.WarningSchemaModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;

namespace BootstrapService.Services
{
    public class DeviceSimulatorHelperService : IDeviceSimulatorHelperService
    {
        private readonly IBootstrapConfigurationService _bootstrapConfigurationService;
        public static CosmosTableService _cosmosTableService;

        public DeviceSimulatorHelperService(IBootstrapConfigurationService bootstrapConfigurationService, CosmosTableService cosmosTableService)
        {
            _bootstrapConfigurationService = bootstrapConfigurationService;
            _cosmosTableService = cosmosTableService;
        }

        public static readonly string userDeviceLinkingURL = "https://linkingdevicedotnet.azurewebsites.net/api/LinkUserDevice?code=1QImNkMEQPCqbs9Nxhjga2amN95W8PJ1loFXbFi05rIk3XOwK3HlFw==";
        public static readonly string userDeviceLinkingTestURL = "https://linkingdevicedotnet-test.azurewebsites.net/api/LinkUserDevice?code=n71myl2704DwyftLMuKIObTDoAAXCX/5b47nWbZaCv0cExiDVTrNuA==";
        public static List<string> devices = new List<string>();
        public static List<string> deviceTypes = new List<string>();
        public static Timer timer = new Timer();
        public static DeviceClient deviceClient = null;
        public static Dictionary<string, IothubConnectionInfo> deviceIothubPair = new Dictionary<string, IothubConnectionInfo>();
        public static string _publicKey;
        public static string lastProvisionedDeviceId;
        public static IothubConnectionInfo lastProvisionedDeviceInfo = new IothubConnectionInfo();
        public static readonly HttpClient httpClient = new HttpClient();
        public static readonly string deleteDeviceURLDev = "https://eprediatissueprocessorapi-dev.azurewebsites.net/epredia/api";
        public static readonly string deleteDeviceURLTest = "https://eprediatissueprocessorapi-test.azurewebsites.net/epredia/api";
        public static Dictionary<string, string> userMessageSelectedDict = new Dictionary<string, string>();
        public static string topic;
        public static string messageType;
        public static string message = null;

        public static Regex versionRegex = new Regex("^[0-9].[0-9].[0-9]");
        public static Regex timezoneRegex = new Regex("^[+-]{0,1}[0-9]{2}:[0-9]{2}$");
        public static Regex regionRegex = new Regex("^[0-9]+$");

        public string GetDeviceId(object userMsg)
        {
            JObject msg = JObject.Parse(userMsg.ToString());
            if (msg.ContainsKey("id"))
            {
                return msg["id"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
        public bool ValidateJsonFormat(object userMsg, string topicType, string deviceId)
        {
            try
            {
                PropertyInfo[] prop;
                PropertyInfo[] subProperties = null;
                string topic = topicType.ToLower().ToString();
                var result = JObject.Parse(userMsg.ToString());
                if (topic == "updatemetadata")
                {
                    UpdateMetadataSchema props = new UpdateMetadataSchema();
                    prop = props.GetType().GetProperties();
                }
                else if (topic == "fault")
                {
                    FaultSchema props = new FaultSchema();
                    prop = props.GetType().GetProperties();
                    Model.FaultSchemaModels.EventParameters subProps = new Model.FaultSchemaModels.EventParameters();
                    subProperties = subProps.GetType().GetProperties();
                }
                else if (topic == "main telemetry")
                {
                    MainTelemetrySchema props = new MainTelemetrySchema();
                    prop = props.GetType().GetProperties();
                }
                else if (topic == "power telemetry")
                {
                    PowerTelemetrySchema props = new PowerTelemetrySchema();
                    prop = props.GetType().GetProperties();
                }
                else if (topic == "warning")
                {
                    WarningSchema props = new WarningSchema();
                    prop = props.GetType().GetProperties();
                    Model.WarningSchemaModels.EventParameters subProps = new Model.WarningSchemaModels.EventParameters();
                    subProperties = subProps.GetType().GetProperties();
                }
                else if (topic == "activity")
                {
                    ActivitySchema props = new ActivitySchema();
                    prop = props.GetType().GetProperties();
                    Model.ActivitySchemaModels.EventParameters subProps = new Model.ActivitySchemaModels.EventParameters();
                    subProperties = subProps.GetType().GetProperties();
                }
                else if (topic == "valve")
                {
                    ValveSchema props = new ValveSchema();
                    prop = props.GetType().GetProperties();
                    Model.ValveSchemaModels.EventParameters subProps = new Model.ValveSchemaModels.EventParameters();
                    subProperties = subProps.GetType().GetProperties();
                }
                else if (topic == "datalogsplunk")
                {
                    DataLogSplunkSchema props = new DataLogSplunkSchema();
                    prop = props.GetType().GetProperties();
                    Model.DataLogSplunkSchemaModels.EventParameters subProps = new Model.DataLogSplunkSchemaModels.EventParameters();
                    subProperties = subProps.GetType().GetProperties();
                }
                else if (topic == "devicemetadata")
                {
                    DeviceMetadataSchema props = new DeviceMetadataSchema();
                    prop = props.GetType().GetProperties();
                }
                else if (topic == "devicestatus")
                {
                    DeviceStatusSchema props = new DeviceStatusSchema();
                    prop = props.GetType().GetProperties();
                }
                else
                    return false;

                JObject msg = JObject.Parse(userMsg.ToString());
                IList<string> keys = msg.Properties().Select(p => p.Name).ToList();
                foreach (PropertyInfo item in prop)
                {
                    if (!keys.Contains(item.Name))
                        return false;
                }
                if (subProperties != null)
                {
                    foreach (PropertyInfo item in subProperties)
                    {
                        if (!msg.ToString().Contains(item.Name))
                            return false;
                    }
                }
                if (msg["device_id"].ToString().ToLower() != deviceId.ToLower())
                {
                    Program.logger.LogError("Device Id in message doesn't match request");
                    return false;
                }
                message = userMsg.ToString();
                return true;
            }
            catch (Exception ex)
            {
                Program.logger.LogCritical(ex.Message);
                return false;
            }
        }
        public async Task RegisterDeviceToIoTHub(string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timezone, string serviceRegion, string commercialRegion, string hubRegion, string environment)
        {
            try
            {
                Program.logger.LogInformation("Execution begings for Registering device to IOT Hub");
                if (serviceRegion == "")
                    serviceRegion = "00";

                if (commercialRegion == "")
                    commercialRegion = "00";

                if (hubRegion == "")
                    hubRegion = "01";

                JwtInfo jwtInfo = GetDeviceToken(deviceId, deviceType, softwareVersion, firmwareVersion, timezone, serviceRegion, commercialRegion, hubRegion);
                var responseMessage = await EnrollDeviceToDPS(jwtInfo, environment);
                Program.logger.LogInformation("Device Enrolled to DPS and returns response " + responseMessage.Assigned);
                if (responseMessage.Assigned == true)   // Device is Provisioned Successfully
                {
                    //Creating Pfx file from data passed from bootstrap service
                    var dir = $"{deviceId}";  // folder location

                    if (!Directory.Exists(dir))  // if it doesn't exist, create
                        Directory.CreateDirectory(dir);

                    string pfxPath = $"{deviceId}\\{deviceId}.pfx";
                    Byte[] bytes = Convert.FromBase64String(responseMessage.PfxFile);
                    System.IO.File.WriteAllBytes(pfxPath, bytes);

                    IothubConnectionInfo deviceIothubData = new IothubConnectionInfo
                    {
                        SecureString = responseMessage.SecureString,
                        PfxFile = responseMessage.PfxFile,
                        AssignedIoTHubName = responseMessage.AssignedIoTHubName,
                        CertificateExpiryDate = responseMessage.CertificateExpiryDate
                    };

                    lastProvisionedDeviceId = deviceId;
                    lastProvisionedDeviceInfo.AssignedIoTHubName = deviceIothubData.AssignedIoTHubName;
                    lastProvisionedDeviceInfo.PfxFile = deviceIothubData.PfxFile;
                    lastProvisionedDeviceInfo.SecureString = deviceIothubData.SecureString;
                    lastProvisionedDeviceInfo.CertificateExpiryDate = deviceIothubData.CertificateExpiryDate;
                    Program.logger.LogInformation("Registered device to IOT Hub");

                }
            }
            catch (Exception e)
            {
                Program.logger.LogInformation("Exception occured while Registering device to IOT Hub");
                Program.logger.LogCritical(e.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(e.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
        }
        public async Task<IothubConnectionInfo> EnrollDeviceToDPS(JwtInfo jwtInfo, string environment)
        {
            Program.logger.LogInformation("EnrollDevice to DPS Executing");

            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();
            try
            {
                string encryptedRandomKey = jwtInfo.EncryptedRandomKey;
                string encryptedToken = jwtInfo.EncryptedToken;

                Program.logger.LogInformation("before DecryptTokenAndRandomKey");
                Program.logger.LogInformation(encryptedRandomKey);
                Program.logger.LogInformation(encryptedToken);
                DecryptedData decryptedData = _bootstrapConfigurationService.DecryptTokenAndRandomKey(encryptedRandomKey, encryptedToken);
                Program.logger.LogInformation("after DecryptTokenAndRandomKey");
                var claimsValues = _bootstrapConfigurationService.ReadToken(decryptedData.DecryptedToken);

                Program.logger.LogInformation("after Read token");
                string deviceId = claimsValues["deviceId"].ToLower();
                string deviceType = claimsValues["deviceType"];
                string softwareVersion = claimsValues["softwareVersion"];
                string firmwareVersion = claimsValues["firmwareVersion"];
                string timezone = claimsValues["timeZone"];
                string serviceRegion = claimsValues["serviceRegion"];
                string commercialRegion = claimsValues["commercialRegion"];
                string hubRegion = claimsValues["hubRegion"];
                string iothubendpoint = await _cosmosTableService.GetIotHubEndpoint(hubRegion, environment);
                Program.logger.LogInformation("IotHubEndPoint : " + iothubendpoint);
                Program.logger.LogInformation("before ProvisionDeviceAsync");
                iothubConnectionInfo = await _bootstrapConfigurationService.ProvisionDeviceAsync(deviceId, environment, iothubendpoint);
                Program.logger.LogInformation("Execution complete for ProvisionDeviceAsync");
                //Program.logger.LogInformation("Assigned : " + iothubConnectionInfo.Assigned.ToString());
                //Program.logger.LogInformation("AssignedIoTHubName : " + iothubConnectionInfo.AssignedIoTHubName.ToString());
                await _cosmosTableService.InsertProvisionedDeviceData(iothubConnectionInfo, deviceId, deviceType, softwareVersion, firmwareVersion, timezone, serviceRegion, commercialRegion, hubRegion, environment).ConfigureAwait(false);
                Program.logger.LogInformation("before SaveDeviceDetails");
            }
            catch (Exception ex)
            {
                Program.logger.LogInformation("Exception occured while Enrolling device to DPS");
                Program.logger.LogCritical(ex.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(ex.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
            return iothubConnectionInfo;
        }
        public async Task<string> RegisterDeviceToIoTHubForTest(JwtInfo jwtInfo, string environment, string testGuid)
        {
            try
            {
                Program.logger.LogInformation("Execution begings for Registering device to IOT Hub");
                var responseMessage = await EnrollDeviceToDPSForTesting(jwtInfo, environment, testGuid);
                Program.logger.LogInformation("Device Enrolled to DPS and returns response " + responseMessage.Assigned);
                if (responseMessage.Assigned == true)   // Device is Provisioned Successfully
                {
                    return "success";
                }
                return "";
            }
            catch (Exception e)
            {
                Program.logger.LogInformation("Exception occured while Registering device to IOT Hub");
                Program.logger.LogCritical(e.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(e.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
        }
        public async Task<IothubConnectionInfo> EnrollDeviceToDPSForTesting(JwtInfo jwtInfo, string environment, string testGuid)
        {
            Program.logger.LogInformation("EnrollDevice to DPS Executing");

            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();
            try
            {
                string encryptedRandomKey = jwtInfo.EncryptedRandomKey;
                string encryptedToken = jwtInfo.EncryptedToken;

                Program.logger.LogInformation("before DecryptTokenAndRandomKey");
                Program.logger.LogInformation(encryptedRandomKey);
                Program.logger.LogInformation(encryptedToken);
                DecryptedData decryptedData = _bootstrapConfigurationService.DecryptTokenAndRandomKey(encryptedRandomKey, encryptedToken);
                Program.logger.LogInformation("after DecryptTokenAndRandomKey");
                var claimsValues = _bootstrapConfigurationService.ReadToken(decryptedData.DecryptedToken);

                Program.logger.LogInformation("after Read token");
                string deviceId = claimsValues["deviceId"].ToLower();
                string deviceType = claimsValues["deviceType"];
                string softwareVersion = claimsValues["softwareVersion"];
                string firmwareVersion = claimsValues["firmwareVersion"];
                string timezone = claimsValues["timeZone"];
                string serviceRegion = claimsValues["serviceRegion"];
                string commercialRegion = claimsValues["commercialRegion"];
                string hubRegion = claimsValues["hubRegion"];
                string iothubendpoint = await _cosmosTableService.GetIotHubEndpoint(hubRegion, environment);
                Program.logger.LogInformation("IotHubEndPoint : " + iothubendpoint);
                Program.logger.LogInformation("before ProvisionDeviceAsync");
                iothubConnectionInfo = await _bootstrapConfigurationService.ProvisionDeviceForTestAsync(deviceId, environment, iothubendpoint);
                //Program.logger.LogInformation("Execution complete for ProvisionDeviceAsync");
                await _cosmosTableService.InsertProvisionedDeviceDataForTestDevices(iothubConnectionInfo, deviceId, deviceType, softwareVersion, firmwareVersion, timezone, serviceRegion, commercialRegion, hubRegion, environment, testGuid).ConfigureAwait(false);
                Program.logger.LogInformation("before SaveDeviceDetails");
            }
            catch (Exception ex)
            {
                Program.logger.LogInformation("Exception occured while Enrolling device to DPS");
                Program.logger.LogCritical(ex.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(ex.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
            return iothubConnectionInfo;
        }

        public JwtInfo GetToken(string userId, string password, string deviceType, string deviceId)
        {
            JwtInfo jwtInfo = new JwtInfo();
            var claims = new List<Claim>
            {
                new Claim("username", userId),
                new Claim("password", password),
                new Claim("deviceId", deviceId),
                new Claim("deviceType", deviceType)
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaims(claims);

            var rsa1 = RSAKeys.ImportPublicKey(System.IO.File.ReadAllText("publicKey.pem"));
            _publicKey = rsa1.ToXmlString(false);

            var token = CreateToken(claimsIdentity);
            jwtInfo.EncryptedToken = Rijndael.Encrypt(token);
            var randomKey = Rijndael.GetRandomKeyText();
            jwtInfo.EncryptedRandomKey = RSA1.Encrypt(randomKey, _publicKey, 2048);
            return jwtInfo;
        }

        public JwtInfo GetDeviceToken(string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timeZone, string serviceRegion, string commercialRegion, string hubRegion)
        {
            JwtInfo jwtInfo = new JwtInfo();
            var claims = new List<Claim>
            {
                new Claim("deviceId", deviceId),
                new Claim("deviceType",deviceType),
                new Claim("softwareVersion",softwareVersion),
                new Claim("firmwareVersion",firmwareVersion),
                new Claim("timeZone",timeZone),
                new Claim("serviceRegion", serviceRegion),
                new Claim("commercialRegion", commercialRegion),
                new Claim("hubRegion", hubRegion)
            };
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();
            claimsIdentity.AddClaims(claims);

            var rsa1 = RSAKeys.ImportPublicKey(System.IO.File.ReadAllText("publicKey.pem"));
            _publicKey = rsa1.ToXmlString(false);

            var token = CreateToken(claimsIdentity);
            jwtInfo.EncryptedToken = Rijndael.Encrypt(token);
            var randomKey = Rijndael.GetRandomKeyText();
            jwtInfo.EncryptedRandomKey = RSA1.Encrypt(randomKey, _publicKey, 2048);
            return jwtInfo;
        }

        public string CreateToken(ClaimsIdentity claimsIdentity)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateJwtSecurityToken(
            issuer: "Revos",
            audience: "Revos",
            subject: claimsIdentity,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddDays(1),
            issuedAt: DateTime.UtcNow
            );
            return tokenHandler.WriteToken(token);
        }
        public async Task DeleteDeviceAsync(string deviceId, string deviceType, string environment)
        {
            try
            {
                await _cosmosTableService.DeleteIotHubPairByID(deviceId, deviceType, environment);
                _bootstrapConfigurationService.DeleteDeviceAsync(deviceId, environment);
                HttpRequestMessage httpRequestMessage;
                if (environment == "1")
                    httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, deleteDeviceURLDev + "/" + deviceId + "/" + deviceType + "/deProvisionDevice");
                else
                    httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, deleteDeviceURLTest + "/" + deviceId + "/" + deviceType + "/deProvisionDevice");
                HttpResponseMessage result = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                result.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                Program.logger.LogCritical(e.Message);
            }
        }
        public async Task ConnectDeviceToIoTHubAsync(string deviceId, string deviceType, string environment)
        {
            string filePath = $"{deviceId}\\{deviceId}.pfx";
            IotHubPairDetails iotHubPair = await _cosmosTableService.GetDeviceIotHubPairDetails(deviceId, deviceType, environment);
            Program.logger.LogInformation("Device id present in deviceiothubpair.json" + deviceId);
            var security = GetSecurityCertificate(filePath, iotHubPair.SecureString);
            await CreateIoTDeviceClientAsync(security, deviceId, iotHubPair.AssignedIoTHubName);
        }

        public void DisconnectDevice()
        {
            timer.Stop();
            deviceClient.CloseAsync();
            deviceClient = null;
        }
        public SecurityProviderX509Certificate GetSecurityCertificate(string filePath, string secureString)
        {
            Program.logger.LogInformation("before getsecurity certificate");
            Program.logger.LogInformation(filePath);
            var certificate = new X509Certificate2(filePath, secureString, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            var security = new SecurityProviderX509Certificate(certificate);
            Program.logger.LogInformation("after GetSecurityCertificate");
            return security;
        }
        public async Task CreateIoTDeviceClientAsync(SecurityProviderX509Certificate security, string deviceId, string iothubHostname)
        {
            try
            {
                Program.logger.LogInformation("Creating X509 DeviceClient authentication.");
                var auth = new DeviceAuthenticationWithX509Certificate(deviceId, (security as SecurityProviderX509).GetAuthenticationCertificate());

                Program.logger.LogInformation("Connecting Device to IoTHub via secure pfx certificate and MQTT.");
                deviceClient = DeviceClient.Create(iothubHostname, auth, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                if (deviceClient == null)
                {
                    Program.logger.LogInformation("Failed to make connection");
                }
                else
                {
                    Program.logger.LogInformation("DeviceClient Created Successfully.");
                    await deviceClient.OpenAsync().ConfigureAwait(false);
                    Program.logger.LogInformation("Connected to IoTHub");
                }
            }
            catch (Exception ex)
            {
                Program.logger.LogCritical(ex.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(ex.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
        }

        public string UserSelectedTopic(string deviceId, string topicType, string messageType)
        {
            try
            {
                string topic = topicType.ToString().ToLower();
                message = null;
                if (topic == "updatemetadata")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\updatemetadata.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                    Program.logger.LogInformation(message);
                }
                else if (topic == "main telemetry")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\mainTelemetry.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "power telemetry")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\powerTelemetry.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "fault")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\fault.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "warning")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\warning.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "activity")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\activity.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "valve")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\valve.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "datalogsplunk")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\dataLogSplunk.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "devicemetadata")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\devicemetadata.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else if (topic == "devicestatus")
                {
                    string dataStr = System.IO.File.ReadAllText("Messages\\devicestatus.json");
                    Dictionary<string, object> listOfMessages = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
                    if (listOfMessages.ContainsKey(messageType))
                        message = JsonConvert.SerializeObject(listOfMessages[messageType]);
                }
                else
                {
                    Program.logger.LogInformation("Topic name or Predefined Message name invalid");
                    message = null;
                }
                if (message != null)
                {
                    JObject obj = JObject.Parse(message);
                    obj["device_id"] = deviceId;
                    message = obj.ToString();
                }
            }
            catch (Exception e)
            {
                Program.logger.LogCritical(e.Message);
            }
            Program.logger.LogInformation(message);
            return message;
        }

        public void SendMessage(string message, bool updateTimestamp, string msgDeviceType, string topicType)
        {
            try
            {
                JObject obj = JObject.Parse(message);
                if (updateTimestamp == true)
                {
                    obj["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    message = obj.ToString();

                    if (obj.ContainsKey("eventParameters"))
                    {
                        JObject childObj = JObject.Parse(obj.SelectToken("eventParameters").ToString());
                        if (childObj.ContainsKey("TelemetryDataLog_1"))
                        {
                            string log1 = childObj["TelemetryDataLog_1"].ToString();
                            string[] substr = log1.Split('\t');
                            log1 = log1.Replace(substr[1], DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff"));
                            childObj["TelemetryDataLog_1"] = log1;
                        }
                        if (childObj.ContainsKey("TelemetryDataLog_2"))
                        {
                            string log2 = childObj["TelemetryDataLog_2"].ToString();
                            string[] substr = log2.Split('\t');
                            log2 = log2.Replace(substr[1], DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff"));
                            childObj["TelemetryDataLog_2"] = log2;
                        }
                        if (childObj.ContainsKey("TelemetryDataLog_3"))
                        {
                            string log3 = childObj["TelemetryDataLog_3"].ToString();
                            string[] substr = log3.Split('\t');
                            log3 = log3.Replace(substr[1], DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff"));
                            childObj["TelemetryDataLog_3"] = log3;
                        }
                        if (childObj.ContainsKey("TelemetryDataLog_4"))
                        {
                            string log4 = childObj["TelemetryDataLog_4"].ToString();
                            string[] substr = log4.Split('\t');
                            log4 = log4.Replace(substr[1], DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss.fff"));
                            childObj["TelemetryDataLog_4"] = log4;
                        }
                        obj["eventParameters"] = childObj;
                        message = obj.ToString();
                    }
                }
                var msg = new Message(Encoding.UTF8.GetBytes(message));
                msg.ContentEncoding = "utf-8";
                JObject parsedObject = JObject.Parse(message);

                string deviceType = msgDeviceType;
                msg.ContentType = "application/json";
                msg.Properties.Add("tenant", "epredia");
                msg.Properties.Add("device_id", parsedObject.GetValue("device_id").ToString().ToLower());
                msg.Properties.Add("device_type", deviceType.ToString());
                if (deviceType == "revostissueprocessor")
                {
                    switch (topicType.ToString())
                    {
                        case "updatemetadata":
                            msg.Properties.Add("request_type", "update_metadata");
                            break;
                        case "main telemetry":
                            msg.Properties.Add("request_type", "main_telemetry");
                            break;
                        case "power telemetry":
                            msg.Properties.Add("request_type", "power_telemetry");
                            break;
                        case "fault":
                            msg.Properties.Add("request_type", "fault_event");
                            break;
                        case "warning":
                            msg.Properties.Add("request_type", "warning_event");
                            break;
                        case "activity":
                            msg.Properties.Add("request_type", "activity_event");
                            break;
                        case "valve":
                            msg.Properties.Add("request_type", "valve_event");
                            break;
                        case "datalogsplunk":
                            msg.Properties.Add("request_type", "data_log");
                            break;
                        case "devicemetadata":
                            msg.Properties.Add("request_type", "device_metadata");
                            break;
                        case "devicestatus":
                            msg.Properties.Add("request_type", "device_status");
                            break;
                    }
                }
                deviceClient.SendEventAsync(msg);
            }
            catch (Exception e)
            {
                Program.logger.LogCritical(e.Message);
                throw e;
            }

        }
    }
}

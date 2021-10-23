using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace BootstrapService.Services
{
    public interface IDeviceSimulatorHelperService
    {
        public string GetDeviceId(object userMsg);
        bool ValidateJsonFormat(object userMsg, string topicType, string deviceId);
        Task RegisterDeviceToIoTHub(string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timeZone, string serviceRegion, string commercialRegion, string hubRegion, string environment);
        Task<IothubConnectionInfo> EnrollDeviceToDPS(JwtInfo jwtInfo, string environment);
        Task<string> RegisterDeviceToIoTHubForTest(JwtInfo jwtInfo, string environment, string testGuid);
        JwtInfo GetToken(string userId, string password, string deviceType, string deviceId);
        JwtInfo GetDeviceToken(string deviceId, string deviceType, string softwareVersion, string firmwareVersion, string timeZone, string serviceRegion, string commercialRegion, string hubRegion);
        string CreateToken(ClaimsIdentity claimsIdentity);
        Task DeleteDeviceAsync(string deviceId, string deviceType, string environment);
        Task ConnectDeviceToIoTHubAsync(string deviceId, string deviceType, string environment);
        void DisconnectDevice();
        SecurityProviderX509Certificate GetSecurityCertificate(string filePath, string secureString);
        Task CreateIoTDeviceClientAsync(SecurityProviderX509Certificate security, string deviceId, string iothubHostname);
        string UserSelectedTopic(string deviceId, string topicType, string messageType);
        void SendMessage(string message, bool updateTimestamp, string msgDeviceType, string topicType);
    }
}

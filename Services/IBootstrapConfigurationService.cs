using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace BootstrapService.Services
{
    public interface IBootstrapConfigurationService
    {
        List<string> GetEnrolledDevices(string environment);
        bool CheckIfDeviceIsEnrolled(string environment, string deviceId);
        void DeleteDeviceAsync(string deviceId, string environment);
        Task<IothubConnectionInfo> ProvisionDeviceAsync(string deviceId, string environment, string iothubname);
        Dictionary<string, string> ReadToken(string token);
        DecryptedData DecryptTokenAndRandomKey(string encryptedRandomKey, string encryptedToken);
        DecryptedData DecryptTokenAndRandomKeyV2(string encryptedRandomKey, string encryptedToken);
        Task<IothubConnectionInfo> ProvisionDeviceForTestAsync(string deviceId, string environment, string iothubendpoint);
        
    }
}

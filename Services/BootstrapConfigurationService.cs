using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Azure.Devices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client;
using System.Security;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System.Net;
using BootstrapService.Model;
using System.Web.Http;
using System.Net.Http;

namespace BootstrapService.Services
{
    public class BootstrapConfigurationService : IBootstrapConfigurationService
    {
        private static ApplicationSettingsModel applicationSettingsModel;
        public static SecureString secureString;
        public static Regex r1 = new Regex("^revos-uk-[a-z0-9-]+$");
        public static Regex r2 = new Regex("^rs[0-9]{8}[-][a-z0-9-]{8,32}$");
        public static Regex r3 = new Regex("^RS[0-9]{8}-[A-Z0-9]{8}-([A-Z0-9]{4}-){3}[A-Z0-9]{12}$");

        public BootstrapConfigurationService(ApplicationSettingsModel _applicationSettingsModel)
        {
            applicationSettingsModel = _applicationSettingsModel;
            secureString = new NetworkCredential("revos", applicationSettingsModel.Password).SecurePassword;

        }
        private static DateTimeOffset certificateExpiryDate;

        public List<string> GetEnrolledDevices(string environment)
        {
            Program.logger.LogInformation("Enrolled devices being fetched");
            List<string> devices = new List<string>();
            try
            {
                string connectionString = "";
                if (environment == "1")
                    connectionString = applicationSettingsModel.DEV_PROVISIONING_CONNECTION_STRING;
                else if (environment == "2")
                    connectionString = applicationSettingsModel.QA_PROVISIONING_CONNECTION_STRING;

                using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(connectionString);
                var dps_sample = new DPSEnrollmentService(provisioningServiceClient, Program.logger);
                List<QueryResult> queryResult = dps_sample.QueryIndividualEnrollmentsAsync().GetAwaiter().GetResult();
                foreach (QueryResult queryItem in queryResult)
                {
                    var items = queryItem.Items;
                    foreach (var item in items)
                    {
                        var Jitem = JObject.FromObject(item);
                        var deviceId = Jitem.Value<string>("deviceId");
                        devices.Add(deviceId);
                    }
                }
                Program.logger.LogInformation("Enrolled devices fetched");
                return devices;
            }
            catch (Exception e)
            {
                Program.logger.LogCritical(e.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(e.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
        }
        public bool CheckIfDeviceIsEnrolled(string environment, string deviceId)
        {
            Program.logger.LogInformation("Enrolled devices being fetched");
            List<string> devices = new List<string>();
            try
            {
                string connectionString = "";
                if (environment == "1")
                    connectionString = applicationSettingsModel.DEV_PROVISIONING_CONNECTION_STRING;
                else if (environment == "2")
                    connectionString = applicationSettingsModel.QA_PROVISIONING_CONNECTION_STRING;

                using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(connectionString);
                var dps_sample = new DPSEnrollmentService(provisioningServiceClient, Program.logger);
                bool queryResult = dps_sample.SearchForIndividualEnrollmentAsync(deviceId.ToLower()).GetAwaiter().GetResult();

                Program.logger.LogInformation("Enrolled devices fetched");
                return queryResult;
            }
            catch (Exception e)
            {
                Program.logger.LogCritical(e.Message);
                throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(e.Message, System.Text.Encoding.UTF8, "text/plain"),
                    StatusCode = HttpStatusCode.NotFound
                });
            }
        }

        public async void DeleteDeviceAsync(string deviceId, string environment)
        {
            string dpsConnectionString = "";
            string iotConnectionString = "";
            try
            {
                if (environment == "1")
                {
                    dpsConnectionString = applicationSettingsModel.DEV_PROVISIONING_CONNECTION_STRING;
                    iotConnectionString = applicationSettingsModel.DEV_IOTHUB_CONNECTION_STRING;
                }
                else if (environment == "2")
                {
                    iotConnectionString = applicationSettingsModel.QA_IOTHUB_CONNECTION_STRING;
                    dpsConnectionString = applicationSettingsModel.QA_PROVISIONING_CONNECTION_STRING;
                }
                using var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(dpsConnectionString);
                var dps_sample = new DPSEnrollmentService(provisioningServiceClient, Program.logger);
                dps_sample.DeleteIndividualEnrollmentAsync(deviceId).GetAwaiter().GetResult();
                using RegistryManager registryManager = RegistryManager.CreateFromConnectionString(iotConnectionString);
                Program.logger.LogInformation($"Removing device '{deviceId}' from IoTHub ");
                await registryManager.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                Program.logger.LogInformation($"Removed Device {deviceId} from IoTHub");
            }
            catch (Exception e)
            {
                Program.logger.LogInformation("Exception occured in DeleteDeviceAsync");
                Program.logger.LogCritical(e.Message);
            }
        }

        public async Task<IothubConnectionInfo> ProvisionDeviceAsync(string deviceId, string environment, string iothubname)
        {
            bool status = CheckIfDeviceIsEnrolled(environment, deviceId);
            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();
            if (status == false)
            {
                CreateCertificates(deviceId, secureString);
                string output = "";
                string connectionString = "";
                string idScope = "";
                if (environment == "1")
                {
                    connectionString = applicationSettingsModel.DEV_PROVISIONING_CONNECTION_STRING;
                    idScope = applicationSettingsModel.dev_IdScope;
                }
                else if (environment == "2")
                {
                    connectionString = applicationSettingsModel.QA_PROVISIONING_CONNECTION_STRING;
                    idScope = applicationSettingsModel.qa_IdScope;
                }

                if (VerifyRegistrationIdFormat(deviceId) == true)
                {
                    using (var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(connectionString))
                    {
                        var dpsService = new DPSEnrollmentService(provisioningServiceClient, Program.logger);
                        var X509RootCertPathVar = @$"Certificates\\{deviceId}.cer";
                        output = dpsService.CreateIndividualEnrollmentX509Async(deviceId, deviceId, X509RootCertPathVar, environment, iothubname).GetAwaiter().GetResult();
                    }
                    Program.logger.LogInformation(output);
                    Program.logger.LogInformation("Provisioning to DPS Done.\n");

                    if (output == "Provisioned")
                    {
                        //Creating X509Certicate for secure authorization from pfx file.
                        string pfxPath = $"Certificates\\{deviceId}.pfx";
                        Program.logger.LogInformation(pfxPath);
                        Program.logger.LogInformation(secureString.Length.ToString());
                        try
                        {
                            Program.logger.LogInformation(Directory.GetCurrentDirectory());
                            Program.logger.LogInformation(pfxPath);
                            var certificate = new X509Certificate2(pfxPath, secureString, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                            Program.logger.LogInformation(certificate.ToString());

                            var security = new SecurityProviderX509Certificate(certificate);
                            Program.logger.LogInformation(security.ToString());
                            //Using HTTP transport for device allocation in IoTHub.
                            var transport = new ProvisioningTransportHandlerHttp();
                            Program.logger.LogInformation("Creating ProvisioningDeviceClient");
                            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(applicationSettingsModel.GLOBAL_DEVICE_ENDPOINT, idScope, security, transport);
                            Program.logger.LogInformation("Done.");

                            //Assigning Device to IoTHub

                            Program.logger.LogInformation("Assigning Device to IoThub...");
                            DeviceRegistrationResult result = await provClient.RegisterAsync().ConfigureAwait(false);
                            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                            {
                                Program.logger.LogError(result.ErrorMessage);
                            }
                            else
                            {
                                Program.logger.LogInformation($"{result.Status} {result.DeviceId} Successfully!");
                                Program.logger.LogInformation($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");
                                Program.logger.LogInformation($"Registered Device {deviceId}");
                            }
                            iothubConnectionInfo.Assigned = true;
                            iothubConnectionInfo.AssignedIoTHubName = result.AssignedHub;
                            iothubConnectionInfo.PfxFile = Convert.ToBase64String(File.ReadAllBytes($"Certificates\\{deviceId}.pfx"));
                            iothubConnectionInfo.SecureString = applicationSettingsModel.Password;
                            iothubConnectionInfo.CertificateExpiryDate = certificateExpiryDate;
                            Program.logger.LogInformation("Device Provisioned!");

                        }
                        catch (Exception e)
                        {
                            Program.logger.LogInformation("Exception occured in ProvisionDeviceAsync method");
                            Program.logger.LogCritical(e.Message);
                            Program.logger.LogInformation(e.StackTrace);
                            DeleteDeviceAsync(deviceId, environment);
                        }
                    }
                    else
                    {
                        Program.logger.LogInformation("Device enrollment already exists!");
                        iothubConnectionInfo.Assigned = false;
                    }
                }
                else
                {
                    Program.logger.LogInformation("Device name is invalid!");
                    iothubConnectionInfo.Assigned = false;
                }
            }
            else
            {
                iothubConnectionInfo.Assigned = true;
            }
            return iothubConnectionInfo;
        }

        public async Task<IothubConnectionInfo> ProvisionDeviceForTestAsync(string deviceId, string environment, string iothubname)
        {
            bool status = CheckIfDeviceIsEnrolled(environment, deviceId);
            IothubConnectionInfo iothubConnectionInfo = new IothubConnectionInfo();
            if (status == false)
            {
                CreateCertificates(deviceId, secureString);
                string output = "";
                string connectionString = "";
                string idScope = "";
                if (environment == "1")
                {
                    connectionString = applicationSettingsModel.DEV_PROVISIONING_CONNECTION_STRING;
                    idScope = applicationSettingsModel.dev_IdScope;
                }
                else if (environment == "2")
                {
                    connectionString = applicationSettingsModel.QA_PROVISIONING_CONNECTION_STRING;
                    idScope = applicationSettingsModel.qa_IdScope;
                }

                if (VerifyRegistrationIdFormat(deviceId) == true)
                {
                    using (var provisioningServiceClient = ProvisioningServiceClient.CreateFromConnectionString(connectionString))
                    {
                        var dpsService = new DPSEnrollmentService(provisioningServiceClient, Program.logger);
                        var X509RootCertPathVar = @$"Certificates\\{deviceId}.cer";
                        output = dpsService.CreateIndividualEnrollmentX509Async(deviceId, deviceId, X509RootCertPathVar, environment, iothubname).GetAwaiter().GetResult();
                    }
                    Program.logger.LogInformation(output);
                    Program.logger.LogInformation("Provisioning to DPS Done.\n");

                    if (output == "Provisioned")
                    {
                        string pfxPath = $"Certificates\\{deviceId}.pfx";
                        Program.logger.LogInformation(pfxPath);
                        Program.logger.LogInformation(secureString.Length.ToString());
                        try
                        {
                            Program.logger.LogInformation(Directory.GetCurrentDirectory());
                            Program.logger.LogInformation(pfxPath);
                            var certificate = new X509Certificate2(pfxPath, secureString, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                            Program.logger.LogInformation(certificate.ToString());

                            var security = new SecurityProviderX509Certificate(certificate);
                            var transport = new ProvisioningTransportHandlerHttp();
                            Program.logger.LogInformation("Creating ProvisioningDeviceClient");
                            ProvisioningDeviceClient provClient = ProvisioningDeviceClient.Create(applicationSettingsModel.GLOBAL_DEVICE_ENDPOINT, idScope, security, transport);
                            Program.logger.LogInformation("Done.");

                            //Assigning Device to IoTHub

                            Program.logger.LogInformation("Assigning Device to IoThub...");
                            DeviceRegistrationResult result = await provClient.RegisterAsync().ConfigureAwait(false);
                            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
                            {
                                Program.logger.LogError(result.ErrorMessage);
                            }
                            else
                            {
                                Program.logger.LogInformation($"{result.Status} {result.DeviceId} Successfully!");
                                Program.logger.LogInformation($"ProvisioningClient AssignedHub: {result.AssignedHub}; DeviceID: {result.DeviceId}");
                                Program.logger.LogInformation($"Registered Device {deviceId}");
                            }
                            iothubConnectionInfo.Assigned = true;
                            iothubConnectionInfo.AssignedIoTHubName = result.AssignedHub;
                            iothubConnectionInfo.PfxFile = Convert.ToBase64String(File.ReadAllBytes($"Certificates\\{deviceId}.pfx"));
                            iothubConnectionInfo.SecureString = applicationSettingsModel.Password;
                            iothubConnectionInfo.CertificateExpiryDate = certificateExpiryDate;
                            Program.logger.LogInformation("Device Provisioned!");

                        }
                        catch (Exception e)
                        {
                            Program.logger.LogInformation("Exception occured in ProvisionDeviceAsync method");
                            Program.logger.LogCritical(e.Message);
                            Program.logger.LogInformation(e.StackTrace);
                            DeleteDeviceAsync(deviceId, environment);
                        }
                    }
                    else
                    {
                        Program.logger.LogInformation("Device enrollment already exists!");
                        iothubConnectionInfo.Assigned = false;
                    }
                }
                else
                {
                    Program.logger.LogInformation("Device name is invalid!");
                    iothubConnectionInfo.Assigned = false;
                }
            }
            else
            {
                iothubConnectionInfo.Assigned = true;
            }
            return iothubConnectionInfo;
        }

        private void CreateCertificates(string deviceId, SecureString secureString)
        {
            try
            {
                Program.logger.LogInformation("Creating .cer and .pfx Files...");
                certificateExpiryDate = DateTimeOffset.Now.AddYears(10);
                var rsa = RSA.Create(2048);
                var req = new CertificateRequest($"CN={deviceId}, O=TEST, C=IND", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var cert = req.CreateSelfSigned(DateTimeOffset.Now, certificateExpiryDate);
                Program.logger.LogInformation("before writing");
                // Create PFX (PKCS #12) with private key
                File.WriteAllBytes($"Certificates\\{deviceId}.pfx", cert.Export(X509ContentType.Pfx, secureString));

                // Create Base 64 encoded CER (public key only)
                File.WriteAllText($"Certificates\\{deviceId}.cer",
                    "-----BEGIN CERTIFICATE-----\r\n"
                    + Convert.ToBase64String(cert.RawData)
                    + "\r\n-----END CERTIFICATE-----");
                Program.logger.LogInformation("Files Created!");
            }
            catch (Exception ex)
            {
                Program.logger.LogInformation("Exception occured in CreateCertificates");
                Program.logger.LogCritical(ex.Message);
            }
        }
        private static bool VerifyRegistrationIdFormat(string v)
        {
            if (!r1.IsMatch(v.ToLower()) && !r2.IsMatch(v.ToLower()) && !r3.IsMatch(v.ToUpper()))
            {
                Program.logger.LogInformation("Invalid registrationId: The registration ID is alphanumeric, lowercase, and may contain hyphens");
                return false;
            }
            return true;
        }

        public Dictionary<string, string> ReadToken(string token)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtSecurityToken = tokenHandler.ReadJwtToken(token);
            var claims = jwtSecurityToken.Claims.ToList();
            foreach (Claim claim in claims)
            {
                values.Add(claim.Type, claim.Value);
            }
            return values;
        }

        public DecryptedData DecryptTokenAndRandomKey(string encryptedRandomKey, string encryptedToken)
        {
            DecryptedData decryptedData = new DecryptedData();
            var rsa = RSAKeys.ImportPrivateKey(File.ReadAllText("privateKey.pem"));
            var _privateKey = rsa.ToXmlString(true);
            decryptedData.DecryptedRandomKey = RSA1.Decrypt(encryptedRandomKey, _privateKey, 2048);
            decryptedData.DecryptedToken = Rijndael.Decrypt(encryptedToken, decryptedData.DecryptedRandomKey);
            return decryptedData;
        }
        public DecryptedData DecryptTokenAndRandomKeyV2(string encryptedRandomKey, string encryptedToken)
        {
            DecryptedData decryptedData = new DecryptedData();
            var rsa = RSAKeys.ImportPrivateKey(File.ReadAllText("privateKey.pem"));
            var _privateKey = rsa.ToXmlString(true);
            decryptedData.DecryptedRandomKey = RSA1.Decrypt(encryptedRandomKey, _privateKey, 2048);
            byte[] decryptedByteArray = RSA1.DecryptAsByteArray(encryptedRandomKey, _privateKey, 2048);
            decryptedData.DecryptedToken = Rijndael.DecryptV2(encryptedToken, decryptedData.DecryptedRandomKey, decryptedByteArray);
            return decryptedData;
        }
    }
}

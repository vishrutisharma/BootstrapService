using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;

namespace BootstrapService
{
    public class DPSEnrollmentService
    {
        private const ProvisioningStatus OptionalProvisioningStatus = ProvisioningStatus.Enabled;
        private readonly ProvisioningServiceClient _provisioningServiceClient;
        private readonly ILogger<Program> _logger;

        public DPSEnrollmentService(ProvisioningServiceClient provisioningServiceClient, ILogger<Program> logger)
        {
            _provisioningServiceClient = provisioningServiceClient;
            _logger = logger;
        }

        public async Task<List<QueryResult>> QueryIndividualEnrollmentsAsync()
        {
            try
            {
                QueryResult queryResult = null;
                List<QueryResult> list = new List<QueryResult>();
                _logger.LogInformation("\nCreating a query for enrollments...");
                QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments");
                using Query query = _provisioningServiceClient.CreateIndividualEnrollmentQuery(querySpecification);
                while (query.HasNext())
                {
                    _logger.LogInformation("\nQuerying the next enrollments...");
                    queryResult = await query.NextAsync().ConfigureAwait(false);
                    _logger.LogInformation(JsonConvert.SerializeObject(queryResult));
                    list.Add(queryResult);
                }
                return list;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                throw e;
            }
        }
        public async Task<bool> SearchForIndividualEnrollmentAsync(string deviceId)
        {
            try
            {
                _logger.LogInformation("\nCreating a query for enrollments...");
                QuerySpecification querySpecification = new QuerySpecification("SELECT * FROM enrollments WHERE deviceId =" + deviceId);
                //DeviceRegistrationState details = await _provisioningServiceClient.GetDeviceRegistrationStateAsync(deviceId);
                IndividualEnrollment query = await _provisioningServiceClient.GetIndividualEnrollmentAsync(deviceId);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                return false;
            }
        }

        public async Task<string> CreateIndividualEnrollmentX509Async(string RegistrationId, string OptionalDeviceId, string X509RootCertPathVar, string environment, string iothubHostName)
        {
            _logger.LogInformation("\nCreating a new individualEnrollment...");
            var deviceCertificate = new X509Certificate2(X509RootCertPathVar);
            Attestation attestation = X509Attestation.CreateFromClientCertificates(deviceCertificate);

            IndividualEnrollment individualEnrollment = new IndividualEnrollment(RegistrationId, attestation)
            {
                // The following parameters are optional:
                DeviceId = OptionalDeviceId,
                ProvisioningStatus = OptionalProvisioningStatus,
                IotHubHostName = iothubHostName
            };
            _logger.LogInformation("\nAdding new individualEnrollment...");
            try
            {
                IndividualEnrollment individualEnrollmentResult = await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                _logger.LogInformation(JsonConvert.SerializeObject(individualEnrollmentResult));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
                return "Not provisioned";
            }
            return "Provisioned";
        }

        public async Task<IndividualEnrollment> GetIndividualEnrollmentInfoAsync(string RegistrationId)
        {
            _logger.LogInformation("\nGetting the individualEnrollment information...");
            IndividualEnrollment getResult =
                await _provisioningServiceClient.GetIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
            _logger.LogInformation(JsonConvert.SerializeObject(getResult));

            return getResult;
        }

        public async Task UpdateIndividualEnrollmentAsync(string OptionalDeviceId, string RegistrationId)
        {
            var individualEnrollment = await GetIndividualEnrollmentInfoAsync(RegistrationId).ConfigureAwait(false);
            individualEnrollment.DeviceId = OptionalDeviceId;
            try
            {
                IndividualEnrollment individualEnrollmentResult =
                await _provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
                _logger.LogInformation(JsonConvert.SerializeObject(individualEnrollmentResult));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
            }
        }

        public async Task DeleteIndividualEnrollmentAsync(string RegistrationId)
        {
            try
            {
                _logger.LogInformation("\nDeleting the individualEnrollment...");
                await _provisioningServiceClient.DeleteIndividualEnrollmentAsync(RegistrationId).ConfigureAwait(false);
                _logger.LogInformation($"Deleted Device {RegistrationId} from DPS");
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
            }
        }
    }
}
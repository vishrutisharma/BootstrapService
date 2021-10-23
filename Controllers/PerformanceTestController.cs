using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BootstrapService.Model;
using BootstrapService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BootstrapService.Controllers
{

    [ApiController]
    public class PerformanceTestController : ControllerBase
    {
        private readonly IBootstrapConfigurationService _bootstrapConfigurationService;
        private readonly IDeviceSimulatorHelperService deviceSimulatorHelper;
        private readonly ICosmosTableService _cosmosTableService;
        private readonly IFileService _fileService;
        private static string sendMessageURL = "https://eprediabootstrapservice.azurewebsites.net/api/updateMetadataForTest";
        private static string sendMessageURLWithoutTimer = "https://eprediabootstrapservice.azurewebsites.net/api/updateMetadataForTestWithoutTimer";
        private static string provisionDeviceURL = "https://eprediabootstrapservice.azurewebsites.net/api/provisionSingleTestDevice";
        private static string linkUserToTestDeviceURL = "https://eprediabootstrapservice.azurewebsites.net/api/linkUserToTestDevice";
        private static string deprovisionDeviceURL = "https://eprediabootstrapservice.azurewebsites.net/api/deprovisionSingleDevice";
        private string testAssignedIotHub = "";
        private string testMessage = "";
        private string testTopicType = "";
        private string testEnv = "1";
        private string testDeviceId;
        private string testGUID;
        private SecurityProviderX509Certificate securityProviderX509Certificate;
        System.Timers.Timer testTimer = new System.Timers.Timer();
        public PerformanceTestController(IBootstrapConfigurationService bootstrapConfigurationService, IDeviceSimulatorHelperService deviceSimulatorHelperService, ICosmosTableService cosmosTableService, IFileService fileService)
        {
            _bootstrapConfigurationService = bootstrapConfigurationService;
            deviceSimulatorHelper = deviceSimulatorHelperService;
            _cosmosTableService = cosmosTableService;
            _fileService = fileService;
        }

        [HttpPost("api/provisionTestDevices")]
        public async Task<IActionResult> ProvisionDevicesForTestAsync(string devCount, int timePeriodInMinutes, string environment)
        {
            Program.logger.LogInformation($"Provisioning Begins : " + DateTime.Now.ToString());
            string testGuid = Guid.NewGuid().ToString();
            try
            {
                Random random = new Random();
                Guid obj = new Guid();
                string id;
                string env = environment == null ? "1" : environment;

                var devRegex = new Regex("^rs[0-9]{8}-[a-z0-9]{8}-([a-z0-9]{4}-){3}[a-z0-9]{12}$");
                List<string> devices = new List<string>();
                await _cosmosTableService.InsertPerformanceTestGuid(environment, testGuid);
                while (devices.Count < Convert.ToInt32(devCount))
                {
                    int delay = random.Next(0, timePeriodInMinutes);
                    obj = Guid.NewGuid();
                    id = random.Next(11111111, 99999999).ToString();
                    string deviceName = "rs" + id + "-" + obj;
                    if (devRegex.IsMatch(deviceName.ToLower()))
                    {
                        JwtInfo jwtInfo = deviceSimulatorHelper.GetDeviceToken(deviceName, "revostissueprocessor", "1.0.0", "1.0.0", "+05:30", "00", "00", "1");
                        ProvisionPerformanceTestDeviceRequestModel requestModel = new ProvisionPerformanceTestDeviceRequestModel
                        {
                            EncryptedRandomKey = jwtInfo.EncryptedRandomKey,
                            EncryptedToken = jwtInfo.EncryptedToken,
                            Environment = env,
                            TestGuid = testGuid,
                            Delay = delay
                        };
                        Program.logger.LogInformation($"Delay is : " + delay);
                        Program.logger.LogInformation($"Sending request for : " + deviceName + "TimeStamp" + DateTime.Now.ToString());
                        Task.Run(() =>
                        {
                            DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(provisionDeviceURL, requestModel);
                        });
                        //await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(provisionDeviceURL, requestModel).ConfigureAwait(false);
                        devices.Add(deviceName);
                        Program.logger.LogInformation($"Provisioning request sent for : " + deviceName + "TimeStamp" + DateTime.Now.ToString());
                    }
                }
                Program.logger.LogInformation($"Provisioning Ends : " + DateTime.Now.ToString());
                return Ok(new ProvisionDeviceForTestResponseModel
                {
                    TestGuid = testGuid
                });
            }
            catch (Exception ex)
            {
                Program.logger.LogInformation($"Provisioning Ends : " + DateTime.Now.ToString());
                Program.logger.LogInformation($"Caught Exception : " + ex.Message);
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Test GUID : " + testGuid + " Exception : " + ex.Message
                });
            }
        }

        [HttpPost("api/linkUsersToTestDevices")]
        public async Task<IActionResult> LinkUserToTestDevice([FromBody] List<UsersDetailsModel> input, string testGuid, string environment)
        {
            try
            {
                if (input.Count == 0)
                {
                    return BadRequest();
                }
                else
                {
                    int i = 0;
                    List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                    foreach (IotHubPairDataForTest item in response)
                    {
                        int j = i % input.Count;
                        LinkUserDeviceRequestModel linkUserDevice = new LinkUserDeviceRequestModel
                        {
                            DeviceId = item.DeviceId,
                            DeviceType = "revostissueprocessor",
                            Username = input[j].Username,
                            Password = input[j].Password
                        };
                        Task.Run(() =>
                        {
                            DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(linkUserToTestDeviceURL + "?environment=" + environment + "&testGuid=" + testGuid, linkUserDevice);
                        });
                        //await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(linkUserToTestDeviceURL + "?environment=" + environment + "&testGuid=" + testGuid, linkUserDevice).ConfigureAwait(false);
                        i = i + 1;
                    }
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/provisionedTestDevicesCount")]
        public async Task<IActionResult> GetProvisionedTestDevicesCount(string testGuid, string environment)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                return Ok(new GetProvisionedDeviceCountResponse
                {
                    ProvisionedDeviceCount = response.Count.ToString()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/sendMessageForTest")]
        public async Task<IActionResult> SendMessageForDevicesByTestGuid(string testGuid, string environment, string topicType, string predefinedMessageName, string selectedFrequencyInSeconds)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                foreach (IotHubPairDataForTest item in response)
                {
                    SendTestMessageRequestModel input = new SendTestMessageRequestModel
                    {
                        AssignedIoTHubName = item.AssignedIoTHubName,
                        DeviceId = item.DeviceId,
                        DeviceType = "revostissueprocssor",
                        PfxFile = item.PfxFile,
                        SecureString = item.SecureString,
                        Frequency = selectedFrequencyInSeconds,
                        TopicType = topicType.ToLower(),
                        CertificateExpiryDate = item.CertificateExpiryDate,
                        TestGuid = testGuid,
                        MessageType = predefinedMessageName
                    };
                    await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(sendMessageURL + "?environment=" + environment, input).ConfigureAwait(false);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }
        [HttpPost("api/sendMessageForTestWithoutTimer")]
        public async Task<IActionResult> SendMessageForDevicesByTestGuidWithoutTimer(string testGuid, string environment, string topicType, string predefinedMessageName, string selectedFrequencyInSeconds)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                foreach (IotHubPairDataForTest item in response)
                {
                    string filePath = _fileService.FetchFiles(item.PfxFile, item.DeviceId);
                    var security = deviceSimulatorHelper.GetSecurityCertificate(filePath, item.SecureString);
                    var auth = new DeviceAuthenticationWithX509Certificate(item.DeviceId, (security as SecurityProviderX509).GetAuthenticationCertificate());

                    DeviceClient deviceClient = DeviceClient.Create(item.AssignedIoTHubName, auth, Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);
                    await deviceClient.OpenAsync().ConfigureAwait(false);                       
                    SendTestMessageRequestModel input = new SendTestMessageRequestModel
                    {
                        AssignedIoTHubName = item.AssignedIoTHubName,
                        DeviceId = item.DeviceId,
                        DeviceType = "revostissueprocssor",
                        PfxFile = item.PfxFile,
                        SecureString = item.SecureString,
                        Frequency = selectedFrequencyInSeconds,
                        TopicType = topicType.ToLower(),
                        CertificateExpiryDate = item.CertificateExpiryDate,
                        TestGuid = testGuid,
                        MessageType = predefinedMessageName,
                        DeviceClient = deviceClient
                    };
                    await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(sendMessageURLWithoutTimer + "?environment=" + environment, input).ConfigureAwait(false);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/deprovisionDevicesByTestGuid")]
        public async Task<IActionResult> DeprovisionDevicesByTestGuid(string testGuid, string environment)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                foreach (IotHubPairDataForTest item in response)
                {
                    //DeprovisionDeviceByTestGuidRequestModel requestModel = new DeprovisionDeviceByTestGuidRequestModel
                    //{
                    //    DeviceId = item.DeviceId,
                    //    SecureString = item.SecureString,
                    //    AssignedIoTHubName = item.AssignedIoTHubName,
                    //    CertificateExpiryDate = item.CertificateExpiryDate,
                    //    PfxFile = item.PfxFile,
                    //    UserLinkStatus = item.UserLinkStatus,
                    //    DeviceSendStatus = item.DeviceSendStatus,
                    //    TestGuid = testGuid,
                    //    Environment = environment
                    //};
                    //HttpResponseMessage httpResponse = await DeviceSimulatorHelperService.httpClient.PostAsJsonAsync(deprovisionDeviceURL, requestModel).ConfigureAwait(false);                   
                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(environment, item.DeviceId.ToLower());
                    if (status == true)
                    {
                        _bootstrapConfigurationService.DeleteDeviceAsync(item.DeviceId, environment);
                        HttpRequestMessage httpRequestMessage;
                        if (environment == "1")
                            httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLDev + "/" + item.DeviceId + "/" + "revostissueprocessor/deProvisionDevice");
                        else
                            httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLTest + "/" + item.DeviceId + "/" + "revostissueprocessor/deProvisionDevice");
                        HttpResponseMessage result = await DeviceSimulatorHelperService.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    }
                    await _cosmosTableService.DeleteDeviceProvisionedForTest(item, environment);

                }
                PerformanceTestGuidEntity request = new PerformanceTestGuidEntity("epredia_performancetest", testGuid)
                {
                    Status = "TestCompleted"
                };
                await _cosmosTableService.UpdatePerformanceTestGuid(request, environment);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        //[HttpPost("api/deprovisionSingleDevice")]
        //public async Task<IActionResult> DeprovisionDevice(DeprovisionDeviceByTestGuidRequestModel input)
        //{
        //    try
        //    {
        //        bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(input.Environment, input.DeviceId.ToLower());
        //        if (status == true)
        //        {
        //            _bootstrapConfigurationService.DeleteDeviceAsync(input.DeviceId, input.Environment);
        //            HttpRequestMessage httpRequestMessage;
        //            if (input.Environment == "1")
        //                httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLDev + "/" + input.DeviceId + "/" + "revostissueprocessor/deProvisionDevice");
        //            else
        //                httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, DeviceSimulatorHelperService.deleteDeviceURLTest + "/" + input.DeviceId + "/" + "revostissueprocessor/deProvisionDevice");
        //            HttpResponseMessage result = await DeviceSimulatorHelperService.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
        //        }
        //        IotHubPairDataForTest pairDataForTest = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
        //        {
        //            AssignedIoTHubName = input.AssignedIoTHubName,
        //            DeviceId = input.DeviceId,
        //            SecureString = input.SecureString,
        //            PfxFile = input.PfxFile,
        //            DeviceSendStatus = input.DeviceSendStatus,
        //            CertificateExpiryDate = input.CertificateExpiryDate
        //        };
        //        await _cosmosTableService.DeleteDeviceProvisionedForTest(pairDataForTest, input.Environment);

        //        //PerformanceTestGuidEntity request = new PerformanceTestGuidEntity("epredia_performancetest", input.TestGuid)
        //        //{
        //          //  Status = "TestCompleted"
        //        //};
        //        //await _cosmosTableService.UpdatePerformanceTestGuid(request, input.TestGuid);

        //        return Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new ErrorResponseModel
        //        {
        //            ErrorMessage = ex.Message
        //        });
        //    }
        //}

        [HttpPost("api/getTestStatus")]
        public async Task<IActionResult> GetTestStatus(string testGuid, string environment)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                if (response.Count != 0)
                {
                    List<GetSendStatusResponse> result = new List<GetSendStatusResponse>();
                    foreach (IotHubPairDataForTest item in response)
                    {
                        GetSendStatusResponse getSendStatus = new GetSendStatusResponse
                        {
                            DeviceId = item.DeviceId,
                            DeviceSendStatus = item.DeviceSendStatus,
                            UserLinkStatus = item.UserLinkStatus
                        };
                        result.Add(getSendStatus);
                    }
                    return Ok(result);
                }
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "No records found for this Test GUID"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }
        [HttpPost("api/getStatusOfAllTests")]
        public async Task<IActionResult> GetStatusForAllTests(string environment)
        {
            try
            {
                List<PerformanceTestGuidEntity> response = await _cosmosTableService.GetPerformanceTestGuid(environment);
                if (response.Count != 0)
                {
                    List<TestGuidStatusRepresentationModel> result = new List<TestGuidStatusRepresentationModel>();
                    foreach (PerformanceTestGuidEntity item in response)
                    {
                        TestGuidStatusRepresentationModel getSendStatus = new TestGuidStatusRepresentationModel
                        {
                            TestGUID = item.RowKey,
                            Status = item.Status
                        };
                        result.Add(getSendStatus);
                    }
                    return Ok(result);
                }
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "No records found"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }
        [HttpPost("api/stopTestExecution")]
        public async Task<IActionResult> StopTestExecution(string testGuid, string environment)
        {
            try
            {
                List<IotHubPairDataForTest> response = await _cosmosTableService.GetProvisionedDeviceCountForTestId(testGuid, environment);
                if (response.Count != 0)
                {
                    foreach (IotHubPairDataForTest item in response)
                    {
                        IotHubPairDataForTest request = new IotHubPairDataForTest("epredia_performancetest_" + testGuid, item.DeviceId)
                        {
                            DeviceId = item.DeviceId,
                            DeviceSendStatus = "Stopped",
                            AssignedIoTHubName = item.AssignedIoTHubName,
                            SecureString = item.SecureString,
                            PfxFile = item.PfxFile,
                            CertificateExpiryDate = item.CertificateExpiryDate
                        };
                        await _cosmosTableService.UpdateDeviceProvisionedForTest(request, environment);
                    }
                    return Ok();
                }
                return BadRequest();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/updateMetadataForTest")]
        public async Task<IActionResult> SendMessageForTest([FromQuery] string environment, [FromBody] SendTestMessageRequestModel input)
        {
            try
            {
                testDeviceId = input.DeviceId;
                testGUID = input.TestGuid;
                testEnv = environment;
                testTopicType = input.TopicType;
                testAssignedIotHub = input.AssignedIoTHubName;
                string filePath = _fileService.FetchFiles(input.PfxFile, input.DeviceId);
                IotHubPairDataForTest request = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                {
                    DeviceId = input.DeviceId,
                    DeviceSendStatus = "Sending",
                    AssignedIoTHubName = input.AssignedIoTHubName,
                    SecureString = input.SecureString,
                    PfxFile = input.PfxFile,
                    CertificateExpiryDate = input.CertificateExpiryDate
                };
                await _cosmosTableService.UpdateDeviceProvisionedForTest(request, environment);
                var security = deviceSimulatorHelper.GetSecurityCertificate(filePath, input.SecureString);
                securityProviderX509Certificate = security;
                await deviceSimulatorHelper.CreateIoTDeviceClientAsync(security, input.DeviceId, input.AssignedIoTHubName);
                testMessage = deviceSimulatorHelper.UserSelectedTopic(input.DeviceId, input.TopicType, input.MessageType);

                if (DeviceSimulatorHelperService.deviceClient == null || testMessage == null)
                {
                    IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                    {
                        DeviceId = input.DeviceId,
                        DeviceSendStatus = "Stopped",
                        AssignedIoTHubName = input.AssignedIoTHubName,
                        SecureString = input.SecureString,
                        PfxFile = input.PfxFile,
                        CertificateExpiryDate = input.CertificateExpiryDate
                    };
                    await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, environment);
                    return Ok(new ErrorResponseModel
                    {
                        ErrorMessage = "Device Client null"
                    });
                }
                if (input.TopicType.ToLower() == "main telemetry")
                {
                    input.Frequency = "30";
                }
                testTimer = new System.Timers.Timer(Convert.ToInt64(input.Frequency) * 1000);
                System.Timers.Timer stop_timer = new System.Timers.Timer(20000);
                // Hook up the Elapsed event for the timer. 
                testTimer.Elapsed += OnTimedEvent;
                testTimer.AutoReset = true;
                testTimer.Enabled = true;

                stop_timer.Elapsed += StopTimerEventAsync;
                stop_timer.AutoReset = true;
                stop_timer.Enabled = true;
                return Ok();

            }
            catch (Exception ex)
            {
                IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                {
                    DeviceId = input.DeviceId,
                    DeviceSendStatus = "Exception Occured : " + ex.Message
                };
                await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, testEnv);
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/updateMetadataForTestWithoutTimer")]
        public async Task<IActionResult> SendMessageForTestWithoutTimer([FromQuery] string environment, [FromBody] SendTestMessageRequestModel input)
        {
            try
            {
                IotHubPairDataForTest request = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                {
                    DeviceId = input.DeviceId,
                    DeviceSendStatus = "Sending",
                    AssignedIoTHubName = input.AssignedIoTHubName,
                    SecureString = input.SecureString,
                    PfxFile = input.PfxFile,
                    CertificateExpiryDate = input.CertificateExpiryDate
                };
                string message = deviceSimulatorHelper.UserSelectedTopic(input.DeviceId, input.TopicType, input.MessageType);

                if (input.DeviceClient == null || message == null)
                {
                    IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                    {
                        DeviceId = input.DeviceId,
                        DeviceSendStatus = "Stopped",
                        AssignedIoTHubName = input.AssignedIoTHubName,
                        SecureString = input.SecureString,
                        PfxFile = input.PfxFile,
                        CertificateExpiryDate = input.CertificateExpiryDate
                    };
                    await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, environment);
                    return Ok(new ErrorResponseModel
                    {
                        ErrorMessage = "Device Client null"
                    });
                }
                if (input.TopicType.ToLower() == "main telemetry")
                {
                    Thread.Sleep(30 * 1000);
                    SendMessageByDeviceClient(message, true, "revostissueprocessor", input.TopicType, input.DeviceClient);
                }
                else
                {
                    SendMessageByDeviceClient(message, true, "revostissueprocessor", input.TopicType, input.DeviceClient);
                }
                return Ok();

            }
            catch (Exception ex)
            {
                IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + input.TestGuid, input.DeviceId)
                {
                    DeviceId = input.DeviceId,
                    DeviceSendStatus = "Exception Occured : " + ex.Message
                };
                await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, testEnv);
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }
        private async void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (DeviceSimulatorHelperService.deviceClient == null)
                {
                    await deviceSimulatorHelper.CreateIoTDeviceClientAsync(securityProviderX509Certificate, testDeviceId, testAssignedIotHub);
                }
                deviceSimulatorHelper.SendMessage(testMessage, true, "revostissueprocessor", testTopicType);
            }
            catch (Exception ex)
            {
                IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + testGUID, testDeviceId)
                {
                    DeviceId = testDeviceId,
                    DeviceSendStatus = "Exception Occured : " + ex.Message
                };
                await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, testEnv);
            }
        }
        private async void StopTimerEventAsync(Object source, System.Timers.ElapsedEventArgs e)
        {
            IotHubPairDataForTest response = await _cosmosTableService.GetProvisionedDeviceCountForDeviceId(testGUID, testDeviceId, testEnv);
            if (response.DeviceSendStatus == "Stopped")
            {
                await DeviceSimulatorHelperService.deviceClient.CloseAsync();
                DeviceSimulatorHelperService.deviceClient = null;
                testTimer.Enabled = false;
            }
        }
        [HttpPost("api/linkUserToTestDevice")]
        public async Task<IActionResult> PostLinkUserToTestDevice([FromBody] LinkUserDeviceRequestModel input, string environment, string testGuid)
        {
            if (input.Username != "" && input.Password != "" && input.DeviceId != "" && input.DeviceType != "")
            {
                List<Model.Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (DeviceSimulatorHelperService.deviceTypes.Contains(input.DeviceType.ToLower()))
                {
                    string env = environment == null ? "1" : environment;

                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, input.DeviceId.ToLower());
                    if (status == true)
                    {
                        JwtInfo jwtInfo = deviceSimulatorHelper.GetToken(input.Username, input.Password, input.DeviceType.ToLower(), input.DeviceId.ToLower());
                        try
                        {
                            string userDeviceLinkURL = "";
                            if (env == "1")
                                userDeviceLinkURL = DeviceSimulatorHelperService.userDeviceLinkingURL;
                            else if (env == "2")
                                userDeviceLinkURL = DeviceSimulatorHelperService.userDeviceLinkingTestURL;
                            // Making API Call to User-Linking Service
                            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, userDeviceLinkURL);
                            DeviceSimulatorHelperService.httpClient.DefaultRequestHeaders.Clear();
                            DeviceSimulatorHelperService.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwtInfo.EncryptedToken);
                            DeviceSimulatorHelperService.httpClient.DefaultRequestHeaders.Add("encryptedRandomKey", jwtInfo.EncryptedRandomKey);
                            HttpResponseMessage result = await DeviceSimulatorHelperService.httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                            if (result.StatusCode == HttpStatusCode.OK)
                            {
                                IotHubPairDataForTest request2 = new IotHubPairDataForTest("epredia_performancetest_" + testGuid, input.DeviceId)
                                {
                                    DeviceId = input.DeviceId,
                                    UserLinkStatus = "linked"
                                };
                                await _cosmosTableService.UpdateDeviceProvisionedForTest(request2, environment);
                                return Ok(new LinkUserDeviceResponseModel
                                {
                                    DeviceId = input.DeviceId,
                                    DeviceType = input.DeviceType,
                                    UserName = input.Username,
                                    UserLinked = "Success"
                                });
                            }
                            else if (result.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                return Unauthorized(new LinkUserDeviceResponseModel
                                {
                                    DeviceId = input.DeviceId,
                                    DeviceType = input.DeviceType,
                                    UserName = input.Username,
                                    UserLinked = "Invalid Username/ Password"
                                });
                            }
                            else
                            {
                                return StatusCode(500);
                            }
                        }
                        catch (Exception ex)
                        {
                            return BadRequest(new ErrorResponseModel
                            {
                                ErrorMessage = ex.Message
                            });
                        }
                    }
                    else
                    {
                        return BadRequest(new ErrorResponseModel
                        {
                            ErrorMessage = "Device Not Provisioned"
                        });
                    }
                }
                else
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Device Type Invalid"
                    });
                }
            }
            else
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Empty/Incorrect Payload"
                });
            }
        }
        [HttpPost("api/provisionSingleTestDevice")]
        public async Task<IActionResult> PostProvisionSingleDevice([FromBody] ProvisionPerformanceTestDeviceRequestModel input)
        {
            try
            {
                Thread.Sleep(input.Delay * 60000);
                JwtInfo jwtInfo = new JwtInfo
                {
                    EncryptedRandomKey = input.EncryptedRandomKey,
                    EncryptedToken = input.EncryptedToken
                };
                await deviceSimulatorHelper.RegisterDeviceToIoTHubForTest(jwtInfo, input.Environment, input.TestGuid);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }
        [HttpPost("api/provisionSingleTestDeviceWithoutDelay")]
        public async Task<IActionResult> PostProvisionSingleDeviceWithoutDelay([FromBody] ProvisionPerformanceTestDeviceRequestModel input)
        {
            try
            {
                JwtInfo jwtInfo = new JwtInfo
                {
                    EncryptedRandomKey = input.EncryptedRandomKey,
                    EncryptedToken = input.EncryptedToken
                };
                await deviceSimulatorHelper.RegisterDeviceToIoTHubForTest(jwtInfo, input.Environment, input.TestGuid);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        private void SendMessageByDeviceClient(string message, bool updateTimestamp, string msgDeviceType, string topicType, DeviceClient deviceClient)
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

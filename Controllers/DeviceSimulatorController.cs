using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using BootstrapService.Model;
using System.Timers;
using BootstrapService.Services;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Linq;

namespace BootstrapService.Controllers
{
    [ApiController]
    public class DeviceSimulatorController : ControllerBase
    {
        private readonly IBootstrapConfigurationService _bootstrapConfigurationService;
        private readonly IDeviceSimulatorHelperService deviceSimulatorHelper;
        private readonly ICosmosTableService _cosmosTableService;
        private readonly IFileService _fileService;
        private bool updateTimeStamp = true;
        private string msgDeviceType = "";
        private string msgTopicType = "";


        public DeviceSimulatorController(IFileService fileService, IBootstrapConfigurationService bootstrapConfigurationService, IDeviceSimulatorHelperService deviceSimulatorHelperService, ICosmosTableService cosmosTableService)
        {
            _bootstrapConfigurationService = bootstrapConfigurationService;
            deviceSimulatorHelper = deviceSimulatorHelperService;
            _fileService = fileService;
            _cosmosTableService = cosmosTableService;
        }

        [HttpPost("api/deviceProvision")]
        public async Task<IActionResult> PostDeviceProvision([FromBody] DeviceProvisionRequestModel device, string environment)
        {
            try
            {
                if (device.DeviceId == "" || !DeviceSimulatorHelperService.versionRegex.IsMatch(device.DeviceSoftwareVersion) || !DeviceSimulatorHelperService.versionRegex.IsMatch(device.DeviceFirmwareVersion) || device.DeviceTimeZone == "" || !DeviceSimulatorHelperService.timezoneRegex.IsMatch(device.DeviceTimeZone) || device.DeviceType == "")
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Incorrect Payload"
                    });
                // Checks if a value is given for region then it should be complaint with regex
                if ((device.ServiceRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(device.ServiceRegion)) || (device.CommercialRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(device.CommercialRegion)) || (device.HubRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(device.HubRegion)))
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Incorrect Payload"
                    });
                }
                string iothub = "";
                if (device.HubRegion != "")
                    iothub = await _cosmosTableService.GetIotHubEndpoint(device.HubRegion, environment);
                if (device.HubRegion != "" && iothub == "")
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Incorrect Hub region"
                    });
                }
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (DeviceSimulatorHelperService.deviceTypes.Contains(device.DeviceType.ToLower()))
                {
                    string env = environment == null ? "1" : environment;

                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, device.DeviceId.ToLower());
                    if (status == false)
                    {
                        await deviceSimulatorHelper.RegisterDeviceToIoTHub(device.DeviceId.ToLower(), device.DeviceType.ToLower(), device.DeviceSoftwareVersion, device.DeviceFirmwareVersion, device.DeviceTimeZone, device.ServiceRegion, device.CommercialRegion, device.HubRegion, env);
                        if (DeviceSimulatorHelperService.lastProvisionedDeviceId == device.DeviceId.ToLower())
                        {
                            return Ok(new DeviceProvisionResponseModel
                            {
                                DeviceId = device.DeviceId,
                                IotHubEndPoint = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.AssignedIoTHubName,
                                PfxKey = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.PfxFile,
                                SecureString = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.SecureString,
                                CertificateExpiryDate = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.CertificateExpiryDate
                            });
                        }
                        else
                        {
                            return BadRequest(new ErrorResponseModel
                            {
                                ErrorMessage = "Failed to provision device"
                            });
                        }
                    }
                    else
                    {
                        //Duplicate entry
                        return UnprocessableEntity(new ErrorResponseModel
                        {
                            ErrorMessage = "Device already Provisioned"
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
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpPost("api/linkUserToDevice")]
        public async Task<IActionResult> PostLinkUserToDevice([FromBody] LinkUserDeviceRequestModel input, string environment)
        {
            if (input.Username != "" && input.Password != "" && input.DeviceId != "" && input.DeviceType != "")
            {
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
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
                            Program.logger.LogCritical(ex.Message);
                            return StatusCode(500);
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

        [HttpPost("api/deviceDeprovision")]
        public async Task<IActionResult> PostDeviceDeprovision([FromBody] DeviceDeprovisionRequestModel input, string environment)
        {
            if (input.DeviceId == "" || input.DeviceType == "")
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Device Id/ Device Type Empty"
                });
            try
            {
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (DeviceSimulatorHelperService.deviceTypes.Contains(input.DeviceType.ToLower()))
                {
                    string env = environment == null ? "1" : environment;
                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, input.DeviceId.ToLower());
                    if (status == false)
                    {
                        return BadRequest(new ErrorResponseModel
                        {
                            ErrorMessage = "Device not provisioned"
                        });
                    }
                    await deviceSimulatorHelper.DeleteDeviceAsync(input.DeviceId.ToLower(), input.DeviceType.ToLower(), env).ConfigureAwait(false);

                    return Ok(new DeviceDeprovisionResponseModel
                    {
                        DeviceId = input.DeviceId,
                        DeviceType = input.DeviceType,
                        DeProvision = "Success"
                    });
                }

                else
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Device Type Invalid"
                    });
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

        [HttpPost("api/updateMetadata")]
        public async Task<IActionResult> PostMetaData([FromQuery] string deviceId, string deviceType, string topicType, string selectedFrequency, string topicPredefinedMessageName, string timerInSeconds, string environment)
        {
            if (deviceId == null || topicType == null || topicPredefinedMessageName == null)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Device not provisioned"
                });
            }
            if (deviceType == "")
            {
                msgDeviceType = "revostissueprocessor";
            }
            else
            {
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (DeviceSimulatorHelperService.deviceTypes.Contains(deviceType.ToLower()))
                {
                    msgDeviceType = deviceType.ToLower();
                }
                else
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Device Type not valid"
                    });
                }
            }
            string env = environment == null ? "1" : environment;
            bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceId.ToLower());
            if (status == false)
            {
                return Unauthorized(new ErrorResponseModel
                {
                    ErrorMessage = "Device not provisioned"
                });
            }
            if (DeviceSimulatorHelperService.deviceClient != null)
            {
                deviceSimulatorHelper.DisconnectDevice();
            }
            await deviceSimulatorHelper.ConnectDeviceToIoTHubAsync(deviceId.ToLower(), msgDeviceType, env);
            deviceSimulatorHelper.UserSelectedTopic(deviceId.ToLower(), topicType, topicPredefinedMessageName);
            //Adding Timer to send messages periodically
            if (DeviceSimulatorHelperService.deviceClient == null)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Device Client is null"
                });
            }
            else if (DeviceSimulatorHelperService.message == null)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Topic Type or Message name is invalid"
                });
            }
            else
            {
                msgTopicType = topicType.ToLower();
                DeviceSimulatorHelperService.timer.Dispose();
                DeviceSimulatorHelperService.timer = new Timer();
                if (Convert.ToInt32(selectedFrequency) > 0 && Convert.ToInt32(timerInSeconds) > 0)
                {
                    if (topicType.ToLower() == "main telemetry")
                    {
                        selectedFrequency = "30";
                    }
                    DeviceSimulatorHelperService.timer = new Timer(Convert.ToInt64(selectedFrequency) * 1000);
                    Timer stop_timer = new Timer(Convert.ToInt64(timerInSeconds) * 1000);
                    // Hook up the Elapsed event for the timer. 
                    DeviceSimulatorHelperService.timer.Elapsed += OnTimedEvent;
                    DeviceSimulatorHelperService.timer.AutoReset = true;
                    stop_timer.Elapsed += StopTimerEvent;
                    stop_timer.AutoReset = true;
                    DeviceSimulatorHelperService.timer.Enabled = true;
                    stop_timer.Enabled = true;
                    return Ok();
                }
                else if (selectedFrequency == "" || Convert.ToInt32(selectedFrequency) == 0)
                {
                    deviceSimulatorHelper.SendMessage(DeviceSimulatorHelperService.message, updateTimeStamp, msgDeviceType, msgTopicType);
                    deviceSimulatorHelper.DisconnectDevice();
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
        }

        [HttpPost("api/sendMessage")]
        public async Task<IActionResult> PostTopicData([FromQuery] string deviceId, string deviceType, string topicType, string selectedFrequency, string timerInSeconds, bool updateTimestamp, string environment, [FromBody] object msg)
        {
            if (deviceId == null || topicType == null)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "DeviceId/ Topic Type is null"
                });
            }
            if (deviceType == "")
            {
                msgDeviceType = "revostissueprocessor";
            }
            else
            {
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (DeviceSimulatorHelperService.deviceTypes.Contains(deviceType.ToLower()))
                {
                    msgDeviceType = deviceType.ToLower();
                }
                else
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "DeviceType invalid"
                    });
                }
            }
            updateTimeStamp = updateTimestamp;
            string env = environment == null ? "1" : environment;
            bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceId.ToLower());
            if (status == false)
            {
                //Device is Not Provisioned yet. Kindly provision the device first!
                return Unauthorized(new ErrorResponseModel
                {
                    ErrorMessage = "Device not provisioned"
                });
            }

            bool IsValid = deviceSimulatorHelper.ValidateJsonFormat(msg, topicType, deviceId.ToLower());
            if (DeviceSimulatorHelperService.deviceClient != null)
            {
                deviceSimulatorHelper.DisconnectDevice();
            }
            //Connect Device to IoTHub
            await deviceSimulatorHelper.ConnectDeviceToIoTHubAsync(deviceId.ToLower(), msgDeviceType, env);

            //Adding Timer to send messages periodically
            if (DeviceSimulatorHelperService.deviceClient == null)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Device Client could not establish connection"
                });
            }
            else if (DeviceSimulatorHelperService.message == null || IsValid == false)
            {
                return BadRequest(new ErrorResponseModel
                {
                    ErrorMessage = "Message/Topic in invalid"
                });
            }
            else
            {
                msgTopicType = topicType.ToLower();
                DeviceSimulatorHelperService.timer.Dispose();
                DeviceSimulatorHelperService.timer = new Timer();
                if (Convert.ToInt32(selectedFrequency) > 0 && Convert.ToInt32(timerInSeconds) > 0)
                {
                    if (topicType.ToLower() == "main telemetry")
                    {
                        selectedFrequency = "30";
                    }
                    DeviceSimulatorHelperService.timer = new Timer(Convert.ToInt64(selectedFrequency) * 1000);
                    Timer stop_timer = new Timer(Convert.ToInt64(timerInSeconds) * 10000);
                    // Hook up the Elapsed event for the timer. 
                    DeviceSimulatorHelperService.timer.Elapsed += OnTimedEvent;
                    DeviceSimulatorHelperService.timer.AutoReset = true;
                    stop_timer.Elapsed += StopTimerEvent;
                    stop_timer.AutoReset = true;
                    stop_timer.Enabled = true;
                    DeviceSimulatorHelperService.timer.Enabled = true;
                    return Ok();
                }
                else if (selectedFrequency == "" || Convert.ToInt32(selectedFrequency) == 0)
                {
                    deviceSimulatorHelper.SendMessage(DeviceSimulatorHelperService.message, updateTimeStamp, msgDeviceType, msgTopicType);
                    await DeviceSimulatorHelperService.deviceClient.CloseAsync();
                    DeviceSimulatorHelperService.deviceClient = null;
                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
        }

        [HttpPost("api/multiDeviceProvision")]
        public async Task<IActionResult> PostMultiDeviceProvision([FromBody] List<DeviceProvisionRequestModel> input, string environment)
        {
            List<DeviceProvisionRepresentationModel> response = new List<DeviceProvisionRepresentationModel>();
            List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
            DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
            foreach (var item in input)
            {
                if (!DeviceSimulatorHelperService.versionRegex.IsMatch(item.DeviceSoftwareVersion) || !DeviceSimulatorHelperService.versionRegex.IsMatch(item.DeviceFirmwareVersion) || item.DeviceId == "" || item.DeviceTimeZone == "" || item.DeviceType == "" || !DeviceSimulatorHelperService.deviceTypes.Contains(item.DeviceType.ToLower()))
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Incorrect Payload"
                    });
                if ((item.ServiceRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(item.ServiceRegion)) || (item.CommercialRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(item.CommercialRegion)) || (item.HubRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(item.HubRegion)))
                {
                    return BadRequest(new ErrorResponseModel
                    {
                        ErrorMessage = "Incorrect Payload"
                    });
                }
            }
            foreach (var deviceDetail in input)
            {
                string iothub = "";
                if (deviceDetail.HubRegion != "")
                    iothub = await _cosmosTableService.GetIotHubEndpoint(deviceDetail.HubRegion, environment);
                if (deviceDetail.HubRegion != "" && iothub == "")
                {
                    return BadRequest();
                }
                string env = environment == null ? "1" : environment;

                DeviceProvisionRepresentationModel dev = new DeviceProvisionRepresentationModel();
                DeviceProvisionResponseModel x = new DeviceProvisionResponseModel();
                bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceDetail.DeviceId.ToLower());
                if (status == false)
                {
                    await deviceSimulatorHelper.RegisterDeviceToIoTHub(deviceDetail.DeviceId.ToLower(), deviceDetail.DeviceType.ToLower(), deviceDetail.DeviceSoftwareVersion, deviceDetail.DeviceFirmwareVersion, deviceDetail.DeviceTimeZone, deviceDetail.ServiceRegion, deviceDetail.CommercialRegion, deviceDetail.HubRegion, env);
                    if (DeviceSimulatorHelperService.lastProvisionedDeviceId.ToLower() == deviceDetail.DeviceId.ToLower())
                    {
                        DeviceProvisionResponseModel provDevice;
                        provDevice = new DeviceProvisionResponseModel
                        {
                            DeviceId = deviceDetail.DeviceId,
                            IotHubEndPoint = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.AssignedIoTHubName,
                            PfxKey = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.PfxFile,
                            SecureString = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.SecureString,
                            CertificateExpiryDate = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.CertificateExpiryDate
                        };
                        dev.ProvisionedDevices = provDevice;
                        dev.Status = "Success";
                        response.Add(dev);
                    }
                    else
                    {
                        x.DeviceId = deviceDetail.DeviceId;
                        dev.ProvisionedDevices = x;
                        dev.Status = "Failed to Provision Device";
                        response.Add(dev);
                    }
                }
                else
                {
                    x.DeviceId = deviceDetail.DeviceId;
                    dev.ProvisionedDevices = x;
                    dev.Status = "Device already provisioned";
                    response.Add(dev);
                }
            }
            return Ok(response);
        }

        [HttpPost("api/multiDeviceDeprovision")]
        public async Task<IActionResult> PostMultiDeviceDeprovision([FromBody] List<DeviceDeprovisionRequestModel> input, string environment)
        {
            List<DeviceDeprovisionResponseModel> response = new List<DeviceDeprovisionResponseModel>();
            List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
            DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
            foreach (var deviceDetail in input)
            {
                if (deviceDetail.DeviceId == "" || !DeviceSimulatorHelperService.deviceTypes.Contains(deviceDetail.DeviceType.ToLower()))
                {
                    return BadRequest();
                }
                if (deviceDetail.DeviceId != "")
                {
                    string env = environment == null ? "1" : environment;

                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceDetail.DeviceId.ToLower());
                    if (status == false)
                    {
                        response.Add(new DeviceDeprovisionResponseModel
                        {
                            DeviceId = deviceDetail.DeviceId,
                            DeviceType = deviceDetail.DeviceType,
                            DeProvision = "Device not Provisioned"
                        });
                        break;
                    }
                    await deviceSimulatorHelper.DeleteDeviceAsync(deviceDetail.DeviceId.ToLower(), deviceDetail.DeviceType.ToLower(), env).ConfigureAwait(false);

                    response.Add(new DeviceDeprovisionResponseModel
                    {
                        DeviceId = deviceDetail.DeviceId,
                        DeviceType = deviceDetail.DeviceType,
                        DeProvision = "Success"
                    });
                }
                else
                {
                    response.Add(new DeviceDeprovisionResponseModel
                    {
                        DeviceId = deviceDetail.DeviceId,
                        DeviceType = deviceDetail.DeviceType,
                        DeProvision = "Failed to Deprovision the device"
                    });
                }
            }
            return Ok(response);
        }

        [HttpPost("api/deviceReprovision")]
        public async Task<IActionResult> PostDeviceReprovision([FromBody] DeviceProvisionRequestModel input, string environment)
        {
            if (input.DeviceId == "" || input.DeviceTimeZone == "" || input.DeviceType == "" || !DeviceSimulatorHelperService.versionRegex.IsMatch(input.DeviceSoftwareVersion) || !DeviceSimulatorHelperService.versionRegex.IsMatch(input.DeviceFirmwareVersion) || !DeviceSimulatorHelperService.timezoneRegex.IsMatch(input.DeviceTimeZone))
                return BadRequest();
            if ((input.ServiceRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(input.ServiceRegion)) || (input.CommercialRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(input.CommercialRegion)) || (input.HubRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(input.HubRegion)))
            {
                return BadRequest();
            }
            string iothub = "";
            if (input.HubRegion != "")
                iothub = await _cosmosTableService.GetIotHubEndpoint(input.HubRegion, environment);
            if (input.HubRegion != "" && iothub == "")
            {
                return BadRequest();
            }
            List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
            DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
            string env = environment == null ? "1" : environment;
            if (!DeviceSimulatorHelperService.deviceTypes.Contains(input.DeviceType.ToLower()))
            {
                return BadRequest();
            }
            bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, input.DeviceId.ToLower());
            if (status == false)
            {
                return BadRequest();
            }
            //First Deprovision/Delete device from both DPS and IoTHub
            await deviceSimulatorHelper.DeleteDeviceAsync(input.DeviceId.ToLower(), input.DeviceType.ToLower(), env).ConfigureAwait(false);

            //Reprovision Device
            await deviceSimulatorHelper.RegisterDeviceToIoTHub(input.DeviceId.ToLower(), input.DeviceType.ToLower(), input.DeviceSoftwareVersion, input.DeviceFirmwareVersion, input.DeviceTimeZone, input.ServiceRegion, input.CommercialRegion, input.HubRegion, env).ConfigureAwait(false);
            if (DeviceSimulatorHelperService.lastProvisionedDeviceId.ToLower() == input.DeviceId.ToLower())
            {
                return Ok(new DeviceProvisionResponseModel
                {
                    DeviceId = input.DeviceId,
                    IotHubEndPoint = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.AssignedIoTHubName,
                    PfxKey = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.PfxFile,
                    SecureString = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.SecureString,
                    CertificateExpiryDate = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.CertificateExpiryDate
                });
            }
            else
                return BadRequest();
            //}
            //else
            //    return BadRequest();
        }

        [HttpPost("api/multiDeviceReprovision")]
        public async Task<IActionResult> PostMultiDeviceReprovision([FromBody] List<DeviceProvisionRequestModel> input, string environment)
        {
            List<DeviceProvisionRepresentationModel> response = new List<DeviceProvisionRepresentationModel>();
            foreach (var deviceDetail in input)
            {
                if (deviceDetail.DeviceId == null || deviceDetail.DeviceType == "" || !DeviceSimulatorHelperService.versionRegex.IsMatch(deviceDetail.DeviceSoftwareVersion) || !DeviceSimulatorHelperService.versionRegex.IsMatch(deviceDetail.DeviceFirmwareVersion))
                {
                    return BadRequest();
                }
                if ((deviceDetail.ServiceRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(deviceDetail.ServiceRegion)) || (deviceDetail.CommercialRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(deviceDetail.CommercialRegion)) || (deviceDetail.HubRegion != "" && !DeviceSimulatorHelperService.regionRegex.IsMatch(deviceDetail.HubRegion)))
                {
                    return BadRequest();
                }
                string iothub = "";
                if (deviceDetail.HubRegion != "")
                    iothub = await _cosmosTableService.GetIotHubEndpoint(deviceDetail.HubRegion, environment);
                if (deviceDetail.HubRegion != "" && iothub == "")
                {
                    return BadRequest();
                }
                List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                if (!DeviceSimulatorHelperService.deviceTypes.Contains(deviceDetail.DeviceType.ToLower()))
                {
                    return BadRequest();
                }
                if (deviceDetail.DeviceId != "")
                {
                    string env = environment == null ? "1" : environment;
                    //DeviceSimulatorHelperService.devices = _bootstrapConfigurationService.GetEnrolledDevices(env);
                    DeviceProvisionRepresentationModel dev = new DeviceProvisionRepresentationModel();
                    DeviceProvisionResponseModel x = new DeviceProvisionResponseModel();
                    bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceDetail.DeviceId.ToLower());
                    if (status == true)
                    {
                        //First Deprovision/Delete device from both DPS and IoTHub
                        await deviceSimulatorHelper.DeleteDeviceAsync(deviceDetail.DeviceId.ToLower(), deviceDetail.DeviceType, env).ConfigureAwait(false);

                        //Reprovision Device
                        await deviceSimulatorHelper.RegisterDeviceToIoTHub(deviceDetail.DeviceId.ToLower(), deviceDetail.DeviceType.ToLower(), deviceDetail.DeviceSoftwareVersion, deviceDetail.DeviceFirmwareVersion, deviceDetail.DeviceTimeZone, deviceDetail.ServiceRegion, deviceDetail.CommercialRegion, deviceDetail.HubRegion, env).ConfigureAwait(false);
                        if (DeviceSimulatorHelperService.lastProvisionedDeviceId.ToLower() == deviceDetail.DeviceId.ToLower())
                        {
                            DeviceProvisionResponseModel provDevice;
                            provDevice = new DeviceProvisionResponseModel
                            {
                                DeviceId = deviceDetail.DeviceId,
                                IotHubEndPoint = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.AssignedIoTHubName,
                                PfxKey = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.PfxFile,
                                SecureString = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.SecureString,
                                CertificateExpiryDate = DeviceSimulatorHelperService.lastProvisionedDeviceInfo.CertificateExpiryDate
                            };
                            dev.ProvisionedDevices = provDevice;
                            dev.Status = "Success";
                            response.Add(dev);
                        }
                        else
                        {
                            x.DeviceId = deviceDetail.DeviceId;
                            dev.ProvisionedDevices = x;
                            dev.Status = "Failed to Provision Device";
                            response.Add(dev);
                        }
                    }
                    else
                    {
                        x.DeviceId = deviceDetail.DeviceId;
                        dev.ProvisionedDevices = x;
                        dev.Status = "Device not provisioned";
                        response.Add(dev);
                    }
                }
            }
            return Ok(response);
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            deviceSimulatorHelper.SendMessage(DeviceSimulatorHelperService.message, updateTimeStamp, msgDeviceType, msgTopicType);
        }

        private void StopTimerEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            DeviceSimulatorHelperService.timer.Enabled = false;
        }
        [HttpPost("api/sendMessageV2")]
        public async Task<IActionResult> SendMessageV2(string topicType, string secureString, string deviceType, string assignedIoTHubName, string environment, [FromBody] SendMessageRequestModel input)
        {
            string deviceId = deviceSimulatorHelper.GetDeviceId(input.UserMessage);
            if (deviceId != "" && deviceId != null && topicType != "" && input.PfxString != "")
            {
                if (deviceType == "")
                {
                    msgDeviceType = "revostissueprocessor";
                }
                else
                {
                    List<Metadata> deviceTypeList = await _cosmosTableService.GetDeviceTypeList();
                    DeviceSimulatorHelperService.deviceTypes = deviceTypeList.Select(x => x.Id).ToList();
                    if (DeviceSimulatorHelperService.deviceTypes.Contains(deviceType.ToLower()))
                    {
                        msgDeviceType = deviceType.ToLower();
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                string env = environment == null ? "1" : environment;
                bool status = _bootstrapConfigurationService.CheckIfDeviceIsEnrolled(env, deviceId.ToLower());
                if (status == true)
                {
                    if (DeviceSimulatorHelperService.deviceClient != null)
                    {
                        deviceSimulatorHelper.DisconnectDevice();
                    }
                    try
                    {
                        string filePath = _fileService.FetchFiles(input.PfxString, deviceId);

                        //Connect Device to IoTHub
                        var security = deviceSimulatorHelper.GetSecurityCertificate(filePath, secureString);
                        await deviceSimulatorHelper.CreateIoTDeviceClientAsync(security, deviceId, assignedIoTHubName);
                        bool IsValid = deviceSimulatorHelper.ValidateJsonFormat(input.UserMessage, topicType, deviceId.ToLower());
                        if (DeviceSimulatorHelperService.deviceClient == null || DeviceSimulatorHelperService.message == null || IsValid == false)
                        {
                            return BadRequest();
                        }
                        else
                        {
                            deviceSimulatorHelper.SendMessage(DeviceSimulatorHelperService.message, updateTimeStamp, msgDeviceType, topicType);
                            await DeviceSimulatorHelperService.deviceClient.CloseAsync();
                            DeviceSimulatorHelperService.deviceClient = null;
                            return Ok();
                        }
                    }
                    catch (Exception exception)
                    {
                        Program.logger.LogCritical(exception.Message);
                        throw new System.Web.Http.HttpResponseException(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                        {
                            Content = new StringContent(exception.Message, System.Text.Encoding.UTF8, "text/plain"),
                            StatusCode = HttpStatusCode.InternalServerError
                        });
                    }
                }
                else
                {
                    //Device Not Provisioned
                    return BadRequest();
                }
            }
            else
            {
                //Incorrect Payload
                return BadRequest();
            }
        }
    }
}
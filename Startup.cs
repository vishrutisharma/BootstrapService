using BootstrapService.Model;
using BootstrapService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.VisualStudio.Web.CodeGeneration.Contracts.ProjectModel;
using System;
using System.Configuration;
using System.Net;
using System.Security;
using Microsoft.Azure.Cosmos.Table;


namespace BootstrapService
{
    public class Startup
    { 
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Device Simulator API", Version = "v1" });
            });
            
            services.Add(new ServiceDescriptor(typeof(IBootstrapConfigurationService), new BootstrapConfigurationService(GetAppSettings(Configuration.GetSection("IOTDPSSettings")))));
            services.Add(new ServiceDescriptor(typeof(IDeviceSimulatorHelperService), new DeviceSimulatorHelperService(new BootstrapConfigurationService(GetAppSettings(Configuration.GetSection("IOTDPSSettings"))), InitializeCosmosTableClientInstanceAsync(Configuration.GetSection("CosmosTable")))));
            services.Add(new ServiceDescriptor(typeof(IFileService), new FileService()));
            services.AddSingleton<ICosmosTableService>(InitializeCosmosTableClientInstanceAsync(Configuration.GetSection("CosmosTable")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            );

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json",
                "Device Simulator API v1");
            });
        }

        private static  ApplicationSettingsModel GetAppSettings(IConfigurationSection configurationSection)
        {
            ApplicationSettingsModel response = new ApplicationSettingsModel();
            response.DEV_PROVISIONING_CONNECTION_STRING = configurationSection.GetSection("DEV_PROVISIONING_CONNECTION_STRING").Value;
            response.DEV_IOTHUB_CONNECTION_STRING = configurationSection.GetSection("DEV_IOTHUB_CONNECTION_STRING").Value;
            response.QA_PROVISIONING_CONNECTION_STRING = configurationSection.GetSection("QA_PROVISIONING_CONNECTION_STRING").Value;
            response.QA_IOTHUB_CONNECTION_STRING = configurationSection.GetSection("QA_IOTHUB_CONNECTION_STRING").Value;
            response.Password = configurationSection.GetSection("Password").Value;
            response.GLOBAL_DEVICE_ENDPOINT = configurationSection.GetSection("GLOBAL_DEVICE_ENDPOINT").Value;
            response.dev_IdScope = configurationSection.GetSection("dev_IdScope").Value;
            response.qa_IdScope = configurationSection.GetSection("qa_IdScope").Value;
            return response;
        }

        private static CosmosTableService InitializeCosmosTableClientInstanceAsync(IConfigurationSection configurationSection)
        {
            string devDatabaseConnectionString = configurationSection.GetSection("DevConnectionString").Value;
            string qaDatabaseConnectionString = configurationSection.GetSection("QaConnectionString").Value;

            string tableName = configurationSection.GetSection("TableName").Value;


            CloudStorageAccount devStorageAccount = CloudStorageAccount.Parse(devDatabaseConnectionString);
            CloudTableClient devTableClient = devStorageAccount.CreateCloudTableClient();
            CloudStorageAccount qastorageAccount = CloudStorageAccount.Parse(qaDatabaseConnectionString);
            CloudTableClient qaTableClient = qastorageAccount.CreateCloudTableClient();
            CosmosTableService devTableService = new CosmosTableService(devTableClient, tableName, qaTableClient);


            return devTableService;
        }
    }
}

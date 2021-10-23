using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Services
{
    public interface IFileService
    {
        public string FetchFiles(string subDirectory, string deviceId);
    }
}

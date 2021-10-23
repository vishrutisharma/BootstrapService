using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Services
{
    public class FileService : IFileService
    {
        public string FetchFiles(string subDirectory, string deviceId)
        {
            var dir = $"{deviceId}";  // folder location

            if (!Directory.Exists(dir))  // if it doesn't exist, create
                Directory.CreateDirectory(dir);

            string pfxPath = $"{deviceId}\\{deviceId}.pfx";
            Byte[] bytes = Convert.FromBase64String(subDirectory);
            System.IO.File.WriteAllBytes(pfxPath, bytes);

            return pfxPath;
        }
    }
}

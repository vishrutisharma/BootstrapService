using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class SendMessageRequestModel
    {
        public string PfxString { get; set; }
        public object UserMessage { get; set; }
    }
}

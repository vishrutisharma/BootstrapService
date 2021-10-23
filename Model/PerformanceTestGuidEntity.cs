using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BootstrapService.Model
{
    public class PerformanceTestGuidEntity : TableEntity
    {
        public PerformanceTestGuidEntity()
        {

        }

        public PerformanceTestGuidEntity(string _partitionkey, string _rowkey)
        {
            PartitionKey = _partitionkey;  //epredia_performancetest
            RowKey = _rowkey;  // testGUID
        }
        public string Status { get; set; }
    }
}

using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GIMFunctions
{
    public class CounterProvider
    {
        public async Task<Counter> GetCounter(CloudTable cloudTable, string id)
        {
            Counter counter;
            var result = await cloudTable.ExecuteAsync(TableOperation.Retrieve<Counter>(Counter.Key, id));
            if (result?.Result == null)
            {
                counter = new Counter(0, id);
            }
            else
            {
                counter = result.Result as Counter;
            }
            return counter;
        }
        public async Task UpdateCounter(CloudTable cloudTable, Counter counter)
        {
            await cloudTable.ExecuteAsync(TableOperation.InsertOrReplace(counter));
        }
    }
}
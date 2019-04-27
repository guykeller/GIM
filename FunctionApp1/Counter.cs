using Microsoft.WindowsAzure.Storage.Table;

namespace GIMFunctions
{
    public class Counter : TableEntity
    {
        public static string Key { get; } = "counter";
        public int Count { get; set; }

        public Counter() { }
        public Counter(int count, string rowKey)
        {
            Count = count;
            RowKey = rowKey;
            PartitionKey = Key;
        }

        public CounterDTO ToDTO()
        {
            return new CounterDTO()
            {
                Count = Count,
                Id = RowKey
            };
        }

        public static explicit operator Counter(CounterDTO dto)
        {
            return new Counter(dto.Count, dto.Id);
        }
    }
}
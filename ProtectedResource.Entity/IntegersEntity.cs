using ProtectedResource.Lib;

namespace ProtectedResource.Entity
{
    public class IntegersEntity
        : IResource
    {
        public int IntegersId { get; set; }

        public int? Number1 { get; set; }

        public int? Number2 { get; set; }

        public int? Number3 { get; set; }

        public string GetPartitionKey() => IntegersId.ToString();
    }
}

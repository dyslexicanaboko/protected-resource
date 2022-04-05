using System.Data;

namespace ProtectedResource.Lib.Models
{
    public class SchemaRaw
    {
        public DataTable GenericSchema { get; set; }
        
        public DataTable SqlServerSchema { get; set; }
    }
}

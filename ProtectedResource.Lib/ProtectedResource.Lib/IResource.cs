using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtectedResource.Lib
{
    public interface IResource
    {
        string GetPartitionKey();
    }
}

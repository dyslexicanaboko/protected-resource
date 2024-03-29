﻿using System.Collections.Generic;

namespace ProtectedResource.Lib.Models
{
    public class SchemaQuery
    {
        public string Query { get; set; }

        public TableQuery TableQuery { get; set; }

        public bool HasPrimaryKey { get; set; }

        public SchemaColumn PrimaryKey { get; set; }

        public IList<SchemaColumn> ColumnsAll { get; set; }
        
        public IList<SchemaColumn> ColumnsNoPk { get; set; }
    }
}

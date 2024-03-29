﻿using System;

namespace ProtectedResource.Lib.Models
{
    public class SchemaColumn
    {
        public bool IsPrimaryKey { get; set; }

        public bool IsIdentity { get; set; }

        public bool IsDbNullable { get; set; }

        public string ColumnName { get; set; }
        
        public Type SystemType { get; set; }
        
        public string SqlType { get; set; }
        
        public int Size { get; set; }
        
        public int Precision { get; set; }
        
        public int Scale { get; set; } 
    }
}

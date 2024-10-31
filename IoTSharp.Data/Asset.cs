﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IoTSharp.Data
{
    public class Asset
    {
        [Key]
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string AssetType { get; set; }
        public List<AssetRelation> OwnedAssets { get; set; }
        public Customer Customer { get; set; }
        public Tenant Tenant { get; set; }
        public bool Deleted { get; set; }
    }
}
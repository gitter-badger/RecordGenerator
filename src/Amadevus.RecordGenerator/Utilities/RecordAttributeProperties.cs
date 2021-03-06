﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amadevus.RecordGenerator
{
    internal class RecordAttributeProperties
    {
        public const string Filename = "RecordAttribute";
        public const string PrimaryCtorAccessDefault = "public";
        public const string PrimaryCtorAccessName = nameof(RecordAttribute.PrimaryCtorAccess);
        public const bool GenerateMutatorsDefault = true;
        public const string GenerateMutatorsName = nameof(RecordAttribute.GenerateMutators);
        public const bool GenerateCollectionMutatorsDefault = true;
        public const string GenerateCollectionMutatorsName = nameof(RecordAttribute.GenerateCollectionMutators);
    }
}

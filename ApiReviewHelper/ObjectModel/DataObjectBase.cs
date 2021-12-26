using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [DebuggerDisplay("{Name,nq}")]
    internal abstract class DataObjectBase
    {
        protected DataObjectBase()
        {
        }

        protected DataObjectBase(string name)
        {
            this.Name = name;
        }

        [DataMember]
        public string Name { get; private set; }
    }
}

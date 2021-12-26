using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [KnownType(typeof(TypeData))]
    [KnownType(typeof(Collection<TypeData>))]
    [DebuggerDisplay("{Name,nq}")]
    [DebuggerTypeProxy(typeof(NamespaceDebugView))]
    internal class NamespaceData : DataObjectBase
    {
        #region Nested Types

        private class NamespaceDebugView
        {
            #region Fields

            private readonly NamespaceData _data;

            #endregion Fields

            #region Constructors

            public NamespaceDebugView(NamespaceData data)
            {
                this._data = data;
            }

            #endregion Constructors

            #region Properties - Public

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Collection<TypeData> Namespaces { get { return this._data.Types; } }

            #endregion Properties - Public
        }

        #endregion Nested Types

        #region Constructors

        public NamespaceData()
        {
            this.Types = new Collection<TypeData>();
        }

        public NamespaceData(string name, IEnumerable<TypeData> types)
            : base(name)
        {
            this.Types = new Collection<TypeData>(types.ToArray());
        }

        #endregion Constructors

        #region Properties - Public

        [DataMember]
        public Collection<TypeData> Types { get; private set; }

        #endregion Properties - Public

        #region Methods - Public

        public bool IsEquivalentTo(NamespaceData other)
        {
            if (other == null) { return false; }

            if (this.Name != other.Name) { return false; }

            if (this.Types.Count != other.Types.Count) { return false; }

            for (int i = 0; i < this.Types.Count; i++)
            {
                if (this.Types[i].IsEquivalentTo(other.Types[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion Methods - Public
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [KnownType(typeof(NamespaceData))]
    [KnownType(typeof(Collection<NamespaceData>))]
    [DebuggerDisplay("Assembly: {Name,nq}.dll")]
    [DebuggerTypeProxy(typeof(AssemblyDebugView))]
    internal class AssemblyData : DataObjectBase
    {
        private class AssemblyDebugView
        {
            private readonly AssemblyData _data;

            public AssemblyDebugView(AssemblyData data)
            {
                this._data = data;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Collection<NamespaceData> Namespaces { get { return this._data.Namespaces; } }
        }

        #region Constructors

        public AssemblyData()
        {
            this.Namespaces = new Collection<NamespaceData>();
        }

        public AssemblyData(Assembly assembly)
            : base(assembly.GetName().Name)
        {
            this.Namespaces = new Collection<NamespaceData>();

            AssemblyName assemblyName = assembly.GetName();

            this.FullName = string.Format(
                CultureInfo.InvariantCulture,
                "{0} (File Version {1}, Assembly Version {2})",
                assemblyName.Name,
                FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion,
                assemblyName.Version);

            // Get all the types and sort them by namespace.
            Dictionary<string, List<TypeData>> typesByNamespace = new Dictionary<string, List<TypeData>>();
            IEnumerable<Type> types = assembly.GetTypes().OrderBy(t => t.FullName, StringComparer.Ordinal);
            foreach (Type type in types)
            {
                // Skip non-exported types.
                if (IsTypeExported(type) == false) { continue; }

                // Skip nested delegate types
                if (type.IsNested && type.BaseType == typeof(MulticastDelegate)) { continue; }

                // Make sure the list exists for the type's namespace.
                if (typesByNamespace.ContainsKey(type.Namespace) == false)
                {
                    typesByNamespace[type.Namespace] = new List<TypeData>();
                }

                typesByNamespace[type.Namespace].Add(new TypeData(type));
            }

            // Create the NamespaceData objects.
            foreach (string namespaceName in typesByNamespace.Keys.OrderBy(name => name, StringComparer.Ordinal))
            {
                NamespaceData nsData = new NamespaceData(namespaceName, typesByNamespace[namespaceName]);
                this.Namespaces.Add(nsData);
            }
        }

        #endregion Constructors

        #region Properties - Public

        [DataMember]
        public string FullName { get; private set; }

        [DataMember]
        public Collection<NamespaceData> Namespaces { get; private set; }

        #endregion Properties - Public

        public bool IsEquivalentTo(AssemblyData other)
        {
            if (other == null) { return false; }

            if (this.Name != other.Name) { return false; }

            if (this.Namespaces.Count != other.Namespaces.Count) { return false; }

            for (int i = 0; i < this.Namespaces.Count; i++)
            {
                if (this.Namespaces[i].IsEquivalentTo(other.Namespaces[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        #region Methods - Private

        private static bool IsTypeExported(Type type)
        {
            return type.IsPublic
                   || ((type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem) && IsTypeExported(type.DeclaringType));
        }

        #endregion Methods - Private

    }
}

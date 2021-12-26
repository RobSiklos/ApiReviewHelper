using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [KnownType(typeof(DataObjectBase))]
    [KnownType(typeof(AssemblyData))]
    [KnownType(typeof(Collection<AssemblyData>))]
    [KnownType(typeof(NamespaceData))]
    [KnownType(typeof(Collection<NamespaceData>))]
    [KnownType(typeof(TypeData))]
    [KnownType(typeof(Collection<TypeData>))]
    [KnownType(typeof(MemberData))]
    [KnownType(typeof(Collection<MemberData>))]
    [KnownType(typeof(MemberAttributes))]
    [KnownType(typeof(TypeVariant))]
    [KnownType(typeof(MemberType))]
    internal class ApiData
    {
        #region Constructors

        public ApiData()
        {
            this.Assemblies = new Collection<AssemblyData>();
        }

        public ApiData(IEnumerable<Assembly> assemblies)
        {
            this.Assemblies = new Collection<AssemblyData>();

            foreach (Assembly assembly in assemblies.OrderBy(a => a.FullName))
            {
                this.Assemblies.Add(new AssemblyData(assembly));
            }
        }

        #endregion Constructors

        #region Properties - Public

        [DataMember]
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Collection<AssemblyData> Assemblies { get; private set; }

        #endregion Properties - Public

        #region Methods - Public

        public bool IsEquivalentTo(ApiData other)
        {
            if (other == null) { return false; }

            if (this.Assemblies.Count != other.Assemblies.Count) { return false; }

            for (int i = 0; i < this.Assemblies.Count; i++)
            {
                if (this.Assemblies[i].IsEquivalentTo(other.Assemblies[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static ApiData ReadXml(string fileName)
        {
            DataContractSerializer s = new DataContractSerializer(typeof(ApiData));

            // Deserialize
            ApiData result;
            using (Stream reader = File.OpenRead(fileName))
            {
                result = (ApiData)s.ReadObject(reader);
            }

            return result;
        }
        
        public void WriteXml(string fileName)
        {
            DataContractSerializerSettings settings = new DataContractSerializerSettings()
            {
                SerializeReadOnlyTypes = true,
                MaxItemsInObjectGraph = int.MaxValue,
            };
            DataContractSerializer s = new DataContractSerializer(typeof(ApiData), settings);

            XmlWriterSettings xmlSettings = new XmlWriterSettings() { Indent = true };
            using (XmlWriter outputStream = XmlWriter.Create(fileName, xmlSettings))
            {
                s.WriteObject(outputStream, this);
            }
        }

        #endregion Methods - Public
    }
}

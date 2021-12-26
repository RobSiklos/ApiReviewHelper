using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [KnownType(typeof(MemberData))]
    [KnownType(typeof(Collection<MemberData>))]
    [DebuggerDisplay("{Signature,nq}")]
    [DebuggerTypeProxy(typeof(TypeDebugView))]
    internal class TypeData : DataObjectBase
    {
        #region Nested Types

        private class TypeDebugView
        {
            #region Fields

            private readonly TypeData _data;

            #endregion Fields

            #region Constructors

            public TypeDebugView(TypeData data)
            {
                this._data = data;
            }

            #endregion Constructors

            #region Properties - Public

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public Collection<MemberData> Namespaces { get { return this._data.Members; } }

            #endregion Properties - Public
        }

        #endregion Nested Types

        #region Fields

        private static readonly List<MemberTypes> _memberTypeOrder = new List<MemberTypes>() {
            MemberTypes.NestedType,
            MemberTypes.Event,
            MemberTypes.Field,
            MemberTypes.Constructor,
            MemberTypes.Property,
            MemberTypes.Method,
        };

        #endregion Fields

        #region Constructors

        public TypeData()
        {
            this.Members = new Collection<MemberData>();
        }

        public TypeData(Type type)
            : base(type.GetFriendlyTypeName())
        {
            this.Members = new Collection<MemberData>();

            // Populate the Members collection.
            IEnumerable<MemberInfo> members = type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            // If it's not an enum, order the members by kind, then alphabetically, then by parameter list.
            // We want the order to be the same every time we generate the list, otherwise we could get invalid diffs.
            if (type.IsEnum == false)
            {
                members = members.OrderBy(m =>
                                 {
                                     return _memberTypeOrder.IndexOf(m.MemberType);
                                 })
                                 .ThenBy(m => m.Name, StringComparer.Ordinal)
                                 .ThenBy(m =>
                                 {
                                     MethodBase methodBase = m as MethodBase;
                                     if (methodBase == null)
                                     {
                                         // Not a method.
                                         return string.Empty;
                                     }
                                     else
                                     {
                                         return string.Join(",", methodBase.GetParameters().Select(p => p.Name));
                                     }
                                 }, StringComparer.Ordinal);
            }

            // Type of the type.
            if (type.IsInterface) { this.TypeVariant = TypeVariant.Interface; }
            else if (type.IsEnum) { this.TypeVariant = TypeVariant.Enum; }
            else if (type.IsValueType) { this.TypeVariant = TypeVariant.Struct; }
            else if (type.IsClass) { this.TypeVariant = TypeVariant.Class; }

            foreach (MemberInfo memberInfo in members)
            {
                switch (memberInfo.MemberType)
                {
                    case MemberTypes.Constructor:
                        ConstructorInfo constructorInfo = (ConstructorInfo)memberInfo;
                        if (IsMemberExported(constructorInfo))
                        {
                            this.Members.Add(new MemberData(constructorInfo));
                        }
                        break;
                    case MemberTypes.Method:
                        MethodInfo methodInfo = (MethodInfo)memberInfo;
                        if (IsMemberExported(methodInfo))
                        {
                            this.Members.Add(new MemberData(methodInfo));
                        }
                        break;
                    case MemberTypes.Property:
                        PropertyInfo propInfo = (PropertyInfo)memberInfo;
                        if (IsMemberExported(propInfo))
                        {
                            this.Members.Add(new MemberData(propInfo));
                        }
                        break;
                    case MemberTypes.Event:
                        EventInfo eventInfo = (EventInfo)memberInfo;
                        if (IsMemberExported(eventInfo))
                        {
                            this.Members.Add(new MemberData(eventInfo));
                        }
                        break;
                    case MemberTypes.Field:
                        FieldInfo fieldInfo = (FieldInfo)memberInfo;
                        if ((fieldInfo.IsSpecialName == false) && IsMemberExported(fieldInfo))
                        {
                            this.Members.Add(new MemberData(fieldInfo));
                        }
                        break;
                    case MemberTypes.NestedType:
                        Type typeInfo = (Type)memberInfo;
                        if (type.BaseType == typeof(MulticastDelegate) && IsDelegateMemberExported(typeInfo))
                        {
                            this.Members.Add(new MemberData(typeInfo));
                        }
                        break;
                    default:
                        Debug.Fail("Unknown type: " + memberInfo.MemberType.ToString());
                        break;
                }
            }

            // Generate the signature.
            this.Signature = this.GenerateSignature(type);
        }

        #endregion Constructors

        #region Properties - Public

        [DataMember]
        public TypeVariant TypeVariant { get; set; }

        [DataMember]
        public Collection<MemberData> Members { get; private set; }

        [DataMember]
        public string Signature { get; set; }

        #endregion Properties - Public

        #region Methods - Public

        public bool IsEquivalentTo(TypeData other)
        {
            if (other == null) { return false; }

            if (this.Signature != other.Signature) { return false; }

            if (this.Members.Count != other.Members.Count) { return false; }

            for (int i = 0; i < this.Members.Count; i++)
            {
                if (this.Members[i].IsEquivalentTo(other.Members[i]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion Methods - Public

        #region Methods - Private

        private static bool IsDelegateMemberExported(Type delegateTypeInfo)
        {
            if (delegateTypeInfo == null) { return false; }
            return IsMemberExported(delegateTypeInfo.GetMethod("Invoke"));
        }

        private static bool IsMemberExported(FieldInfo info)
        {
            if (info == null) { return false; }
            return info.IsPublic || info.IsFamily || info.IsFamilyOrAssembly;
        }

        private static bool IsMemberExported(MethodInfo info)
        {
            if (info == null) { return false; }

            // Only show special names for operator overloads.
            if (info.IsSpecialName && info.Name.StartsWith("op_") == false)
            {
                return false;
            }

            return IsMethodExportedCore(info);
        }

        private static bool IsMemberExported(PropertyInfo info)
        {
            return IsMethodExportedCore(info.GetGetMethod(true)) || IsMethodExportedCore(info.GetSetMethod(true));
        }

        private static bool IsMemberExported(EventInfo info)
        {
            return IsMethodExportedCore(info.GetAddMethod(true)) || IsMethodExportedCore(info.GetRemoveMethod(true));
        }

        private static bool IsMemberExported(MethodBase info)
        {
            if (info == null) { return false; }
            return info.IsPublic || info.IsFamily || info.IsFamilyOrAssembly;
        }

        private static bool IsMethodExportedCore(MethodInfo info)
        {
            if (info == null) { return false; }

            // Don't expose any methods on enum types.
            if (info.DeclaringType.IsEnum) { return false; }

            // Don't list unsealed overrides, or overrides on sealed types.
            bool isOverride = info.IsVirtual && (info.GetBaseDefinition() != info);
            if (isOverride)
            {
                // If the entire type is sealed, don't show it.
                if (info.DeclaringType.IsSealed) { return false; }

                // The type is not sealed - only show overrides if they are sealed.
                if (info.IsFinal == false) { return false; }
            }

            return IsMemberExported((MethodBase)info);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "C# strings are in lower case")]
        private string GenerateSignature(Type type)
        {
            StringBuilder builder = new StringBuilder();

            // Accessibility modifiers.
            if (type.IsPublic || type.IsNestedPublic) { builder.Append(" public"); }
            else if (type.IsNestedFamily) { builder.Append(" protected"); }
            else if (type.IsNestedFamORAssem) { builder.Append(" protected internal"); }
            else if (type.IsNestedFamANDAssem) { builder.Append(" private protected"); }

            // Inheritance modifiers.
            if (type.IsClass)
            {
                if (type.IsAbstract && type.IsSealed) { builder.Append(" static"); }
                else if (type.IsAbstract) { builder.Append(" abstract"); }
                else if (type.IsSealed) { builder.Append(" sealed"); }
            }

            // Type of the type.
            builder.Append(' ');
            builder.Append(this.TypeVariant.ToString().ToLowerInvariant());

            // Type name.
            builder.Append(' ');
            builder.Append(type.GetFriendlyTypeName());

            // Inherits from.
            if (type.IsEnum == false)
            {
                List<Type> inheritsFrom = new List<Type>();

                // First add the base type, if not Object.
                if (type.IsClass && type.BaseType != null && type.BaseType != typeof(object))
                {
                    inheritsFrom.Add(type.BaseType);
                }
                // Now add the interfaces.
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    // First get the interface map.
                    if (type.IsInterface == false)
                    {
                        InterfaceMapping interfaceMap = type.GetInterfaceMap(interfaceType);
                        if (interfaceMap.TargetMethods.Any(tm => tm.DeclaringType == type) == false)
                        {
                            // It's an inherited interface, and the type doesn't provide an implementation for
                            // any of the methods.
                            continue;
                        }
                    }

                    if (interfaceType.IsPublic || interfaceType.IsNestedFamily || interfaceType.IsNestedFamORAssem)
                    {
                        inheritsFrom.Add(interfaceType);
                    }
                }

                if (inheritsFrom.Count > 0)
                {
                    builder.Append(" : ");
                    builder.Append(string.Join(", ", inheritsFrom.Select(t => t.GetFriendlyTypeName())));
                }
            }

            // Generic constraints.
            if (type.IsGenericType)
            {
                string genericConstraintText = Helpers.GetGenericConstraintText(type.GetGenericArguments());
                if (genericConstraintText.Length > 0)
                {
                    builder.Append(' ');
                    builder.Append(genericConstraintText);
                }
            }

            // Attributes.
            Helpers.ProcessCustomAttributes(type.GetCustomAttributesData(), builder);

            return builder.ToString().Trim();
        }

        #endregion Methods - Private

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [DataContract]
    [DebuggerDisplay("{Signature,nq}")]
    internal class MemberData : DataObjectBase
    {
        #region Constructors

        public MemberData()
        {
        }

        public MemberData(Type info)
        {
            // This is for delegates only.
            if (info.BaseType != typeof(MulticastDelegate)) { throw new ArgumentException("Must be delegate type", nameof(info)); }

            this.MemberType = MemberType.Delegate;

            MethodInfo method = info.GetMethod("Invoke");
            this.InitializeFromMethodBase(method, method.ReturnType);
        }

        public MemberData(EventInfo info)
            : base(info.Name)
        {
            this.MemberType = MemberType.Event;
            MemberAttributes attributes = MemberAttributes.Event;
            if (info.GetAddMethod(true).IsStatic) { attributes |= MemberAttributes.Static; }

            attributes |= GetMemberAccessibility(info.GetAddMethod(true));

            this.Signature = this.GenerateSignature(info, info.EventHandlerType, attributes, null);
        }

        public MemberData(ConstructorInfo info)
            : base(info.Name)
        {
            this.MemberType = MemberType.Constructor;
            this.InitializeFromMethodBase(info, null);
        }

        public MemberData(MethodInfo info)
            : base(info.Name)
        {
            this.MemberType = MemberType.Method;
            this.InitializeFromMethodBase(info, info.ReturnType);
        }

        public MemberData(PropertyInfo info)
            : base(info.Name)
        {
            this.MemberType = MemberType.Property;

            MethodInfo getMethod = info.GetGetMethod(true);
            MethodInfo setMethod = info.GetSetMethod(true);

            MemberAttributes attributes = MemberAttributes.None;

            // Set property-level info.
            MethodInfo methodInfo = getMethod ?? setMethod;
            attributes |= GetBaseAttributes(methodInfo);

            // Find out the accessibility
            MemberAttributes getAccessibility = GetMemberAccessibility(getMethod);
            MemberAttributes setAccessibility = GetMemberAccessibility(setMethod);

            // Find the most visible modifier.
            MemberAttributes highestAccessibility = (MemberAttributes)Math.Max((int)getAccessibility, (int)setAccessibility);
            attributes |= highestAccessibility;

            bool isInterface = info.DeclaringType.IsInterface;

            // Build the body.
            StringBuilder body = new StringBuilder();

            // Add any indexer information to the body.
            ParameterInfo[] indexParams = info.GetIndexParameters();
            if (indexParams.Length > 0)
            {
                IEnumerable<string> indexStrings = indexParams.Select(pi => Helpers.GetFriendlyTypeName(pi.ParameterType) + " " + pi.Name);

                body.AppendFormat("[{0}]", String.Join(", ", indexStrings));
            }

            // Add get/set to the body.
            body.Append(" {");
            if (getMethod != null && (isInterface || getAccessibility != MemberAttributes.None))
            {
                if (getAccessibility != highestAccessibility)
                {
                    // Specify the accessibility explicitly.
                    body.Append(' ');
                    body.Append(getAccessibility.ToCSharpText());
                }
                body.Append(" get;");
            }
            if (setMethod != null && (isInterface || setAccessibility != MemberAttributes.None))
            {
                if (setAccessibility != highestAccessibility)
                {
                    // Specify the accessibility explicitly.
                    body.Append(' ');
                    body.Append(setAccessibility.ToCSharpText());
                }
                body.Append(" set;");
            }
            body.Append(" }");

            this.Signature = this.GenerateSignature(info, info.PropertyType, attributes, body.ToString());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Nothing can be done here")]
        public MemberData(FieldInfo info)
            : base(info.Name)
        {
            this.MemberType = MemberType.Field;

            Type returnType;

            MemberAttributes attributes = MemberAttributes.None;
            if (info.DeclaringType.IsEnum)
            {
                returnType = null;
            }
            else
            {
                if (info.IsLiteral) { attributes |= MemberAttributes.Const; }
                else if (info.IsStatic) { attributes |= MemberAttributes.Static; }
                if (info.IsInitOnly) { attributes |= MemberAttributes.Readonly; }
                attributes |= GetMemberAccessibility(info);

                returnType = info.FieldType;
            }

            string body;
            if (info.IsLiteral)
            {
                // Put the numeric value of the enum or constant in the body.
                body = String.Format(" = \"{0}\"", info.GetRawConstantValue());
            }
            else if (info.IsStatic && info.IsInitOnly)
            {
                // It's a static readonly - try to put the string representation of the field.
                try
                {
                    object value = info.GetValue(null);
                    if (value == null)
                    {
                        body = "<null>";
                    }
                    else
                    {
                        // We have a non-null value.

                        // See if the ToString() method has been overridden from object.
                        MethodInfo toString = value.GetType().GetMethod("ToString",
                                                                        BindingFlags.Public | BindingFlags.Instance,
                                                                        null,
                                                                        Array.Empty<Type>(),
                                                                        null);

                        if (toString.DeclaringType == typeof(object) || toString.DeclaringType == typeof(ValueType))
                        {
                            // ToString() was not overridden - can't show anything useful in the output.
                            body = null;
                        }
                        else
                        {
                            // ToString() WAS overridden - show it's value.
                            body = String.Format(" = \"{0}\"", value.ToString());
                        }
                    }
                }
                catch
                {
                    body = null;
                }
            }
            else
            {
                body = null;
            }

            this.Signature = this.GenerateSignature(info, returnType, attributes, body);
        }

        #endregion Constructors

        #region Properties - Public

        [DataMember]
        public MemberType MemberType { get; set; }

        [DataMember]
        public string Signature { get; set; }

        #endregion Properties - Public

        #region Methods - Public

        public bool IsEquivalentTo(MemberData other)
        {
            if (other == null) { return false; }

            return (this.Signature == other.Signature);
        }

        #endregion Methods - Public

        #region Methods - Private

        private static MemberAttributes GetBaseAttributes(MethodBase info)
        {
            MethodInfo methodInfo = info as MethodInfo;
            MemberAttributes attributes = MemberAttributes.None;

            if (info.DeclaringType.IsInterface == false)
            {
                if (info.IsStatic) { attributes |= MemberAttributes.Static; }

                if (info.IsFinal && info.IsVirtual)
                {
                    if (info.Attributes.HasFlag(MethodAttributes.VtableLayoutMask))
                    {
                        // don't do anything - the method has these flags because it is part of an interface implementation.
                    }
                    else
                    {
                        // Sealed virtual method.
                        attributes |= MemberAttributes.Sealed | MemberAttributes.Override;
                    }

                }
                else if (info.IsFinal) { attributes |= MemberAttributes.Sealed; }
                else if (info.IsAbstract) { attributes |= MemberAttributes.Abstract; }
                else if (info.IsVirtual && methodInfo != null)
                {
                    MethodInfo baseMethod = methodInfo.GetBaseDefinition();
                    if (baseMethod.Equals(info))
                    {
                        // Type declared the method as virtual.
                        attributes |= MemberAttributes.Virtual;
                    }
                    else
                    {
                        // Type is overriding the method.
                        attributes |= MemberAttributes.Override;
                    }
                }
            }

            if (methodInfo != null && methodInfo.IsHiding())
            {
                attributes |= MemberAttributes.New;
            }

            if (methodInfo != null && methodInfo.ReflectionOnlyIsDefined(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute)))
            {
                attributes |= MemberAttributes.Async;
            }

            return attributes;
        }

        private void InitializeFromMethodBase(MethodBase info, Type returnType)
        {
            // Set up the attributes.
            MemberAttributes attributes = GetBaseAttributes(info);
            attributes |= GetMemberAccessibility(info);

            // Create the body.
            StringBuilder body = new StringBuilder();

            // Generic parameters.
            if (info.IsGenericMethod)
            {
                body.AppendFormat("<{0}>", string.Join(", ", info.GetGenericArguments().Select(t => t.Name)));
            }

            // Parameters.
            List<string> paramStrings = new List<string>();
            foreach (ParameterInfo paramInfo in info.GetParameters())
            {
                List<string> paramAttributes = new List<string>();

                bool isOutParam = false;

                if (paramInfo.ParameterType.IsByRef)
                {
                    if (paramInfo.IsOut)
                    {
                        // "out" param.
                        paramAttributes.Add(LangWord.Out);
                        isOutParam = true;
                    }
                    else
                    {
                        // "ref" param.
                        paramAttributes.Add(LangWord.Ref);
                    }
                }
                else if (paramInfo.ReflectionOnlyIsDefined(typeof(ParamArrayAttribute)))
                {
                    paramAttributes.Add(LangWord.Params);
                }

                foreach (CustomAttributeData attr in paramInfo.GetCustomAttributesData().OrderBy(a => a.AttributeType.Name))
                {
                    // We already handled "out" params.
                    if (attr.AttributeType == typeof(System.Runtime.InteropServices.OutAttribute) && isOutParam) { continue; }

                    // We already handled "params".
                    if (attr.AttributeType == typeof(ParamArrayAttribute)) { continue; }

                    // We're going to handle arguments with default values.
                    if (attr.AttributeType == typeof(OptionalAttribute)) { continue; }

                    paramAttributes.Add(Helpers.GetFriendlyAttributeString(attr));
                }

                string paramString = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} {2}",
                    String.Join(" ", paramAttributes.Where(s => s.Length > 0)),
                    paramInfo.ParameterType.GetFriendlyTypeName(paramInfo),
                    paramInfo.Name)
                    .Trim();
                if (paramInfo.ReflectionOnlyHasDefaultValue())
                {
                    string defaultValueString = " = ";
                    object defaultValue = paramInfo.RawDefaultValue;
                    if (defaultValue == null)
                    {
                        if (paramInfo.ParameterType.IsValueType)
                        {
                            // Value type: Output defatult type value.
                            defaultValueString += $"{LangWord.Default}({paramInfo.ParameterType.Name})";
                        }
                        else
                        {
                            // Reference type: Output explicit null.
                            defaultValueString += LangWord.Null;
                        }
                    }
                    else if (defaultValue is string)
                    {
                        // String value.
                        defaultValueString += "\"" + (string)defaultValue + "\"";
                    }
                    else if (paramInfo.ParameterType.IsEnum)
                    {
                        // Enum value.
                        object enumMemberName = Enum.GetName(paramInfo.ParameterType, defaultValue);

                        defaultValueString += $"{paramInfo.ParameterType.Name}.{enumMemberName}";
                    }
                    else if (defaultValue is bool)
                    {
                        // Boolean value.
                        defaultValueString += LangWord.FromBoolean((bool)defaultValue);
                    }
                    else if (defaultValue.GetType().IsNumeric())
                    {
                        // Raw numeric value.
                        defaultValueString += defaultValue.ToString();
                    }
                    else
                    {
                        // Some other kind of value.
                        defaultValueString += "{" + defaultValue.ToString() + "}";
                    }
                    paramString += defaultValueString;
                }
                else if (paramInfo.IsOptional)
                {
                    // The default value is not a constant expression, so it's a new instance of a value type.
                    paramString += $" = {LangWord.Default}({paramInfo.ParameterType.Name})";
                }

                paramStrings.Add(paramString);
            }

            // Extension methods.
            if (info.IsStatic && paramStrings.Any() && info.ReflectionOnlyIsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)))
            {
                paramStrings[0] = LangWord.This + " " + paramStrings[0];
            }

            body.AppendFormat("({0})", string.Join(", ", paramStrings));

            // Generic parameter type constraints.
            if (info.IsGenericMethod)
            {
                string genericConstraintText = Helpers.GetGenericConstraintText(info.GetGenericArguments());
                if (genericConstraintText.Length > 0)
                {
                    body.Append(' ');
                    body.Append(genericConstraintText);
                }
            }

            this.Signature = this.GenerateSignature(info, returnType, attributes, body.ToString());
        }

        private string GenerateSignature(MemberInfo info, Type returnType, MemberAttributes attributes, string body)
        {
            StringBuilder builder = new StringBuilder();

            // Set up the custom attribute provider for the signature's tuple names.
            ICustomAttributeProvider attributeProvider;
            switch (info.MemberType)
            {
                case MemberTypes.Method:
                    attributeProvider = ((MethodInfo)info).ReturnParameter;
                    break;
                default:
                    attributeProvider = info;
                    break;
            }

            // Add any interface implementation we detect.
            Type declaringType = info.DeclaringType;
            if (declaringType.IsInterface == false)
            {
                MethodInfo targetInterfaceMethod;
                switch (info.MemberType)
                {
                    case MemberTypes.Method:
                        targetInterfaceMethod = (MethodInfo)info;
                        break;
                    case MemberTypes.Property:
                        targetInterfaceMethod = ((PropertyInfo)info).GetGetMethod(true);
                        break;
                    case MemberTypes.Event:
                        EventInfo eventInfo = (EventInfo)info;
                        targetInterfaceMethod = eventInfo.GetAddMethod(true) ?? eventInfo.GetRemoveMethod(true);
                        break;
                    default:
                        targetInterfaceMethod = null;
                        break;
                }
                if (targetInterfaceMethod != null)
                {
                    Type foundInterface = null;
                    foreach (Type interfaceType in declaringType.GetInterfaces().Where(i => i.IsPublic))
                    {
                        InterfaceMapping mapping = declaringType.GetInterfaceMap(interfaceType);

                        if (mapping.TargetMethods.Contains(targetInterfaceMethod))
                        {
                            foundInterface = interfaceType;
                            break;
                        }
                    }

                    if (foundInterface != null)
                    {
                        builder.AppendFormat(" ({0})", foundInterface.GetFriendlyTypeName());
                    }
                }
            }

            // Go through the attributes and add them as strings.
            foreach (MemberAttributes attribute in Enum.GetValues(typeof(MemberAttributes)))
            {
                // Don't write out "none"...
                if (attribute == MemberAttributes.None) { continue; }

                if (attributes.HasFlag(attribute))
                {
                    builder.Append(' ');
                    builder.Append(attribute.ToCSharpText());
                }
            }

            // Add the return type.
            if (returnType != null)
            {
                builder.Append(' ');
                builder.Append(returnType.GetFriendlyTypeName(attributeProvider));

                // Add attributes from the return type.
                MethodInfo methodInfo = info as MethodInfo;
                if (methodInfo != null && methodInfo.ReturnParameter != null && methodInfo.ReturnParameter.CustomAttributes.Any())
                {
                    Helpers.ProcessCustomAttributes(methodInfo.ReturnParameter.GetCustomAttributesData(), builder);
                }
            }

            // Add the name.
            builder.Append(' ');
            if (info is PropertyInfo && ((PropertyInfo)info).GetIndexParameters().Length > 0)
            {
                // It's a property with an indexer - use "this" as the name.
                builder.Append(LangWord.This);
            }
            else
            {
                // Use the actual name.
                builder.Append(this.Name);
            }

            // Add the body if there is one.
            if (body != null)
            {
                builder.Append(body);
            }

            // Custom Attributes.
            Helpers.ProcessCustomAttributes(info.GetCustomAttributesData(), builder);

            return builder.ToString().Trim();
        }

        private static MemberAttributes GetMemberAccessibility(MethodBase info)
        {
            MemberAttributes attributes = GetMemberAccessibilityCore(info);
            return attributes;
        }

        private static MemberAttributes GetMemberAccessibility(FieldInfo info)
        {
            MemberAttributes attributes = GetMemberAccessibilityCore(info);
            return attributes;
        }

        private static MemberAttributes GetMemberAccessibilityCore(dynamic info)
        {
            if (info == null) { return MemberAttributes.None; }
            if (info.DeclaringType.IsInterface) { return MemberAttributes.None; }

            if (info.IsPrivate || info.IsAssembly || info.IsFamilyAndAssembly) { return MemberAttributes.None; }
            if (info.IsPublic) { return MemberAttributes.Public; }
            if (info.IsFamilyOrAssembly) { return MemberAttributes.Protected_Internal; }
            if (info.IsFamily) { return MemberAttributes.Protected; }

            // Should never be here.
            Debug.Fail("Unknown member accessibility");
            return MemberAttributes.None;
        }

        #endregion Methods - Private

    }
}

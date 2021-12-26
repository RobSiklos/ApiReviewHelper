using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    internal static class Helpers
    {
        public static void ProcessCustomAttributes(IEnumerable<CustomAttributeData> attributes, StringBuilder builder)
        {
            foreach (CustomAttributeData attr in attributes.OrderBy(a => a.AttributeType.Name))
            {
                string str = Helpers.GetFriendlyAttributeString(attr).Trim();
                if (str.Length > 0)
                {
                    builder.AppendFormat(" {0}", str);
                }
            }
        }

        public static string GetFriendlyAttributeString(CustomAttributeData attr)
        {
            Type attrType = attr.AttributeType;

            if (attrType.IsPublic == false)
            {
                // Ignore non-public attributes.
                return string.Empty;
            }

            // Ignore attributes that we don't care about.
            if ((attrType.ReflectionOnlyEquals(typeof(System.Diagnostics.CodeAnalysis.SuppressMessageAttribute)))
             || (attrType.ReflectionOnlyEquals(typeof(System.Diagnostics.DebuggerDisplayAttribute)))
             || (attrType.ReflectionOnlyEquals(typeof(System.Diagnostics.DebuggerStepThroughAttribute)))
             || (attrType.ReflectionOnlyEquals(typeof(System.Diagnostics.DebuggerTypeProxyAttribute)))
             || (attrType.ReflectionOnlyEquals(typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute)))
             || (attrType.ReflectionOnlyEquals(typeof(System.Runtime.CompilerServices.ExtensionAttribute))) // handled elsewhere.
             || (attrType.ReflectionOnlyEquals(typeof(System.Runtime.CompilerServices.TupleElementNamesAttribute))) // handled elsewhere.
             || (attrType.ReflectionOnlyEquals(typeof(System.Reflection.DefaultMemberAttribute))) // handled elsewhere.
             )
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            builder.Append('[');

            // Add the name (without the "Attribute" suffix).
            string shortName = Regex.Replace(attrType.Name, @"Attribute$", string.Empty);
            builder.Append(shortName);

            // Add property values, for specific types.
            string attrParams;
            if (attrType.FullName == "System.Web.Http.ActionNameAttribute"
                && attr.Constructor.GetParameters().Length == 1
                && attr.ConstructorArguments[0].ArgumentType.ReflectionOnlyEquals(typeof(string)))
            {
                // Get the "Name"
                string nameValue = (string)attr.ConstructorArguments[0].Value;
                attrParams = String.Format("\"{0}\"", nameValue);
            }
            else if (attrType.ReflectionOnlyEquals(typeof(System.ComponentModel.EditorBrowsableAttribute))
                     && attr.Constructor.GetParameters().Length == 1
                     && attr.ConstructorArguments[0].ArgumentType.ReflectionOnlyEquals(typeof(System.ComponentModel.EditorBrowsableState)))
            {
                attrParams = ((System.ComponentModel.EditorBrowsableState)(int)attr.ConstructorArguments[0].Value).ToString();
            }
            else
            {
                attrParams = null;
            }
            if (attrParams != null)
            {
                builder.AppendFormat("({0})", attrParams);
            }

            builder.Append(']');

            return builder.ToString();
        }

        public static string GetFriendlyTypeName(this Type type, ICustomAttributeProvider tupleAttributeProvider = null, bool includeGenericParmeterModifiers = true)
        {
            // Properly handle out and ref parameters.
            if (type.Name.EndsWith("&") && type.HasElementType)
            {
                type = type.GetElementType();
            }

            Type underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
            {
                // Nullable type.
                return underlyingNullableType.GetFriendlyTypeName(tupleAttributeProvider) + "?";
            }

            if (type.IsArray)
            {
                return String.Format("{0}[{1}]",
                                     type.GetElementType().GetFriendlyTypeName(tupleAttributeProvider),
                                     new string(',', type.GetArrayRank() - 1));
            }

            if (type == typeof(void)) { return "void"; }
            else if (type == typeof(Boolean)) { return "bool"; }
            else if (type == typeof(Int16)) { return "short"; }
            else if (type == typeof(Int32)) { return "int"; }
            else if (type == typeof(Int64)) { return "long"; }
            else if (type == typeof(Single)) { return "float"; }
            else if (type == typeof(Object) ||
                     type == typeof(Byte) ||
                     type == typeof(Double) ||
                     type == typeof(Decimal) ||
                     type == typeof(Char) ||
                     type == typeof(String))
            {
                return type.Name.ToLower();
            }

            string result = string.Empty;

            if (type.IsNested && (type.IsGenericParameter == false))
            {
                result = type.DeclaringType.GetFriendlyTypeName() + "+";
            }

            if (IsValueTuple(type))
            {
                result += '(';

                TupleElementNamesAttribute tupleElementNamesAttr
                    = tupleAttributeProvider?.GetCustomAttributes(typeof(TupleElementNamesAttribute), false)
                                            ?.Cast<TupleElementNamesAttribute>()
                                            ?.FirstOrDefault();

                Type[] tupleArgs = type.GetGenericArguments();
                var tupleArgElements = new List<string>(tupleArgs.Length);
                for (int i = 0; i < tupleArgs.Length; i++)
                {
                    Type tupleArg = tupleArgs[i];

                    string tupleElementText = tupleArg.GetFriendlyTypeName();

                    // Try and add the tuple element name from the attribute.
                    if (tupleElementNamesAttr != null)
                    {
                        tupleElementText += " " + tupleElementNamesAttr.TransformNames[i];
                    }

                    tupleArgElements.Add(tupleElementText);
                }

                result += string.Join(", ", tupleArgElements);

                result += ')';
            }
            else if (type.IsGenericType)
            {
                // Add the name of the type.
                result += type.Name.Substring(0, type.Name.IndexOf('`'));

                result += "<";

                // Add the parameters.
                List<string> typeParamItems = new List<string>();
                foreach (Type typeParam in type.GetGenericArguments())
                {
                    string item = string.Empty;

                    // Add any in/out modifier for the parameter.
                    if (typeParam.IsGenericParameter && includeGenericParmeterModifiers)
                    {
                        if (typeParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Contravariant))
                        {
                            item += "in ";
                        }
                        else if (typeParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.Covariant))
                        {
                            item += "out ";
                        }
                    }

                    // Add the name of the generic parameter.
                    item += typeParam.GetFriendlyTypeName();

                    typeParamItems.Add(item);
                }

                result += string.Join(", ", typeParamItems);

                result += ">";
            }
            else
            {
                // Plain old type name.
                result += type.Name;
            }

            return result;
        }

        private static bool IsValueTuple(Type type)
        {
            if (type.IsGenericType == false) { return false; }

            Type genericDefType = type.GetGenericTypeDefinition();

            return genericDefType.FullName.StartsWith("System.ValueTuple`")
                   &&
                   genericDefType.Assembly == typeof(ValueTuple<int>).Assembly;
        }

        public static string GetGenericConstraintText(Type[] genericArguments)
        {
            string result = String.Empty;

            foreach (Type genericParam in genericArguments)
            {
                List<string> constraintItems = new List<string>();

                // Inheritance constraints.
                foreach (Type constraint in genericParam.GetGenericParameterConstraints())
                {
                    // "struct" constraint puts an inheritance constraint on ValueType, but we don't want to show this.
                    if (constraint == typeof(ValueType)) { continue; }

                    constraintItems.Add(constraint.GetFriendlyTypeName(includeGenericParmeterModifiers: false));
                }

                // Other constraints.
                GenericParameterAttributes gpas = genericParam.GenericParameterAttributes;
                if (gpas.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                {
                    constraintItems.Add("struct");
                }
                else
                {
                    if (gpas.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                    {
                        constraintItems.Add("class");
                    }
                    if (gpas.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                    {
                        constraintItems.Add("new()");
                    }
                }

                if (constraintItems.Any())
                {
                    result += string.Format(CultureInfo.InvariantCulture,
                                            " where {0} : {1}",
                                            genericParam.Name,
                                            string.Join(", ", constraintItems));
                }
            }

            return result.Trim();
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "C# strings are in lower case")]
        public static string ToCSharpText(this MemberAttributes attribute)
        {
            return attribute.ToString().ToLowerInvariant().Replace('_', ' ');
        }

        public static bool IsHiding(this MethodInfo info)
        {
            if (info.DeclaringType.IsEnum)
            {
                return false;
            }

            if (info.DeclaringType.IsInterface)
            {
                return IsHidingInterface(info);
            }
            else
            {
                return IsHidingClass(info);
            }
        }

        private static bool IsHidingInterface(MethodInfo info)
        {
            return info.DeclaringType.GetInterfaces()
                                     .SelectMany(i => i.GetMethods())
                                     .Any(mi => IsHiddenMethodCandidate(info, mi));
        }

        private static bool IsHidingClass(MethodInfo info)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public
                                 | BindingFlags.Static | BindingFlags.Instance
                                 | BindingFlags.ExactBinding;

            List<MethodInfo> candidateMethods = new List<MethodInfo>();
            foreach (MethodInfo mi in info.DeclaringType.BaseType.GetMethods(flags))
            {
                if (IsHiddenMethodCandidate(info, mi))
                {
                    // Found a method with the same name and generic arguments.
                    candidateMethods.Add(mi);
                }
            }

            // Ensure there is only one candidate method - otherwise something is weird.
            var baseMethod = candidateMethods.SingleOrDefault();

            if (baseMethod == null)
            {
                // No method with the same signature on the base class.
                return false;
            }

            if (info.IsVirtual)
            {
                // If the reflected method is virtual... need to see if we are overriding or hiding.
                // If the VtableLayoutMask attribute is present, it means we are hiding, otherwise we are overriding.
                if (info.Attributes.HasFlag(MethodAttributes.VtableLayoutMask) == false)
                {
                    // VtableLayoutMask is not present, so we are not hiding.
                    return false;
                }
            }

            // Looks like everything worked out - we are hiding.
            return true;
        }

        private static bool IsHiddenMethodCandidate(MethodInfo info, MethodInfo candidate)
        {
            // If the base method is not exposed, don't consider it.
            if ((candidate.IsPublic || candidate.IsFamily || candidate.IsFamilyOrAssembly) == false)
            {
                return false;
            }

            // If the names aren't the same, we obviously aren't hiding it.
            if (candidate.Name != info.Name) { return false; }

            var paramTypes = info.GetParameters().Select(p => p.ParameterType);
            var baseParamTypes = candidate.GetParameters().Select(p => p.ParameterType);
            if (paramTypes.SequenceEqual(baseParamTypes) == false)
            {
                // Methods have different parameter types.
                return false;
            }

            if (info.IsGenericMethod)
            {
                if (candidate.IsGenericMethod == false)
                {
                    // We are a generic method, but this one from the parent isn't, so it's not a candidate.
                    return false;
                }

                // Ensure the methods have the same number of generic arguments.
                if (info.GetGenericArguments().Length != candidate.GetGenericArguments().Length) { return false; }
            }

            return true;
        }

    }
}

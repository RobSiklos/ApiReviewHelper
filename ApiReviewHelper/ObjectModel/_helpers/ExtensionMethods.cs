using System;
using System.Linq;
using System.Reflection;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    internal static class ExtensionMethods
    {
        public static bool ReflectionOnlyIsDefined(this MemberInfo info, Type attributeType)
        {
            return info.GetCustomAttributesData().Any(c => c.AttributeType == attributeType);
        }

        public static bool ReflectionOnlyIsDefined(this ParameterInfo info, Type attributeType)
        {
            return info.GetCustomAttributesData().Any(c => c.AttributeType == attributeType);
        }

        public static bool ReflectionOnlyHasDefaultValue(this ParameterInfo info)
        {
            try
            {
                return info.RawDefaultValue != DBNull.Value;
            }
            catch (FormatException)
            {
                // Bug in .NET - FormatException thrown if default value is a non-primitive value type (e.g. DateTime).
                // See http://stackoverflow.com/questions/22912021/exception-at-initialization-of-datetime
                // and https://web.archive.org/web/20130325022304/http://connect.microsoft.com/VisualStudio/feedback/details/733995/datetime-default-parameter-value-throws-formatexception-at-runtime
                return false;
            }
        }

        public static bool ReflectionOnlyEquals(this Type type, Type otherType)
        {
            return type.AssemblyQualifiedName == otherType.AssemblyQualifiedName;
        }

        public static bool IsNumeric(this Type type)
        {
            return type == typeof(byte)
                   || type == typeof(Int16)
                   || type == typeof(Int32)
                   || type == typeof(Int64)
                   || type == typeof(UInt16)
                   || type == typeof(UInt32)
                   || type == typeof(UInt64)
                   || type == typeof(Single)
                   || type == typeof(Double)
                   || type == typeof(Decimal);
        }
    }
}

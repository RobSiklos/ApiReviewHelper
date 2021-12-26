using System;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    public static class LangWord
    {
        public const string This = "this";

        public const string Null = "null";

        public const string False = "false";

        public const string True = "true";

        public const string Out = "out";

        public const string Ref = "ref";

        public const string Params = "params";

        public const string Default = "default";

        public static string FromBoolean(bool value)
        {
            return value ? True : False;
        }
    }
}

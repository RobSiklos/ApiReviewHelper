using System;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [Flags]
    internal enum MemberAttributes
    {
        None = 0x0,
        New = 0x1,
        //Private = 0x2,
        Protected = 0x4,
        Protected_Internal = 0x8,
        Public = 0x10,
        Static = 0x20,
        Virtual = 0x40,
        Abstract = 0x80,
        Sealed = 0x100,
        Override = 0x200,
        Async = 0x400,
        Readonly = 0x800,
        Const = 0x1000,
        Event = 0x2000,
    }
}

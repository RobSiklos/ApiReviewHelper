namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Justification = "False positive")]
    internal enum MemberType
    {
        Unknown = 0,
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        Delegate = 17,
        TypeInfo = 32,
    }
}

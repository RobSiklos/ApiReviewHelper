using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class FieldSignatureTests
    {
        [TestMethod]
        public void FieldSig_ConstString_ShouldContainLiteralValue()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetField("ConstantString"));

            string expected = "public const string ConstantString = \"ConstantStringValue\"";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void FieldSig_StaticReadOnlyGuid_ShouldContainLiteralValue()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetField("StaticReadonlyGuid"));

            string expected = "public static readonly Guid StaticReadonlyGuid = \"11a5e294-8f45-4848-9547-01f01d809b62\"";
            Assert.AreEqual(expected, md.Signature);
        }
    }
}

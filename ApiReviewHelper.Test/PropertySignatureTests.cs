using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class PropertySignatureTests
    {
        [TestMethod]
        public void PropSig_ValueTuple()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetProperty("ValueTupleProp"));

            string expected = "public (int A, int B) ValueTupleProp { get; set; }";
            Assert.AreEqual(expected, md.Signature);
        }

    }
}

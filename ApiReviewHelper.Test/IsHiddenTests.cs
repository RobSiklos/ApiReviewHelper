using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Robzilla888.ApiReviewHelper.ObjectModel;

using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class IsHiddenTests
    {
        [TestMethod]
        public void IsHidden_PrivateHidingPublic_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("M1");

            Assert.IsFalse(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_ProtectedHidingPublic_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("M2", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance);

            // Make sure we're testing the right thing.
            Assert.IsTrue(method.IsFamily);

            // Make sure the result is correct.
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_PublicHidingProtected_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("M3");
            MethodInfo baseMethod = typeof(TS.Base).GetMethod("M3", BindingFlags.Instance | BindingFlags.NonPublic);

            // Make sure we're testing the right thing.
            Assert.IsTrue(method.IsPublic);
            Assert.IsTrue(baseMethod.IsFamily);

            // Make sure the result is correct.
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_AnythingOnAStruct_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.S1).GetMethod("Equals", new Type[] { typeof(TS.S1) });

            bool isHiding = method.IsHiding();

            Assert.IsFalse(isHiding);
        }

        [TestMethod]
        public void IsHidden_SealedVirtual_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("VirtualMethod");

            bool isHiding = method.IsHiding();

            Assert.IsFalse(isHiding);
        }

        [TestMethod]
        public void IsHidden_VirtualOverridden_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("VirtualOverridden");
            Assert.IsFalse(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_VirtualHiddenAsVirtual_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("VirtualHiddenAsVirtual");
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_VirtualHiddenAsNonVirtual_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("VirtualHiddenAsNonVirtual");
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_NonVirtualHiddenAsVirtual_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("NonVirtualHiddenAsVirtual");
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_NonVirtualHiddenAsNonVirtual_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("NonVirtualHiddenAsNonVirtual");
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_NotHiddenGeneric_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.Derived).GetMethod("Clone", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual("T Clone[T]()", method.ToString());
            Assert.IsFalse(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_Interface_ShouldReturnTrue()
        {
            MethodInfo method = typeof(TS.IAlsoHelpTesting).GetMethod("InterfaceMethod");
            Assert.IsTrue(method.IsHiding());
        }

        [TestMethod]
        public void IsHidden_Interface_ShouldReturnFalse()
        {
            MethodInfo method = typeof(TS.IAlsoHelpTesting).GetMethod("NotHiding");
            Assert.IsFalse(method.IsHiding());
        }
    }
}

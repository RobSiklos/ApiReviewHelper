using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class TypeSignatureTests
    {
        [TestMethod]
        public void TypeSig_VarianceConstraint_ShouldAppear()
        {
            // Arrange.
            Type t = typeof(TS.IGarble<>);

            // Act.
            TypeData td = new TypeData(t);

            // Assert.
            string expected = "public interface IGarble<out T>";
            Assert.AreEqual(expected, td.Signature);
        }

        [TestMethod]
        public void TypeSig_GenericConstraint_ShouldAppear()
        {
            // Arrange.
            Type t = typeof(TS.IGenericConstraint<>);

            // Act.
            TypeData td = new TypeData(t);

            // Assert.
            string expected = "public interface IGenericConstraint<in T> where T : List<T>";
            Assert.AreEqual(expected, td.Signature);
        }

        [TestMethod]
        public void TypeSig_InternalInterfaceWhichImplementsPublicInterface()
        {
            // Arrange.
            Type t = typeof(TS.PublicClassImplementingInternalInterface);

            // Act.
            TypeData td = new TypeData(t);

            // Assert.
            string expected = "public class PublicClassImplementingInternalInterface : IHelpTesting";
            Assert.AreEqual(expected, td.Signature);
        }
    }
}

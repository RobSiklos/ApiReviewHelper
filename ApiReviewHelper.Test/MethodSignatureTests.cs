using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class MethodSignatureTests
    {
        [TestMethod]
        public void MethodSig_ProtectedHider_ShouldContainNew()
        {
            MemberData md = new MemberData(typeof(TestSubjects.Derived).GetMethod("M2", BindingFlags.Instance | BindingFlags.NonPublic));

            string expected = "new protected void M2(bool b)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_InterfaceMethod_ShouldContainInterfaceName()
        {
            // Arrange.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            MethodInfo mi = typeof(TS.Base).GetMethod("InterfaceMethod", flags);

            // Act.
            MemberData md = new MemberData(mi);

            // Assert.
            string expected = "(IHelpTesting) public virtual void InterfaceMethod()";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_PublicMethodDeclaredOnInternalInterface_ShouldNotContainInterfaceName()
        {
            // Arrange.
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
            MethodInfo mi = typeof(TS.Base).GetMethod("PublicMethodDeclaredOnInternalInterface", flags);

            // Act.
            MemberData md = new MemberData(mi);

            // Assert.
            string expected = "public void PublicMethodDeclaredOnInternalInterface()";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_SealedVirtual()
        {
            MemberData md = new MemberData(typeof(TS.Derived).GetMethod("VirtualMethod"));

            // Assert.
            string expected = "public sealed override void VirtualMethod()";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_ContainsAsync()
        {
            MethodInfo mi = typeof(TS.Foo).GetMethod("DoSomethingAsync");
            MemberData md = new MemberData(mi);

            // Assert.
            string expected = "public virtual async void DoSomethingAsync()";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_DoesNotContainAsync()
        {
            MethodInfo mi = typeof(TS.Foo).GetMethod("DoSomething");
            MemberData md = new MemberData(mi);

            // Assert.
            Assert.IsFalse(md.Signature.Contains(" async "));
        }

        [TestMethod]
        public void MethodSig_ExtensionMethod_ShouldContainThis()
        {
            MethodInfo mi = typeof(TS.IGarbleTExtensions).GetMethod("Mumble");
            MemberData md = new MemberData(mi);

            // Assert.
            string expected = "public static int Mumble<T>(this IGarble<T> garble)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_GenericConstraint_ShouldContainClass()
        {
            MethodInfo mi = typeof(TS.IGarble<>).GetMethod("GenericConstraintClass");
            MemberData md = new MemberData(mi);

            // Assert.
            string expected = "T GenericConstraintClass<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3) where T1 : class, new() where T2 : struct where T3 : new()";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_Indexer_ShouldContainParamTypes()
        {
            // Single-parameter indexer.
            PropertyInfo propInfo = typeof(TS.Foo).GetProperties()
                                                     .Where(p => p.Name == "Item" && p.GetIndexParameters().Length == 1)
                                                     .Single();

            MemberData md = new MemberData(propInfo);

            string expected = "public string this[int index] { get; }";
            Assert.AreEqual(expected, md.Signature, "Single-parameter");

            // Multi-parameter indexer.
            propInfo = typeof(TS.Foo).GetProperties()
                                     .Where(p => p.Name == "Item" && p.GetIndexParameters().Length == 2)
                                     .Single();

            md = new MemberData(propInfo);

            expected = "public string this[string name, int something] { get; }";
            Assert.AreEqual(expected, md.Signature, "Multi-parameter");

        }

        [TestMethod]
        public void MethodSig_OutAndRef()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetMethod("OutAndRef"));

            // Assert.
            string expected = "public void OutAndRef(int regular, out int outParam, ref int refParam, [Out] Int32* fakeOutParam)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_Params()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetMethod("MethodWithParams"));

            // Assert.
            string expected = "public void MethodWithParams(int foo, params string[] args)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_DefaultParamValues()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetMethod(nameof(TS.Foo.MethodWithDefaultParamValues)));

            // Assert.
            string expected = "public void MethodWithDefaultParamValues(int i = 5, string s = \"s123\", TestEnum e = TestEnum.B, bool b = true, DateTime d = default(DateTime))";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_ValueTuples()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetMethod(nameof(TS.Foo.MethodWithValueTuples)));

            string expected = "public (int Num, string Text) MethodWithValueTuples(int x, (float FloatNum, Version Ver) tuple, (int X, int Y)[] tupArr, out (DateTime MyDate, Task MyTask) MyOutVar)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_ValueTuplesNullable()
        {
            MemberData md = new MemberData(typeof(TS.Foo).GetMethod(nameof(TS.Foo.MethodWithValueTuplesNullable)));

            string expected = "public (int Num, string Text)? MethodWithValueTuplesNullable(int x, (float FloatNum, Version Ver)? tuple, out (DateTime MyDate, Task MyTask)? MyOutVar)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_ValueTuples_Interface()
        {
            MemberData md = new MemberData(typeof(TS.IHelpTesting).GetMethod(nameof(TS.IHelpTesting.ValueTupleInterfaceMethod)));

            string expected = "(int X, double Y) ValueTupleInterfaceMethod(Guid id, out (int A, Version Version) result)";
            Assert.AreEqual(expected, md.Signature);
        }

        [TestMethod]
        public void MethodSig_ValueTuples_GenericInterface()
        {
            MemberData md = new MemberData(typeof(TS.IGenericInterface<>).GetMethod("ValueTupleInterfaceMethod"));

            string expected = "(int X, double Y) ValueTupleInterfaceMethod(Guid id, out (T A, Version Version) result)";
            Assert.AreEqual(expected, md.Signature);
        }
    }
}

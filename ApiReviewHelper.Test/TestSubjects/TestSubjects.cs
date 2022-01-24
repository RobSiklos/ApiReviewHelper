using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects
{
    public enum TestEnum
    {
        A,
        B,
        C,
    }

    public interface IHelpTesting
    {
        void InterfaceMethod();

        (int X, double Y) ValueTupleInterfaceMethod(Guid id, out (int A, Version Version) result);
    }

    public interface IAlsoHelpTesting : IHelpTesting
    {
        new void InterfaceMethod();
        void NotHiding();
    }

    internal interface IHelpTestingInternal : IHelpTesting
    {
        void PublicMethodDeclaredOnInternalInterface();
    }

    public interface IGenericInterface<T>
    {
        (int X, double Y) ValueTupleInterfaceMethod(Guid id, out (T A, Version Version) result);
    }

    public struct S1
    {
        public bool Equals(S1 other) { throw new NotImplementedException(); }
    }

    public class Foo
    {
        public const string ConstantString = "ConstantStringValue";

        public static readonly Guid StaticReadonlyGuid = new Guid("11a5e294-8f45-4848-9547-01f01d809b62");

        // Private constructor.
        private Foo() { }

        public (int A, int B) ValueTupleProp { get; set; }

        public virtual void DoSomething()
        {
        }

        public async virtual void DoSomethingAsync()
        {
            await Task.Delay(1000);
        }

        public string this[int index]
        {
            get { throw null; }
        }

        public string this[string name, int something]
        {
            get { throw null; }
        }

        public unsafe void OutAndRef(int regular, out int outParam, ref int refParam, [Out] int* fakeOutParam)
        {
            outParam = 0;
        }

        public void MethodWithParams(int foo, params string[] args)
        {
        }

        public void MethodWithDefaultParamValues(
            int i = 5,
            string s = "s123",
            TestEnum e = TestEnum.B,
            bool b = true,
            DateTime d = default(DateTime))
        {
        }

        public (int Num, string Text) MethodWithValueTuples(int x, (float FloatNum, Version Ver) tuple, (int X, int Y)[] tupArr, out (DateTime MyDate, Task MyTask) MyOutVar)
        {
            MyOutVar = (DateTime.Now, null);
            return (5, "foo");
        }


        public (int Num, string Text)? MethodWithValueTuplesNullable(int x, (float FloatNum, Version Ver)? tuple, out (DateTime MyDate, Task MyTask)? MyOutVar)
        {
            MyOutVar = null;
            return null;
        }
    }

    public interface IGenericConstraint<in T>
        where T : List<T>
    {
    }

    public interface IGarble<out T>
    {
        void Garble();

        T GenericConstraintClass<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
            where T1 : class, new()
            where T2 : struct
            where T3 : new();
    }

    public static class IGarbleTExtensions
    {
        public static int Mumble<T>(this IGarble<T> garble)
        {
            return garble.GetHashCode();
        }
    }

    public class Base : IHelpTesting, IHelpTestingInternal
    {
        public int P1 { get; private protected set; }

        public void M1(bool b) { }
        public void M2(bool b) { }
        protected void M3() { }
        private protected void M4() { }

        public virtual void InterfaceMethod() { }

        public void PublicMethodDeclaredOnInternalInterface() { }

        public virtual void VirtualMethod() { }

        public virtual void VirtualOverridden() { }
        public virtual void VirtualHiddenAsVirtual() { }
        public virtual void VirtualHiddenAsNonVirtual() { }
        public void NonVirtualHiddenAsVirtual() { }
        public void NonVirtualHiddenAsNonVirtual() { }

        public virtual Base Clone()
        {
            throw new NotImplementedException();
        }

        public (int X, double Y) ValueTupleInterfaceMethod(Guid id, out (int A, Version Version) result)
        {
            throw new NotImplementedException();
        }
    }

    public class Derived : Base
    {
        private new void M1(bool b) { }
        protected new void M2(bool b) { }

        public new void M3() { base.M3(); }

        public override void InterfaceMethod() { }

        /// <inheritdoc/>
        public sealed override void VirtualMethod()
        {
            base.VirtualMethod();
        }

        public override void VirtualOverridden() { }
        public new virtual void VirtualHiddenAsVirtual() { }
        public new void VirtualHiddenAsNonVirtual() { }
        public new virtual void NonVirtualHiddenAsVirtual() { }
        public new void NonVirtualHiddenAsNonVirtual() { }

        public override Base Clone()
        {
            throw new NotImplementedException();
        }

        protected virtual T Clone<T>() where T : Derived, new()
        {
            throw new NotImplementedException();
        }

    }

    internal class InternalClass
    {
        public class PublicNestedOfInternal
        {
            public class PublicNestedOfPublicNestedOfInternal { }
            protected class ProtectedNestedOfPublicNestedOfInternal { }
            internal class InternalNestedOfPublicNestedOfInternal { }
        }

        protected class ProtectedNestedOfInternal
        {
            public class PublicNestedOfProtectedNestedOfInternal { }
            protected class ProtectedNestedOfProtectedNestedOfInternal { }
            internal class InternalNestedOfProtectedNestedOfInternal { }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class PublicClass
    {
        public class PublicNestedOfPublic
        {
            public class PublicNestedOfPublicNestedOfPublic { }
            protected class ProtectedNestedOfPublicNestedOfPublic { }
            internal class InternalNestedOfPublicNestedOfPublic { }
        }

        protected class ProtectedNestedOfPublic
        {
            public class PublicNestedOfProtectedNestedOfPublic { }
            protected class ProtectedNestedOfProtectedNestedOfPublic { }
            internal class InternalNestedOfProtectedNestedOfPublic { }
        }

        private protected class PrivateProtectedNestedOfPublic
        {
        }
    }

    public class PublicClassImplementingInternalInterface : IHelpTestingInternal
    {
        public void InterfaceMethod() => throw new NotImplementedException();

        public void PublicMethodDeclaredOnInternalInterface() => throw new NotImplementedException();

        public (int X, double Y) ValueTupleInterfaceMethod(Guid id, out (int A, Version Version) result) => throw new NotImplementedException();
    }
}

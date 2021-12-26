using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TS = Robzilla888.ApiReviewHelper.ObjectModel.Test.TestSubjects;

namespace Robzilla888.ApiReviewHelper.ObjectModel.Test
{
    [TestClass]
    public class VisibilityTests
    {
        [TestMethod]
        public void NonExportedNestedTypesOfInternalClass_ShouldNotBeVisible()
        {
            var ad = new AssemblyData(this.GetType().Assembly);

            var nsd = ad.Namespaces.Single(ns => ns.Name == typeof(TS.InternalClass).Namespace);

            string typeNamePrefix = typeof(TS.InternalClass).Name + "+";
            IEnumerable<TypeData> visibleNestedTypes = nsd.Types.Where(t => t.Name.StartsWith(typeNamePrefix));

            Assert.IsFalse(visibleNestedTypes.Any(), string.Join(", ", visibleNestedTypes.Select(t => t.Name)));
        }

        [TestMethod]
        public void ExportedNestedTypesOfPublicClass_ShouldBeVisible()
        {
            var ad = new AssemblyData(this.GetType().Assembly);

            var nsd = ad.Namespaces.Single(ns => ns.Name == typeof(TS.PublicClass).Namespace);

            string typeNamePrefix = typeof(TS.PublicClass).Name + "+";

            string[] expectedTypeNames = new[] {
                "PublicClass+PublicNestedOfPublic",
                "PublicClass+PublicNestedOfPublic+PublicNestedOfPublicNestedOfPublic",
                "PublicClass+PublicNestedOfPublic+ProtectedNestedOfPublicNestedOfPublic",
                "PublicClass+ProtectedNestedOfPublic",
                "PublicClass+ProtectedNestedOfPublic+PublicNestedOfProtectedNestedOfPublic",
                "PublicClass+ProtectedNestedOfPublic+ProtectedNestedOfProtectedNestedOfPublic",
            };

            List<TypeData> visibleNestedTypes = nsd.Types.Where(t => t.Name.StartsWith(typeNamePrefix)).ToList();

            // Assert.
            Assert.AreEqual(expectedTypeNames.Length, visibleNestedTypes.Count);

            foreach (TypeData td in visibleNestedTypes)
            {
                Assert.IsTrue(expectedTypeNames.Contains(td.Name), "{0} was not expected", td.Name);
            }
        }
    }
}

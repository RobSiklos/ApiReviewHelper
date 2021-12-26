using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    internal static class DiffHelper
    {
        public static void CreateDiff(string baselineFile1, string baselineFile2, string outputFileName)
        {
            // Create the output dir if it doesn't exist.
            string outputDir = Path.GetDirectoryName(outputFileName);
            if (outputDir.Length > 0 && Directory.Exists(outputDir) == false)
            {
                Directory.CreateDirectory(outputDir);
            }

            ApiData api1 = ApiData.ReadXml(baselineFile1);
            ApiData api2 = ApiData.ReadXml(baselineFile2);

            using (StreamWriter w = File.CreateText(outputFileName))
            {
                w.WriteLine("<html>");
                w.WriteLine(@"
    <head>
        <title>API Diff " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</title>
        <style rel=""stylesheet"">
          .added { color: DarkGreen; }
          .removed { color: DarkRed; }
          .mono { font-family: Monospace; }
          h2 { font-family: Monospace; margin-bottom: 0; }
        </style>
    </head>");

                w.WriteLine("<body>");

                w.WriteLine("<h1>API Diff</h1>");

                w.WriteLine("<b>Date:</b> {0}<br/>", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                w.WriteLine("<b>Baseline 1:</b> {0}<br/>", HttpUtility.HtmlEncode(baselineFile1));
                w.WriteLine("<b>Baseline 2:</b> {0}<br/>", HttpUtility.HtmlEncode(baselineFile2));

                w.WriteLine("<hr/>");

                ProcessApis(w, api1, api2);

                w.WriteLine("</body></html>");
            }
        }

        private static void ProcessApis(StreamWriter w, ApiData api1, ApiData api2)
        {
            if (api1.IsEquivalentTo(api2))
            {
                w.WriteLine("No differences.");
            }
            else
            {
                // Go through the assemblies and process the ones which are different.

                string[] allAssemblies = api1.Assemblies
                                             .Concat(api2.Assemblies)
                                             .Select(a => a.Name)
                                             .Distinct()
                                             .ToArray();

                foreach (string assemblyName in allAssemblies)
                {
                    AssemblyData ad1 = api1.Assemblies.FirstOrDefault(ad => ad.Name == assemblyName);
                    AssemblyData ad2 = api2.Assemblies.FirstOrDefault(ad => ad.Name == assemblyName);
                    ProcessAssemblies(w, ad1, ad2);
                }
            }
        }

        private static void ProcessAssemblies(StreamWriter w, AssemblyData ad1, AssemblyData ad2)
        {
            List<Tuple<NamespaceData, NamespaceData>> childrenToCompare = new List<Tuple<NamespaceData, NamespaceData>>();
            if (ad1 == null)
            {
                // Entire assembly ad2 was added.
                childrenToCompare.AddRange(ad2.Namespaces.Select(nsd => new Tuple<NamespaceData, NamespaceData>(null, nsd)));
            }
            else if (ad2 == null)
            {
                // Entire assembly ad1 was removed.
                childrenToCompare.AddRange(ad1.Namespaces.Select(nsd => new Tuple<NamespaceData, NamespaceData>(nsd, null)));
            }
            else if (ad1.IsEquivalentTo(ad2))
            {
                // Assembly is present in both, and there are no differences.
                // Nothing to do.
            }
            else
            {
                // Assembly is present in both, but there are differences.

                IEnumerable<string> allNamespaces 
                    = ad1.Namespaces
                         .Concat(ad2.Namespaces)
                         .Select(ns => ns.Name)
                         .Distinct()
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
                foreach (string nsName in allNamespaces)
                {
                    NamespaceData nsd1 = ad1.Namespaces.FirstOrDefault(nsd => nsd.Name == nsName);
                    NamespaceData nsd2 = ad2.Namespaces.FirstOrDefault(nsd => nsd.Name == nsName);
                    childrenToCompare.Add(new Tuple<NamespaceData, NamespaceData>(nsd1, nsd2));
                }
            }

            if (childrenToCompare.Any())
            {
                // Write out the assembly name.
                w.WriteLine("<h1>{0}</h1>", HttpUtility.HtmlEncode((ad2 ?? ad1).Name));
                w.WriteLine(HttpUtility.HtmlEncode((ad2 ?? ad1).FullName));

                w.WriteLine("<ul>");
                foreach (Tuple<NamespaceData, NamespaceData> nsPair in childrenToCompare)
                {
                    ProcessNamespaces(w, nsPair.Item1, nsPair.Item2);
                }
                w.WriteLine("</ul>");
            }
        }

        private static void ProcessNamespaces(StreamWriter w, NamespaceData nsd1, NamespaceData nsd2)
        {
            List<Tuple<TypeData, TypeData>> childrenToCompare = new List<Tuple<TypeData, TypeData>>();
            if (nsd1 == null)
            {
                // Entire namespace nsd2 was added.
                childrenToCompare.AddRange(nsd2.Types.Select(td => new Tuple<TypeData, TypeData>(null, td)));
            }
            else if (nsd2 == null)
            {
                // Entire namespace nsd1 was removed.
                childrenToCompare.AddRange(nsd1.Types.Select(td => new Tuple<TypeData, TypeData>(td, null)));
            }
            else if (nsd1.IsEquivalentTo(nsd2))
            {
                // Namespace is present in both, and there are no differences.
                // Nothing to do.
            }
            else
            {
                // Namespace is present in both, but there are differences.

                IEnumerable<string> allTypes 
                       = nsd1.Types
                             .Concat(nsd2.Types)
                             .Select(td => td.Name)
                             .Distinct()
                             .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
                foreach (string typeName in allTypes)
                {
                    TypeData td1 = nsd1.Types.FirstOrDefault(td => td.Name == typeName);
                    TypeData td2 = nsd2.Types.FirstOrDefault(td => td.Name == typeName);
                    childrenToCompare.Add(new Tuple<TypeData, TypeData>(td1, td2));
                }
            }

            if (childrenToCompare.Any())
            {
                // Write out the namespace name.
                w.WriteLine("<h2>{0}</h2>", HttpUtility.HtmlEncode((nsd1 ?? nsd2).Name));

                w.WriteLine("<ul>");
                foreach (Tuple<TypeData, TypeData> typePair in childrenToCompare)
                {
                    ProcessTypes(w, typePair.Item1, typePair.Item2);
                }
                w.WriteLine("</ul>");
            }
        }

        private static void ProcessTypes(StreamWriter w, TypeData td1, TypeData td2)
        {
            string signatureChange = null;
            bool isAdded = false;
            bool isRemoved = false;

            List<Tuple<MemberData, MemberData>> childrenToCompare = new List<Tuple<MemberData, MemberData>>();
            if (td1 == null)
            {
                // Entire type td2 was added.
                isAdded = true;
                signatureChange = String.Format("<div class='mono added'>{0}</div>", HttpUtility.HtmlEncode(td2.Signature));
                childrenToCompare.AddRange(td2.Members.Select(md => new Tuple<MemberData, MemberData>(null, md)));
            }
            else if (td2 == null)
            {
                // Entire type td1 was removed.
                isRemoved = true;
                childrenToCompare.AddRange(td1.Members.Select(md => new Tuple<MemberData, MemberData>(md, null)));
            }
            else if (td1.IsEquivalentTo(td2))
            {
                // Type is present in both, and there are no differences.
                // Nothing to do.
            }
            else
            {
                // Type is present in both, but there are differences.

                // Show differences in the signature.
                if (td1.Signature != td2.Signature)
                {
                    signatureChange = String.Format("<div class='mono removed'>{0}</div>", HttpUtility.HtmlEncode(td1.Signature))
                                      + String.Format("<div class='mono added'>{0}</div>", HttpUtility.HtmlEncode(td2.Signature));
                }

                // Show differences in the members.
                IEnumerable<string> allMembers =
                    td1.Members
                       .Concat(td2.Members)
                       .Select(md => md.Signature)
                       .Distinct();
                foreach (string memberSig in allMembers)
                {
                    MemberData md1 = td1.Members.FirstOrDefault(td => td.Signature == memberSig);
                    MemberData md2 = td2.Members.FirstOrDefault(td => td.Signature == memberSig);
                    childrenToCompare.Add(new Tuple<MemberData, MemberData>(md1, md2));
                }
            }

            if (signatureChange != null || childrenToCompare.Any())
            {
                string styleClass = null;
                string prefix = String.Empty;
                if (isAdded)
                {
                    styleClass = "added";
                    prefix = "[+] ";
                }
                else if (isRemoved)
                {
                    styleClass = "removed";
                    prefix = "[-] ";
                }

                // Write out the type name.
                w.WriteLine("<h3 {0} title=\"{1}\">{2}{3}</h3>",
                            (styleClass == null) ? string.Empty : "class=\"" + styleClass + "\"",
                            HttpUtility.HtmlEncode((td1 ?? td2).Signature),
                            prefix,
                            HttpUtility.HtmlEncode((td1 ?? td2).Name));

                if (signatureChange != null)
                {
                    w.WriteLine(signatureChange);
                }

                IEnumerable<Tuple<MemberData, MemberData>> orderedChildren;
                if ((td1 ?? td2).TypeVariant == TypeVariant.Enum)
                {
                    // Keep the existing order for enums.
                    orderedChildren = childrenToCompare;
                }
                else
                {
                    // Order the members by signature.
                    orderedChildren = childrenToCompare.OrderBy(pair => (pair.Item1 ?? pair.Item2).Signature, StringComparer.OrdinalIgnoreCase);
                }

                w.WriteLine("<ul class='mono'>");
                foreach (Tuple<MemberData, MemberData> memberPair in orderedChildren)
                {
                    ProcessMembers(w, memberPair.Item1, memberPair.Item2);
                }
                w.WriteLine("</ul>");
            }
        }

        private static void ProcessMembers(StreamWriter w, MemberData md1, MemberData md2)
        {
            if (md1 == null)
            {
                // md2 was added.
                w.WriteLine("<li class='added'>[+] {0}</li>", HttpUtility.HtmlEncode(md2.Signature));
            }
            else if (md2 == null)
            {
                // md1 was removed.
                w.WriteLine("<li class='removed'>[-] {0}</li>", HttpUtility.HtmlEncode(md1.Signature));
            }
            else if (md1.IsEquivalentTo(md2))
            {
                // Members are the same - do nothing.
            }
            else
            {
                // should never be here
                throw new InvalidOperationException("huh?");
            }
        }
    }
}

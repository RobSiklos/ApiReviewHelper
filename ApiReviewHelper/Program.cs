using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Robzilla888.ApiReviewHelper.ObjectModel;

namespace Robzilla888.ApiReviewHelper
{
    /*

    // Mode 1: Create baseline

    // Read list of assemblies to reflect on into an in-memory object.
    // Serialize the in-memory object to XML.
    // Write the XML to a file.

    /* List of types, along with
     *   - assembly
     *   - namespace
     * List of members in a type, along with
     *   - parent type
     *   - member type
     *   - signature
     *   - attributes
     * 
     * 1. Get distinct list of assemblies
     * 1.1 for each assembly: get distinct list of namespaces
     * 1.2 for each namespace, get all the types in that namespace for that assembly
     * 
     * 
     * Need:
     *   - AssemblyData
     *   - TypeData
     *   - MemberData
     *


    // Mode 2: Compare baselines

    // Read both baselines into in-memory objects.
    // Find the differences
    // Write the differences to HTML
    */
    internal class Program
    {
        #region Nested Types

        private enum OutputType
        {
            Html,
            Xml,
        }

        #endregion Nested Types

        #region Methods - Private

        static void Main(string[] args)
        {
            /* Process arguments:
             * create-baseline assemblieslist outputfile outputtype
             * create-diff baseline1 baseline2 outputfile
             */

#if DEBUG
            if (args[0] == "attach")
            {
                args = args.Skip(1).ToArray();
                Debugger.Launch();
            }
#endif

            bool ranCommand = false;
            if (args.Length > 0)
            {
                if (args[0] == "create-baseline" && args.Length == 4)
                {
                    CreateBaseline(args.Skip(1).ToArray());
                    ranCommand = true;
                }
                else if (args[0] == "create-diff" && args.Length == 4)
                {
                    CreateDiff(args.Skip(1).ToArray());
                    ranCommand = true;
                }
            }

            if (ranCommand == false)
            {
                PrintUsage();
            }
        }

        private static void PrintUsage()
        {
            string progName = Assembly.GetExecutingAssembly().GetName().Name + ".exe";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Create Baseline:");
            builder.Append("  " + progName);
            builder.AppendLine(" create-baseline <AssembliesListFile> <OutputFile> <OutputType>");

            builder.AppendLine();

            builder.AppendLine("Create Diff:");
            builder.Append("  " + progName);
            builder.AppendLine(" create-diff <Baseline1> <Baseline2> <OutputFile>");

            Console.Error.WriteLine(builder.ToString());
        }

        private static void CreateBaseline(string[] args)
        {
            string listFileName = args[0] ?? String.Empty;
            string outputFileName = args[1] ?? String.Empty;
            string outputTypeArg = args[2] ?? String.Empty;

            if (File.Exists(listFileName) == false)
            {
                Console.Error.WriteLine("File not found: {0}", listFileName);
                return;
            }

            OutputType outputType;
            if (Enum.TryParse(outputTypeArg, true, out outputType) == false)
            {
                Console.Error.WriteLine("Output type must be XML or HTML");
                return;
            }

            // Read the list of files from the list file.
            string[] lines = File.ReadAllLines(listFileName);
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string rawAssemblyPath in lines)
            {
                if (rawAssemblyPath.Trim().Length == 0) { continue; }

                string assemblyPath;
                if (Path.IsPathRooted(rawAssemblyPath))
                {
                    assemblyPath = rawAssemblyPath;
                }
                else
                {
                    // Relative path.
                    // Convert to full path, relative to the location of the list file.
                    assemblyPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(listFileName), rawAssemblyPath));
                }

                if (File.Exists(assemblyPath) == false)
                {
                    Console.Error.WriteLine("Assembly not found: {0}", assemblyPath);
                    continue;
                }

                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                // Make sure we can get all the types from each assembly. Otherwise, we're in trouble down the road.
                try
                {
                    assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Console.Error.WriteLine($"Error loading types from {assemblyPath}. Details: {ex}");
                    return;
                }

                assemblies.Add(assembly);
            }

            // Read the assemblies into an ApiData object.
            ApiData api = new ApiData(assemblies);

            // Write to the proper output format.
            switch (outputType)
            {
                case OutputType.Html:
                    api.WriteHtml(outputFileName);
                    break;
                case OutputType.Xml:
                    api.WriteXml(outputFileName);
                    break;
            }
        }

        private static void CreateDiff(string[] args)
        {
            string baselineFile1 = args[0];
            string baselineFile2 = args[1];
            string outputFile = args[2];

            // Validate args.
            if (File.Exists(baselineFile1) == false)
            {
                Console.Error.WriteLine("File not found: {0}", baselineFile1);
                return;
            }
            if (File.Exists(baselineFile2) == false)
            {
                Console.Error.WriteLine("File not found: {0}", baselineFile2);
                return;
            }

            DiffHelper.CreateDiff(baselineFile1, baselineFile2, outputFile);
        }

        #endregion Methods - Private
    }
}

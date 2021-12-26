using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Robzilla888.ApiReviewHelper.ObjectModel
{
    internal static class HtmlBaselineOutputHelper
    {
        public static void WriteHtml(this ApiData api, string outputFileName)
        {
            // Create the output dir if it doesn't exist.
            string outputDir = Path.GetDirectoryName(outputFileName);
            if (outputDir.Length > 0 && Directory.Exists(outputDir) == false)
            {
                Directory.CreateDirectory(outputDir);
            }

            using (StreamWriter w = File.CreateText(outputFileName))
            {
                w.WriteLine("<html>");
                w.WriteLine(@"
    <head>
        <style rel=""stylesheet"">
            .typesig
            { 
              font-family: Monospace; 
              font-size: 1.1em;
              font-weight: bold;
              //text-decoration: underline;
            }

            ul { margin-top: 0; }

            ul.membersig li
            {
              font-family: Monospace; 
              font-size: 1em;
            }

            a
            {
              text-decoration: none;
              color: black;
            }
            a.toggle
            {
              font-family: Monospace;
            }
            a.childToggle
            {
              font-size: 0.8em;
            }
        </style>
        <script type='text/javascript'>
            function toggleCollapse(id) {
                var content = document.getElementById('c_' + id);

                if (content.style.display == 'none') { expandElement(id); }
                else { collapseElement(id); }
            }

            function collapseElement(id) 
            {
                var header = document.getElementById('h_' + id);
                var content = document.getElementById('c_' + id);
                var links = document.getElementById('l_' + id);

                // Currently shown - hide it.
                header.style.display = 'block';
                content.style.display = 'none';
                if (links)
                {
                  links.style.display = 'none';
                }
            }

            function expandElement(id) 
            {
                var header = document.getElementById('h_' + id);
                var content = document.getElementById('c_' + id);
                var links = document.getElementById('l_' + id);

                header.style.display = 'inline'; 
                content.style.display = 'block';
                if (links)
                {
                  links.style.display = 'inline-block';
                }
            }

            function collapseChildren(id, isCollapse) {
              var container = document.getElementById('c_' + id);

              for (var childIdx in container.children)
              {
                var child = container.children[childIdx];

                // Get the ID of the child by stripping off the 'c_'
                var childId = child.id;

                if (childId == undefined || childId.startsWith('c_') == false) { continue; }
 
                childId = childId.replace(/^c_/, '');

                if (isCollapse) {   
                  collapseElement(childId);
                  collapseChildren(childId, true);
                }
                else {
                  expandElement(childId);
                  collapseChildren(childId, false);
                }
              }
            }

            function init(){
                // Chrome doesn't have 'startsWith' right now, so add it.
                if (typeof String.prototype.startsWith != 'function') {
                  String.prototype.startsWith = function (str){
                    return this.slice(0, str.length) == str;
                  };
                }
            }

        </script>
    </head>
    <body onload='init();'>");
                w.WriteLine("<h1>API Baseline</h1>");
                w.WriteLine("<h3>Date: {0}</h3>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // Assembly summary.
                w.WriteLine("<h2 style='margin-bottom:0;'>Assemblies:</h2>");
                w.WriteLine("<ul>");
                foreach (AssemblyData ad in api.Assemblies)
                {
                    w.WriteLine("<li>{0}</li>", HttpUtility.HtmlEncode(ad.FullName));
                }
                w.WriteLine("</ul>");

                w.WriteLine("<hr/>");

                // Assembly detail.
                foreach (AssemblyData ad in api.Assemblies)
                {
                    ad.WriteHtml(w);
                }

                w.WriteLine("</body></html>");
            }
        }

        private static void WriteHtml(this AssemblyData assemblyData, StreamWriter w)
        {
            // Assembly title.

            string id = GenerateId(assemblyData.Name);
            int numTypes = assemblyData.Namespaces.SelectMany(ns => ns.Types).Count();
            string toolTip = String.Format("Assembly {0} ({1} namespaces, {2} types)", assemblyData.Name, assemblyData.Namespaces.Count, numTypes);
            WriteCollapsibleHeader(w, id, assemblyData.Name, toolTip,  "<h2", "</h2>");

            // Namespaces.
            w.WriteLine("<div id=\"c_{0}\" style=\"margin-left:25px;\">", id);
            foreach (NamespaceData nd in assemblyData.Namespaces)
            {
                nd.WriteHtml(w);
            }
            w.WriteLine("</div>");
        }

        private static void WriteHtml(this NamespaceData nsData, StreamWriter w)
        {
            // Namespace title.

            string id = GenerateId(nsData.Name);
            string toolTip = String.Format("Namespace {0} ({1} types)", nsData.Name, nsData.Types.Count);
            WriteCollapsibleHeader(w, id, nsData.Name, toolTip, "<h3", "</h3>");

            // Types.
            w.WriteLine("<div id=\"c_{0}\" style=\"margin-left:25px;\">", id);
            foreach (TypeData td in nsData.Types)
            {
                td.WriteHtml(w);
            }
            w.WriteLine("</div>");
        }

        private static void WriteHtml(this TypeData typeData, StreamWriter w)
        {
            string id = GenerateId(typeData.Name);
            string toolTip = String.Format("Type {0} ({1} members)", typeData.Name, typeData.Members.Count);
            WriteCollapsibleHeader(w, id, typeData.Signature, toolTip, "<div class='typesig'", "</div>", allowCollapseChildren: false);

            // Members.
            w.WriteLine("<ul class='membersig' id=\"c_{0}\">", id);

            // If type is not an enum, order the members by signature.
            IEnumerable<MemberData> members = typeData.Members;
            if (typeData.TypeVariant != TypeVariant.Enum)
            {
                members = members.OrderBy(m => m.Signature);
            }

            foreach (MemberData md in members)
            {
                md.WriteHtml(w);
            }
            w.WriteLine("</ul>");
        }

        private static void WriteHtml(this MemberData memberData, StreamWriter w)
        {
            w.WriteLine("<li>{0}</li>", HttpUtility.HtmlEncode(memberData.Signature));
        }

        private static void WriteCollapsibleHeader(
            StreamWriter w,
            string id,
            string headerText, 
            string hoverText,
            string headerElementPrefix, 
            string headerElementEndTag,
            bool allowCollapseChildren = true)
        {
            w.WriteLine("<div id='h_{0}'>", id);
            w.WriteLine("  <a class='toggle' href=\"javascript:toggleCollapse('{0}');\" title=\"{1}\">", id, hoverText);
            w.WriteLine("  {0} style='display: inline-block; vertical-align: middle; margin: 0;'>{1}{2}</a>",
                        headerElementPrefix,
                        HttpUtility.HtmlEncode(headerText),
                        headerElementEndTag);
            if (allowCollapseChildren)
            {
                w.WriteLine("  <div id='l_{0}' style='display: inline-block;'>", id);
                w.WriteLine("    <a class='childToggle' href='javascript:collapseChildren(\"{0}\", true);'>[Collapse Children]</a>", id);
                w.WriteLine("    <a class='childToggle' href='javascript:collapseChildren(\"{0}\", false);'>[Expand Children]</a>", id);
                w.WriteLine("  </div>");
            }
            w.WriteLine("</div>");
        }

        private static string GenerateId(string prefix)
        {
            return prefix + "_" + Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

    }
}

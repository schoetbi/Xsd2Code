using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Xsd2Code.SetupLibrary
{
    /// <summary>
    /// Custom action for add-in deployment.
    /// </summary>
    [RunInstaller(true)]
    public partial class Xsd2CodeInstaller : Installer
    {
        /// <summary>
        /// Namespace used in the .addin configuration file.
        /// </summary>         
        private const string ExtNameSpace = "http://schemas.microsoft.com/AutomationExtensibility";

        /// <summary>
        /// Addin control file name  
        /// </summary>
        private const string addinControlFileName = "Xsd2Code.Addin.Addin";

        /// <summary>
        /// Addin assembly file name  
        /// </summary>
        private const string addinAssemblyFileName = "Xsd2Code.Addin.dll";

        /// <summary>
        /// Saved state key  
        /// </summary>
        private const string savedStateKey = "AddinPath";

        /// <summary>
        /// Constructor. Initializes components.
        /// </summary>
        public Xsd2CodeInstaller()
        {
            this.InitializeComponent();
        }


        /// <summary>
        /// Overrides Installer.Install,
        /// which will be executed during install process.
        /// </summary>
        /// <param name="savedState">The saved state.</param>
        public override void Install(IDictionary savedState)
        {
            base.Install(savedState);

            // Parameters required to pass in from installer

/* RU20090225: Not being used. Remove?
            string productName = this.Context.Parameters["ProductName"];
            string assemblyName = this.Context.Parameters["AssemblyName"];
*/

            // Setup .addin path and assembly path
            string addinTargetPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"Visual Studio 2008\Addins");

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                var targetFolder = new DirectoryInfo(addinTargetPath);
                if (!targetFolder.Exists) targetFolder.Create();

                string sourceFile = Path.Combine(assemblyPath, addinControlFileName);

                var addinXml = new XmlDocument();
                addinXml.Load(sourceFile);

                var nsmgr = new XmlNamespaceManager(addinXml.NameTable);
                nsmgr.AddNamespace("def", ExtNameSpace);

                // Update Addin/Assembly node
                SetNodeValue(addinXml, nsmgr,
                             "/def:Extensibility/def:Addin/def:Assembly",
                             Path.Combine(assemblyPath, addinAssemblyFileName));

                // Update ToolsOptionsPage/Assembly node
                SetNodeValue(addinXml, nsmgr,
                             "/def:Extensibility/def:ToolsOptionsPage/def:Category/def:SubCategory/def:Assembly",
                             Path.Combine(assemblyPath, addinAssemblyFileName));

                addinXml.Save(sourceFile);

                string targetFile = Path.Combine(addinTargetPath, addinControlFileName);
                File.Copy(sourceFile, targetFile, true);

                // Save AddinPath to be used in Uninstall or Rollback

                savedState.Add(savedStateKey, targetFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private static void SetNodeValue(XmlNode sourceNode, XmlNamespaceManager nsmgr, string xpath, string value)
        {
            var node = sourceNode.SelectSingleNode(xpath, nsmgr);
            if (node != null) node.InnerText = value;
        }

        /// <summary>
        /// Overrides Installer.Rollback, which will be executed during rollback process.
        /// </summary>
        /// <param name="savedState">The saved state.</param>
        public override void Rollback(IDictionary savedState)
        {
            ////Debugger.Break();

            base.Rollback(savedState);

            try
            {
                var fileName = (string)savedState[savedStateKey];
                if (File.Exists(fileName)) File.Delete(fileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Overrides Installer.Uninstall, which will be executed during uninstall process.
        /// </summary>
        /// <param name="savedState">The saved state.</param>
        public override void Uninstall(IDictionary savedState)
        {
            ////Debugger.Break();

            base.Uninstall(savedState);

            try
            {
                var fileName = (string)savedState[savedStateKey];
                if (File.Exists(fileName)) File.Delete(fileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
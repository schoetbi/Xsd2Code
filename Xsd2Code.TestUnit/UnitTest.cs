using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xsd2Code.Library;
using Xsd2Code.TestUnit.Properties;

namespace Xsd2Code.TestUnit
{
    /// <summary>
    /// Xsd2Code unit tests
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Modified 2009-02-25 by Ruslan Urban 
    ///     Performed code review
    ///     Changed output folder to the TestResults folder to preserve files in the testing history
    ///     TODO: Add tests that compile generated code
    /// 
    /// </remarks>
    [TestClass]
    public class UnitTest
    {
        /// <summary>
        /// Output folder: TestResults folder relative to the solution root folder
        /// </summary>
        private static string OutputFolder
        {
            get { return @"c:\temp\"; } // Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\"; }
        }

        /// <summary>
        /// Code generation namespace  
        /// </summary>
        private const string CodeGenerationNamespace = "Xsd2Code.TestUnit";

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion

        /// <summary>
        /// Circulars this instance.
        /// </summary>
        [TestMethod]
        public void Circular()
        {
            // Copy resource file to the run-time directory
            string inputFilePath = GetInputFilePath("Circular.xsd", Resources.Circular);

            var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));
            var result = xsdGen.Generate();

            Assert.IsTrue(result.Success, result.Messages.ToString());
        }

        /// <summary>
        /// Stacks the over flow.
        /// </summary>
        [TestMethod]
        public void StackOverFlow()
        {
            // Copy resource file to the run-time directory
            string inputFilePath = GetInputFilePath("StackOverFlow.xsd", Resources.StackOverFlow);

            var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));
            var result = xsdGen.Generate();

            Assert.IsTrue(result.Success, result.Messages.ToString());
        }


        /// <summary>
        /// DVDs this instance.
        /// </summary>
        [TestMethod]
        public void Dvd()
        {
            // Copy resource file to the run-time directory
            GetInputFilePath("Actor.xsd", Resources.Actor);

            // Copy resource file to the run-time directory
            string inputFilePath = GetInputFilePath("Dvd.xsd", Resources.dvd);

            var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));
            var result = xsdGen.Generate();

            Assert.IsTrue(result.Success, result.Messages.ToString());
        }


        /// <summary>
        /// Genders this instance.
        /// </summary>
        [TestMethod]
        public void Gender()
        {
            // Get the code namespace for the schema.
            string inputFilePath = GetInputFilePath("Gender.xsd", Resources.Gender);

            var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));

            var result = xsdGen.Generate();
            if (!result.Success)
                Assert.Fail(result.Messages.ToString());

            var genderRoot = new Root
                                 {
                                     GenderAttribute = ksgender.female,
                                     GenderAttributeSpecified = true,
                                     GenderElement = ksgender.female,
                                     GenderIntAttribute = "toto"
                                 };
            Exception exp;
            genderRoot.SaveToFile(OutputFolder + @"gender.xml", out exp);
        }

        /// <summary>
        /// Alows the debug.
        /// </summary>
        [TestMethod]
        public void AlowDebug()
        {
            // Copy resource file to the run-time directory
            GetInputFilePath("Actor.xsd", Resources.Actor);
            string inputFilePath = GetInputFilePath("Dvd.xsd", Resources.dvd);

            var generatorParams = GetGeneratorParams(inputFilePath);
            generatorParams.DisableDebug = false;
            generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".DebugEnabled.cs");

            var xsdGen = new GeneratorFacade(generatorParams);
            var result = xsdGen.Generate();

            Assert.IsTrue(result.Success, result.Messages.ToString());
        }

        [TestMethod]
        public void Hierarchical()
        {
            // Copy resource file to the run-time directory
            string inputFilePath = GetInputFilePath("Hierarchical.xsd", Resources.Hierarchical);

            var xsdGen = new GeneratorFacade(GetGeneratorParams(inputFilePath));
            var result = xsdGen.Generate();

            Assert.IsTrue(result.Success, result.Messages.ToString());
        }

        [TestMethod]
        public void Serialize()
        {
            DvdCollection dvdCol = GetDvd();
            string dvdColStr1 = dvdCol.Serialize();

            DvdCollection dvdColFromXml;
            Exception exception;
            bool sucess = DvdCollection.Deserialize(dvdColStr1, out dvdColFromXml, out exception);
            if (sucess)
            {
                string dvdColStr2 = dvdColFromXml.Serialize();
                if (!dvdColStr1.Equals(dvdColStr2))
                    Assert.Fail("dvdColFromXml is not equal after Deserialize");
            }
            else
                Assert.Fail(exception.Message);
        }

        [TestMethod]
        public void Silverlight20_1()
        {
            // Get the code namespace for the schema.
            GetInputFilePath("Actor.xsd", Resources.Actor);
            string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

            var generatorParams = GetGeneratorParams(inputFilePath);
            generatorParams.TargetFramework = TargetFramework.Silverlight20;
            generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".Silverlight20_01.cs");

            var xsdGen = new GeneratorFacade(generatorParams);

            var result = xsdGen.Generate();
            if (!result.Success) Assert.Fail(result.Messages.ToString());

        }

        [TestMethod]
        public void XMLAttributes()
        {
            // Get the code namespace for the schema.
            GetInputFilePath("Actor.xsd", Resources.Actor);
            string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

            var generatorParams = GetGeneratorParams(inputFilePath);
            generatorParams.GenerateXMLAttributes = true;

            generatorParams.TargetFramework = TargetFramework.Net20;
            generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".xml.cs");

            var xsdGen = new GeneratorFacade(generatorParams);
            var result = xsdGen.Generate();

            if (!result.Success) Assert.Fail(result.Messages.ToString());
        }

        [TestMethod]
        public void AutomaticProperties()
        {
            // Get the code namespace for the schema.
            GetInputFilePath("Actor.xsd", Resources.Actor);
            string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

            var generatorParams = new GeneratorParams();
            generatorParams.InputFilePath = inputFilePath;
            GetGeneratorParams(inputFilePath);
            generatorParams.EnableSummaryComment = true;
            generatorParams.GenerateDataContracts = false;
            generatorParams.AutomaticProperties = true;

            generatorParams.TargetFramework = TargetFramework.Net30;
            generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".autoProp.cs");

            var xsdGen = new GeneratorFacade(generatorParams);
            var result = xsdGen.Generate();

            if (!result.Success) Assert.Fail(result.Messages.ToString());
        }
        [TestMethod]
        public void WcfAttributes()
        {
            // Get the code namespace for the schema.
            GetInputFilePath("Actor.xsd", Resources.Actor);
            string inputFilePath = GetInputFilePath("dvd.xsd", Resources.dvd);

            var generatorParams = GetGeneratorParams(inputFilePath);
            generatorParams.GenerateDataContracts = true;
            generatorParams.TargetFramework = TargetFramework.Net30;
            generatorParams.OutputFilePath = Path.ChangeExtension(generatorParams.InputFilePath, ".wcf.cs");

            var xsdGen = new GeneratorFacade(generatorParams);
            var result = xsdGen.Generate();

            if (!result.Success) Assert.Fail(result.Messages.ToString());
        }

        [TestMethod]
        public void Persistent()
        {
            DvdCollection dvdCol = GetDvd();
            Exception exception;
            if (!dvdCol.SaveToFile(OutputFolder + @"savedvd.xml", out exception))
                Assert.Fail(string.Format("Failed to save file. {0}", exception.Message));

            DvdCollection loadedDvdCollection;
            Exception e;
            if (!DvdCollection.LoadFromFile(OutputFolder + @"savedvd.xml", out loadedDvdCollection, out e))
                Assert.Fail(string.Format("Failed to load file. {0}", e.Message));

            string xmlBegin = dvdCol.Serialize();
            string xmlEnd = loadedDvdCollection.Serialize();

            if (!xmlBegin.Equals(xmlEnd))
                Assert.Fail(string.Format("xmlBegin and xmlEnd are not equal after LoadFromFile"));
        }

        [TestMethod]
        public void InvalidLoadFromFile()
        {
            DvdCollection loadedDvdCollection;
            Exception e;
            DvdCollection.LoadFromFile(OutputFolder + @"savedvd.error.xml", out loadedDvdCollection, out e);
        }

        private static DvdCollection GetDvd()
        {
            var dvdCol = new DvdCollection();
            var newdvd = new dvd {Title = "Matrix", Style = Styles.Action};
            newdvd.Actor.Add(new Actor {firstname = "Thomas", lastname = "Anderson"});
            dvdCol.Dvds.Add(newdvd);
            return dvdCol;
        }

        private static string GetInputFilePath(string resourceFileName, string fileContent)
        {
            using (var sw = new StreamWriter(OutputFolder + resourceFileName, false))
                sw.Write(fileContent);

            return OutputFolder + resourceFileName;
        }

        private static GeneratorParams GetGeneratorParams(string inputFilePath)
        {
            return new GeneratorParams
                       {
                           InputFilePath = inputFilePath,
                           NameSpace = CodeGenerationNamespace,
                           TargetFramework = TargetFramework.Net20,
                           CollectionObjectType = CollectionType.ObservableCollection,
                           DisableDebug = true,
                           EnableDataBinding = true,
                           GenerateDataContracts = true,
                           GenerateCloneMethod = true,
                           IncludeSerializeMethod = true,
                           HidePrivateFieldInIde = true
                       };
        }
    }
}
namespace Xsd2Code.Library.Extensions
{
    /// <summary>
    /// Implements code generation extension for .Net Framework 2.0
    /// </summary>
    [CodeExtension(TargetFramework.Net20)]
    public class Net20Extension : CodeExtension
    {
        protected override void CreateDataContractAttribute(System.CodeDom.CodeTypeDeclaration type, System.Xml.Schema.XmlSchema schema)
        {
            // No data contracts in the Net.20
        }
    }
}
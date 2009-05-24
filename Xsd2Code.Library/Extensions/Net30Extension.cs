namespace Xsd2Code.Library.Extensions
{
    using System.CodeDom;
    using System.Xml.Schema;
    using System.Collections.Generic;
    using Xsd2Code.Library.Helpers;
    using System.IO;
    using System.CodeDom.Compiler;
    using System.Linq;

    /// <summary>
    /// Implements code generation extension for .Net Framework 3.0
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Created 2009-03-16 by Ruslan Urban
    ///     based on GeneratorExtension.cs
    ///     Updates 2009-05-18 by Pascal Cabanel.
    ///     Include CreateDataMemberAttribute and CreateDataContractAttribute methods
    /// </remarks>

    [CodeExtension(TargetFramework.Net30)]
    public class Net30Extension : CodeExtension
    {
        protected List<CodeMemberProperty> autoPropertyList = new List<CodeMemberProperty>();
        protected List<CodeMemberField> fieldListToRemove = new List<CodeMemberField>();
        protected List<string> fieldWithAssignementInCtorList = new List<string>();

        protected override void ProcessClass(CodeNamespace code, XmlSchema schema, CodeTypeDeclaration type)
        {
            autoPropertyList.Clear();
            fieldListToRemove.Clear();
            fieldWithAssignementInCtorList.Clear();

            #region looks for properties that can not become automatic property
            CodeConstructor ctor = null;
            foreach (CodeTypeMember member in type.Members)
            {
                if (member is CodeConstructor)
                    ctor = member as CodeConstructor;
            }
            if (ctor != null)
            {
                foreach (var statement in ctor.Statements)
                {
                    var codeAssignStatement = statement as CodeAssignStatement;
                    if (codeAssignStatement != null)
                    {
                        var codeField = codeAssignStatement.Left as CodeFieldReferenceExpression;
                        if (codeField != null)
                        {
                            fieldWithAssignementInCtorList.Add(codeField.FieldName);
                        }
                    }
                }
            }
            #endregion

            base.ProcessClass(code, schema, type);

            #region generate automatic properties
            if (GeneratorContext.GeneratorParams.Language == GenerationLanguage.CSharp)
            {
                // If databinding is disable, use automatic property
                if (GeneratorContext.GeneratorParams.AutomaticProperties)
                {
                    foreach (var item in autoPropertyList)
                    {
                        CodeSnippetTypeMember cm = new CodeSnippetTypeMember();
                        bool transformToAutomaticproperty = true;

                        List<string> attributesString = new List<string>();
                        foreach (var attribute in item.CustomAttributes)
                        {
                            var attrib = attribute as CodeAttributeDeclaration;
                            if (attrib != null)
                            {
                                // Don't transform property with default value.
                                if (attrib.Name == "System.ComponentModel.DefaultValueAttribute")
                                {
                                    transformToAutomaticproperty = false;
                                }
                                else
                                {
                                    string attributesArgument = string.Empty;
                                    foreach (var arg in attrib.Arguments)
                                    {
                                        var argument = arg as CodeAttributeArgument;
                                        if (argument != null)
                                        {
                                            attributesArgument += OutputAttributeArgument(argument);
                                        }
                                    }
                                    attributesString.Add(string.Format("[{0}({1})]", attrib.Name, attributesArgument));
                                }
                            }
                        }

                        if (transformToAutomaticproperty)
                        {
                            foreach (var attribute in attributesString)
                            {
                                cm.Text += "    " + attribute + "\n";
                            }
                            string text = string.Format("    public {0} {1} ", item.Type.BaseType.ToString(), item.Name);
                            cm.Text += string.Concat(text, "{get; set;}\n");
                            cm.Comments.AddRange(item.Comments);
                            type.Members.Add(cm);
                            type.Members.Remove(item);
                        }
                    }

                    // Now remove all private fileds
                    foreach (var item in fieldListToRemove)
                    {
                        type.Members.Remove(item);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Outputs the attribute argument.
        /// </summary>
        /// <param name="arg">The arg.</param>
        /// <returns></returns>
        private string OutputAttributeArgument(CodeAttributeArgument arg)
        {
            StringWriter strWriter = new StringWriter();
            var provider = CodeDomProviderFactory.GetProvider(GeneratorContext.GeneratorParams.Language);

            if ((arg.Name != null) && (arg.Name.Length > 0))
            {
                strWriter.Write(arg.Name);
                strWriter.Write("=");
            }
            provider.GenerateCodeFromExpression(arg.Value, strWriter, new CodeGeneratorOptions());
            StringReader strrdr = new StringReader(strWriter.ToString());
            return strrdr.ReadToEnd();
        }


        /// <summary>
        /// Create data contract attribute
        /// </summary>
        /// <param name="type">Code type declaration</param>
        /// <param name="schema">XML schema</param>
        protected override void CreateDataContractAttribute(CodeTypeDeclaration type, XmlSchema schema)
        {
            base.CreateDataContractAttribute(type, schema);

            if (GeneratorContext.GeneratorParams.GenerateDataContracts)
            {
                var attributeType = new CodeTypeReference("System.Runtime.Serialization.DataContractAttribute");
                var codeAttributeArgument = new List<CodeAttributeArgument>();

                var typeName = string.Concat('"', type.Name, '"');
                codeAttributeArgument.Add(new CodeAttributeArgument("Name", new CodeSnippetExpression(typeName)));

                if (!string.IsNullOrEmpty(schema.TargetNamespace))
                {
                    var targetNamespace = string.Concat('\"', schema.TargetNamespace, '\"');
                    codeAttributeArgument.Add(new CodeAttributeArgument("Namespace", new CodeSnippetExpression(targetNamespace)));
                }

                type.CustomAttributes.Add(new CodeAttributeDeclaration(attributeType, codeAttributeArgument.ToArray()));
            }
        }

        /// <summary>
        /// Creates the data member attribute.
        /// </summary>
        /// <param name="prop">The prop.</param>
        protected override void CreateDataMemberAttribute(System.CodeDom.CodeMemberProperty prop)
        {
            base.CreateDataMemberAttribute(prop);

            if (GeneratorContext.GeneratorParams.GenerateDataContracts)
            {
                var attrib = new CodeTypeReference("System.Runtime.Serialization.DataMemberAttribute");
                prop.CustomAttributes.Add(new CodeAttributeDeclaration(attrib));
            }
        }

        /// <summary>
        /// Import namespaces
        /// </summary>
        /// <param name="code">Code namespace</param>
        protected override void ImportNamespaces(CodeNamespace code)
        {
            base.ImportNamespaces(code);

            if (GeneratorContext.GeneratorParams.GenerateDataContracts)
                code.Imports.Add(new CodeNamespaceImport("System.Runtime.Serialization"));
        }

        /// <summary>
        /// Property process
        /// </summary>
        /// <param name="type">Represents a type declaration for a class, structure, interface, or enumeration</param>
        /// <param name="member">Type members include fields, methods, properties, constructors and nested types</param>
        /// <param name="xmlElement">Represent the root element in schema</param>
        /// <param name="schema">XML Schema</param>
        protected override void ProcessProperty(CodeTypeDeclaration type, CodeTypeMember member, XmlSchemaElement xmlElement, XmlSchema schema)
        {
            // Get now if property is array before base.ProcessProperty call.
            var prop = (CodeMemberProperty)member;
            bool isArray = prop.Type.ArrayElementType != null;

            base.ProcessProperty(type, member, xmlElement, schema);

            // Generate automatic properties.
            if (GeneratorContext.GeneratorParams.Language == GenerationLanguage.CSharp)
            {
                if (GeneratorContext.GeneratorParams.AutomaticProperties)
                {
                    if (!isArray)
                    {
                        autoPropertyList.Add(member as CodeMemberProperty);
                    }
                }
            }
        }

        /// <summary>
        /// Field process.
        /// </summary>
        /// <param name="member">CodeTypeMember member</param>
        /// <param name="ctor">CodeMemberMethod constructor</param>
        /// <param name="ns">CodeNamespace XSD</param>
        /// <param name="addedToConstructor">Indicates if create a new constructor</param>
        protected override void ProcessField(CodeTypeMember member, CodeMemberMethod ctor, CodeNamespace ns, ref bool addedToConstructor)
        {
            // Get now if filed is array before base.ProcessProperty call.
            var field = (CodeMemberField)member;
            bool isArray = field.Type.ArrayElementType != null;

            base.ProcessField(member, ctor, ns, ref addedToConstructor);

            // Generate automatic properties.
            if (GeneratorContext.GeneratorParams.Language == GenerationLanguage.CSharp)
            {
                if (GeneratorContext.GeneratorParams.AutomaticProperties)
                {
                    if (!isArray)
                    {
                        // If this field is not assigned in ctor, add it in remove list.
                        // with automatic property, don't need to keep private field.
                        if (fieldWithAssignementInCtorList.FindIndex(p => p == field.Name) == -1)
                        {
                            fieldListToRemove.Add(field as CodeMemberField);
                        }
                    }
                }
            }
        }
    }
}
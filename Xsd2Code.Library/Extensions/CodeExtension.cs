using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xsd2Code.Library.Helpers;
using Xsd2Code.Library.Properties;

namespace Xsd2Code.Library.Extensions
{
    /// <summary>
    /// Base class for code generation extension
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Created 2009-03-16 by Ruslan Urban
    ///     based on GeneratorExtension.cs
    ///     Updated 2009-05-18 move wcf CodeDom generation into Net35Extention.cs by Pascal Cabanel
    ///     Updated 2009-05-18 Remove .Net 2.0 XML attributes by Pascal Cabanel
    /// </remarks>
    public abstract class CodeExtension : ICodeExtension
    {
        /// <summary>
        /// Sorted list for custom collection
        /// </summary>
        protected static readonly SortedList<string, string> collectionTypesField = new SortedList<string, string>();

        #region ICodeExtension Members

        /// <summary>
        /// Process method for cs or vb CodeDom generation
        /// </summary>
        /// <param name="code">CodeNamespace generated</param>
        /// <param name="schema">XmlSchema to generate</param>
        public virtual void Process(CodeNamespace code, XmlSchema schema)
        {
            #region Namespace imports

            this.ImportNamespaces(code);

            #endregion

            collectionTypesField.Clear();


            var types = new CodeTypeDeclaration[code.Types.Count];
            code.Types.CopyTo(types, 0);

            foreach (var type in types)
            {          
                
                // Remove default remarks attribute
                type.Comments.Clear();

                // Remove default .Net 2.0 XML attributes if disabled.
                if (!GeneratorContext.GeneratorParams.GenerateXMLAttributes)
                {
                    this.RemoveDefaultXMLAttributes(type.CustomAttributes);
                }

                if (!type.IsClass && !type.IsStruct) continue;

                ProcessClass(code, schema, type);
            }

            #region Custom Collection

            foreach (string collName in collectionTypesField.Keys)
                this.CreateCollectionClass(code, collName);

            #endregion
        }

        /// <summary>
        /// Processes the class.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="type">The type.</param>
        protected virtual void ProcessClass(CodeNamespace code, XmlSchema schema, CodeTypeDeclaration type)
        {
            bool addedToConstructor = false;
            bool newCTor = false;

            var ctor = this.GetConstructor(type, ref newCTor);

            // Generate WCF DataContract
            this.CreateDataContractAttribute(type, schema);

            #region Find item in XmlSchema for generate class documentation.

            XmlSchemaElement currentElement = null;
            if (GeneratorContext.GeneratorParams.EnableSummaryComment)
                currentElement = this.CreateSummaryCommentFromSchema(type, schema, currentElement);

            #endregion

            foreach (CodeTypeMember member in type.Members)
            {
                #region Process Fields

                // Remove default remarks attribute
                member.Comments.Clear();

                // Remove default .Net 2.0 XML attributes if disabled.
                if (!GeneratorContext.GeneratorParams.GenerateXMLAttributes)
                {
                    this.RemoveDefaultXMLAttributes(member.CustomAttributes);
                }

                var codeMemberField = member as CodeMemberField;
                if (codeMemberField != null)
                    this.ProcessField(codeMemberField, ctor, code, ref addedToConstructor);

                #endregion

                #region Process properties

                CodeMemberProperty codeMemberProperty = member as CodeMemberProperty;
                if (codeMemberProperty != null)
                    this.ProcessProperty(type, codeMemberProperty, currentElement, schema);

                #endregion
            }

             // Add new ctor if required
            if (addedToConstructor && newCTor)
                type.Members.Add(ctor);

            if (GeneratorContext.GeneratorParams.EnableDataBinding)
                this.CreateDataBinding(type);

            if (GeneratorContext.GeneratorParams.IncludeSerializeMethod)
                this.CreateSerializeMethods(type);

            #region Clone

            if (GeneratorContext.GeneratorParams.GenerateCloneMethod)
                this.CreateCloneMethod(type);

            #endregion Clone
        }

        /// <summary>
        /// Create data binding
        /// </summary>
        /// <param name="type">Code type declaration</param>
        protected virtual void CreateDataBinding(CodeTypeDeclaration type)
        {
            #region add public PropertyChangedEventHandler event

            // -------------------------------------------------------------------------------
            // public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
            // -------------------------------------------------------------------------------
            var propertyChangedEvent =
                new CodeMemberEvent
                    {
                        // ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
                        Attributes = (MemberAttributes.Final | MemberAttributes.Public),
                        // ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
                        Name = "PropertyChanged",
                        Type =
                            new CodeTypeReference(typeof(PropertyChangedEventHandler))
                    };

            type.Members.Add(propertyChangedEvent);

            #endregion

            #region Add OnPropertyChanged method.

            // -----------------------------------------------------------
            //  protected virtual  void OnPropertyChanged(string info) {
            //      PropertyChangedEventHandler handler = PropertyChanged;
            //      if (handler != null) {
            //          handler(this, new PropertyChangedEventArgs(info));
            //      }
            //  }
            // -----------------------------------------------------------
            var propertyChangedMethod = new CodeMemberMethod { Name = "OnPropertyChanged" };
            propertyChangedMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "info"));


            switch (GeneratorContext.GeneratorParams.Language)
            {
                case GenerationLanguage.VisualBasic:

                    propertyChangedMethod.Statements.Add(
                        new CodeExpressionStatement(
                            new CodeSnippetExpression(
                                "RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(info))")));

                    break;

                case GenerationLanguage.CSharp:
                case GenerationLanguage.VisualCpp:

                    propertyChangedMethod.Statements.Add(
                        new CodeExpressionStatement(
                            new CodeSnippetExpression("PropertyChangedEventHandler handler = PropertyChanged")));

                    var codeExpressionStatement =
                        new CodeExpressionStatement(
                            new CodeSnippetExpression("handler(this, new PropertyChangedEventArgs(info))"));

                    CodeStatement[] statements = new[] { codeExpressionStatement };

                    propertyChangedMethod.Statements.Add(
                        new CodeConditionStatement(new CodeSnippetExpression("handler != null"), statements));

                    break;
            }

            type.Members.Add(propertyChangedMethod);

            #endregion
        }

        protected virtual XmlSchemaElement CreateSummaryCommentFromSchema(CodeTypeDeclaration type, XmlSchema schema,
                                                                          XmlSchemaElement currentElement)
        {
            var xmlSchemaElement = this.SearchElementInSchema(type, schema);
            if (xmlSchemaElement != null)
            {
                currentElement = xmlSchemaElement;
                if (xmlSchemaElement.Annotation != null)
                {
                    foreach (var item in xmlSchemaElement.Annotation.Items)
                    {
                        var xmlDoc = item as XmlSchemaDocumentation;
                        if (xmlDoc == null) continue;
                        this.CreateCommentStatment(type.Comments, xmlDoc);
                    }
                }
            }
            return currentElement;
        }

        protected virtual void CreateCollectionClass(CodeNamespace code, string collName)
        {
            var ctd = new CodeTypeDeclaration(collName) { IsClass = true };
            ctd.BaseTypes.Add(string.Format("{0}<{1}>",
                                            GeneratorContext.GeneratorParams.CollectionBase,
                                            collectionTypesField[collName]));
            ctd.IsPartial = true;

            bool newCTor = false;
            var ctor = this.GetConstructor(ctd, ref newCTor);

            ctd.Members.Add(ctor);
            code.Types.Add(ctd);
        }

        protected virtual void CreateCloneMethod(CodeTypeDeclaration type)
        {
            type.Members.Add(this.GetCloneMethod(type));
        }

        #region GetCloneMethod

        /// <summary>
        /// Generate defenition of the Clone() method
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual CodeTypeMember GetCloneMethod(CodeTypeDeclaration type)
        {
            // ----------------------------------------------------------------------
            // /// <summary>
            // /// Create clone of this TClass object
            // /// </summary>
            // public TClass Clone()
            // {
            //    return ((TClass)this.MemberwiseClone());
            // }
            // ----------------------------------------------------------------------

            var cloneMethod = new CodeMemberMethod
                                  {
                                      Attributes = MemberAttributes.Public,
                                      Name = "Clone",
                                      ReturnType = new CodeTypeReference(type.Name)
                                  };

            CodeDomHelper.CreateSummaryComment(cloneMethod.Comments,
                                               string.Format("Create a clone of this {0} object", type.Name));
            var memberwiseCloneMethod = new CodeMethodInvokeExpression(new CodeThisReferenceExpression(),
                                                                       "MemberwiseClone");
            var statement = new CodeMethodReturnStatement(new CodeCastExpression(type.Name, memberwiseCloneMethod));
            cloneMethod.Statements.Add(statement);

            /*
                        TODO: Remove commented out fragment
                        RU20090220: Commented out code fragment
             
                        cloneMethod.Statements.Add(new CodeVariableDeclarationStatement(
                                                       new CodeTypeReference(type.Name), "cloneObject",
                                                       new CodeObjectCreateExpression(new CodeTypeReference(type.Name))));

                        foreach (CodeTypeMember member in type.Members)
                        {
                            #region Process Fields

                            if (member is CodeMemberProperty)
                            {
                                CodeMemberProperty cmp = member as CodeMemberProperty;
                                CodeAssignStatement cdtAssignStmt =
                                    new CodeAssignStatement(
                                        new CodeSnippetExpression(string.Format("cloneObject.{0}", member.Name)),
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), member.Name));
                                cloneMethod.Statements.Add(cdtAssignStmt);
                            }

                            #endregion
                        }
                        cloneMethod.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("cloneObject")));
             */

            return cloneMethod;
        }

        #endregion

        protected virtual void CreateSerializeMethods(CodeTypeDeclaration type)
        {
            type.Members.Add(this.CreateSerializeMethod(type));
            type.Members.Add(this.CreateDeserializeMethod(type));
            type.Members.Add(this.CreateSaveToFileMethod(type));
            type.Members.Add(this.CreateLoadFromFileMethod(type));
        }

        /// <summary>
        /// Gets the serialize CodeDOM method.
        /// </summary>
        /// <param name="type">The type object to serilize.</param>
        /// <returns>return the CodeDOM serialize method</returns>
        protected virtual CodeMemberMethod CreateSerializeMethod(CodeTypeDeclaration type)
        {
            var serializeMethod = new CodeMemberMethod
                                      {
                                          Attributes = MemberAttributes.Public,
                                          Name = GeneratorContext.GeneratorParams.SerializeMethodName
                                      };

            // --------------------------------------------------------------------------
            // System.Xml.Serialization.XmlSerializer xmlSerializer = 
            //      new System.Xml.Serialization.XmlSerializer(this.GetType());
            // System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            // System.IO.StreamReader streamReader = new System.IO.StreamReader();
            // --------------------------------------------------------------------------
            var getTypeMethod = new CodeMethodInvokeExpression(
                new CodeThisReferenceExpression(), "GetType");

            serializeMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(XmlSerializer)), "xmlSerializer",
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(XmlSerializer)), getTypeMethod)));

            serializeMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(MemoryStream)), "memoryStream",
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof(MemoryStream)))));

            // --------------------------------------------------------------------------
            // xmlSerializer = new System.Xml.Serialization.XmlSerializer(this.GetType());
            // xmlSerializer.Serialize(memoryStream, this);
            // --------------------------------------------------------------------------
            serializeMethod.Statements.Add(
                CodeDomHelper.GetInvokeMethod("xmlSerializer", "Serialize",
                                              new CodeExpression[]
                                                  {
                                                      new CodeTypeReferenceExpression("memoryStream"),
                                                      new CodeThisReferenceExpression()
                                                  }));

            // ---------------------------------------------------------------------------
            // memoryStream.Seek(0, SeekOrigin.Begin);
            // System.IO.StreamReader streamReader = new System.IO.StreamReader(memoryStream);
            // ---------------------------------------------------------------------------
            serializeMethod.Statements.Add(
                CodeDomHelper.GetInvokeMethod("memoryStream",
                                              "Seek",
                                              new CodeExpression[]
                                                  {
                                                      new CodeTypeReferenceExpression("0"),
                                                      new CodeTypeReferenceExpression("System.IO.SeekOrigin.Begin")
                                                  }));

            serializeMethod.Statements.Add(
                CodeDomHelper.CreateObject(typeof(StreamReader), "streamReader", new[] { "memoryStream" }));


            var readToEnd = CodeDomHelper.GetInvokeMethod("streamReader", "ReadToEnd");
            serializeMethod.Statements.Add(new CodeMethodReturnStatement(readToEnd));
            serializeMethod.ReturnType = new CodeTypeReference(typeof(string));

            // --------
            // Comments
            // --------
            serializeMethod.Comments.AddRange(
                CodeDomHelper.GetSummaryComment(string.Format("Serializes current {0} object into an XML document",
                                                              type.Name)));

            serializeMethod.Comments.Add(CodeDomHelper.GetReturnComment("string XML value"));
            return serializeMethod;
        }

        /// <summary>
        /// Get Deserialize method
        /// </summary>
        /// <param name="type">represent a type declaration of class</param>
        /// <returns>Deserialize CodeMemberMethod</returns>
        protected virtual CodeMemberMethod CreateDeserializeMethod(CodeTypeDeclaration type)
        {
            var deserializeMethod = new CodeMemberMethod
                                        {
                                            // ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
                                            Attributes = (MemberAttributes.Public | MemberAttributes.Static),
                                            // ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
                                            Name = GeneratorContext.GeneratorParams.DeserializeMethodName
                                        };

            deserializeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "xml"));

            var param = new CodeParameterDeclarationExpression(type.Name, "obj") { Direction = FieldDirection.Out };
            deserializeMethod.Parameters.Add(param);

            param = new CodeParameterDeclarationExpression(typeof(Exception), "exception") { Direction = FieldDirection.Out };

            deserializeMethod.Parameters.Add(param);

            deserializeMethod.ReturnType = new CodeTypeReference(typeof(bool));

            deserializeMethod.Statements.Add(
                new CodeAssignStatement(
                    new CodeSnippetExpression("exception"),
                    new CodePrimitiveExpression(null)));

            deserializeMethod.Statements.Add(
                new CodeAssignStatement(
                    new CodeSnippetExpression("obj"),
                    new CodePrimitiveExpression(null)));

            // ---------------------
            // try {...} catch {...}
            // ---------------------
            var tryStatmanentsCol = new CodeStatementCollection();

            // --------------------------------------------------------------------
            // StringReader stringReader = new StringReader(xml);
            // XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
            // XmlSerializer xmlSerializer = new XmlSerializer(typeof(&ClassName));
            // --------------------------------------------------------------------
            var typeRef = new CodeTypeReference(type.Name);
            var typeofValue = new CodeTypeOfExpression(typeRef);

            // TODO: Not being used. Remove?
            var getObjTypeMethod = new CodeMethodInvokeExpression(
                null, "typeof", new CodeSnippetExpression(type.Name));

            tryStatmanentsCol.Add(CodeDomHelper.CreateObject(typeof(StringReader), "stringReader", new[] { "xml" }));

            var serializerParameter = "xmlTextReader";
            switch (GeneratorContext.GeneratorParams.TargetFramework)
            {
                case TargetFramework.Silverlight20:
                    serializerParameter = "stringReader";
                    break;
                default:
                    tryStatmanentsCol.Add(CodeDomHelper.CreateObject(typeof(XmlTextReader), "xmlTextReader",
                                                                     new[] { "stringReader" }));
                    break;
            }


            tryStatmanentsCol.Add(CodeDomHelper.CreateObject(typeof(XmlSerializer), "xmlSerializer",
                                                             new CodeExpression[] { typeofValue }));

            // ----------------------------------------------------------------------
            // if (xmlSerializer.CanDeserialize(xmlTextReader))
            // { ... } else { ... }
            // ----------------------------------------------------------------------
            // TODO: Not being used. Remove?
            var canDeserialize = CodeDomHelper.GetInvokeMethod("xmlSerializer", "CanDeserialize",
                                                               new CodeExpression[] { new CodeSnippetExpression("xmlTextReader") });

            // ----------------------------------------------------------
            // obj = (ClassName)xmlSerializer.Deserialize(xmlTextReader);
            // return true;
            // ----------------------------------------------------------
            var deserialize = CodeDomHelper.GetInvokeMethod("xmlSerializer", "Deserialize",
                                                            new CodeExpression[]
                                                                {
                                                                    new CodeSnippetExpression(serializerParameter)
                                                                });

            var castExpr = new CodeCastExpression(type.Name, deserialize);
            var cdtAssignStmt = new CodeAssignStatement(new CodeSnippetExpression("obj"), castExpr);

            tryStatmanentsCol.Add(cdtAssignStmt);
            tryStatmanentsCol.Add(CodeDomHelper.GetReturnTrue());

            // catch
            var catchClauses = CodeDomHelper.GetCatchClause();

            var tryStatments = new CodeStatement[tryStatmanentsCol.Count];
            tryStatmanentsCol.CopyTo(tryStatments, 0);

            var trycatch = new CodeTryCatchFinallyStatement(tryStatments, catchClauses);
            deserializeMethod.Statements.Add(trycatch);

            // --------
            // Comments
            // --------
            deserializeMethod.Comments.AddRange(
                CodeDomHelper.GetSummaryComment(string.Format("Deserializes workflow markup into an {0} object",
                                                              type.Name)));

            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("xml", "string workflow markup to deserialize"));
            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("obj",
                                                                         string.Format("Output {0} object", type.Name)));
            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("exception",
                                                                         "output Exception value if deserialize failed"));

            deserializeMethod.Comments.Add(
                CodeDomHelper.GetReturnComment("true if this XmlSerializer can deserialize the object; otherwise, false"));

            return deserializeMethod;
        }

        /// <summary>
        /// Gets the save to file code DOM method.
        /// </summary>
        /// <param name="type">CodeTypeDeclaration type.</param>
        /// <returns>return the save to file code DOM method statment </returns>
        protected virtual CodeMemberMethod CreateSaveToFileMethod(CodeTypeDeclaration type)
        {
            var saveToFileMethod = new CodeMemberMethod
                                       {
                                           Attributes = MemberAttributes.Public,
                                           Name = GeneratorContext.GeneratorParams.SaveToFileMethodName
                                       };

            saveToFileMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fileName"));

            var paramException = new CodeParameterDeclarationExpression(
                typeof(Exception), "exception") { Direction = FieldDirection.Out };

            saveToFileMethod.Parameters.Add(paramException);

            saveToFileMethod.ReturnType = new CodeTypeReference(typeof(bool));

            saveToFileMethod.Statements.Add(new CodeAssignStatement(new CodeSnippetExpression("exception"),
                                                                    new CodePrimitiveExpression(null)));

            // ---------------------
            // try {...} catch {...}
            // ---------------------
            var tryExpression = new CodeStatementCollection();

            // ------------------------------
            // string xmlString = Serialize();
            // -------------------------------
            var serializeMethodInvoke = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(null, GeneratorContext.GeneratorParams.SerializeMethodName));

            var xmlString = new CodeVariableDeclarationStatement(
                new CodeTypeReference(typeof(string)), "xmlString", serializeMethodInvoke);

            tryExpression.Add(xmlString);

            // --------------------------------------------------------------
            // System.IO.FileInfo xmlFile = new System.IO.FileInfo(fileName);
            // --------------------------------------------------------------
            tryExpression.Add(CodeDomHelper.CreateObject(typeof(FileInfo), "xmlFile", new[] { "fileName" }));

            // ----------------------------------
            // StreamWriter Tex = xmlFile.CreateText();
            // ----------------------------------
            CodeMethodInvokeExpression createTextMethodInvoke = CodeDomHelper.GetInvokeMethod("xmlFile", "CreateText");
            tryExpression.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof(StreamWriter)),
                    "streamWriter",
                    createTextMethodInvoke));

            // ----------------------------------
            // streamWriter.WriteLine(xmlString);
            // ----------------------------------
            CodeMethodInvokeExpression writeLineMethodInvoke =
                CodeDomHelper.GetInvokeMethod("streamWriter", "WriteLine",
                                              new CodeExpression[]
                                                  {
                                                      new CodeSnippetExpression("xmlString")
                                                  });
            tryExpression.Add(writeLineMethodInvoke);

            CodeMethodInvokeExpression closeMethodInvoke = CodeDomHelper.GetInvokeMethod("streamWriter", "Close");

            tryExpression.Add(closeMethodInvoke);
            tryExpression.Add(CodeDomHelper.GetReturnTrue());

            var tryStatment = new CodeStatement[tryExpression.Count];
            tryExpression.CopyTo(tryStatment, 0);

            // -----------
            // Catch {...}
            // -----------
            var catchstmts = new CodeStatement[2];
            catchstmts[0] = new CodeAssignStatement(new CodeSnippetExpression("exception"),
                                                    new CodeSnippetExpression("e"));

            catchstmts[1] = CodeDomHelper.GetReturnFalse();
            var codeCatchClause = new CodeCatchClause("e", new CodeTypeReference(typeof(Exception)), catchstmts);

            var codeCatchClauses = new[] { codeCatchClause };

            var trycatch = new CodeTryCatchFinallyStatement(tryStatment, codeCatchClauses);
            saveToFileMethod.Statements.Add(trycatch);

            saveToFileMethod.Comments.AddRange(
                CodeDomHelper.GetSummaryComment(string.Format("Serializes current {0} object into file", type.Name)));
            saveToFileMethod.Comments.Add(CodeDomHelper.GetParamComment("fileName", "full path of outupt xml file"));
            saveToFileMethod.Comments.Add(CodeDomHelper.GetParamComment("exception", "output Exception value if failed"));
            saveToFileMethod.Comments.Add(
                CodeDomHelper.GetReturnComment("true if can serialize and save into file; otherwise, false"));

            return saveToFileMethod;
        }


        /// <summary>
        /// Gets the load from file CodeDOM method.
        /// </summary>
        /// <param name="type">The type CodeTypeDeclaration.</param>
        /// <returns>return the codeDom LoadFromFile method</returns>
        protected virtual CodeMemberMethod CreateLoadFromFileMethod(CodeTypeDeclaration type)
        {
            #region Method declaration

            var loadFromFileMethod = new CodeMemberMethod
                                         {
                                             // ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
                                             Attributes = (MemberAttributes.Public | MemberAttributes.Static),
                                             // ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
                                             Name = GeneratorContext.GeneratorParams.LoadFromFileMethodName
                                         };

            loadFromFileMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "fileName"));

            var param = new CodeParameterDeclarationExpression(type.Name, "obj") { Direction = FieldDirection.Out };
            loadFromFileMethod.Parameters.Add(param);

            param = new CodeParameterDeclarationExpression(typeof(Exception), "exception") { Direction = FieldDirection.Out };

            loadFromFileMethod.Parameters.Add(param);
            loadFromFileMethod.ReturnType = new CodeTypeReference(typeof(bool));

            #endregion

            #region Variable assignement and new instance

            // -----------------
            // exception = null;
            // obj = null;
            // -----------------
            loadFromFileMethod.Statements.Add(
                new CodeAssignStatement(new CodeSnippetExpression("exception"), new CodePrimitiveExpression(null)));

            loadFromFileMethod.Statements.Add(
                new CodeAssignStatement(new CodeSnippetExpression("obj"), new CodePrimitiveExpression(null)));

            #endregion

            #region Try

            var tryStatmanentsCol = new CodeStatementCollection
                                        {
                                            CodeDomHelper.CreateObject(typeof (FileStream),
                                                                       "file",
                                                                       new[]
                                                                           {
                                                                               "fileName", "FileMode.Open",
                                                                               "FileAccess.Read"
                                                                           }),
                                            CodeDomHelper.CreateObject(typeof (StreamReader), "sr", new[] {"file"})
                                        };

            // ---------------------------------------------------------------------------
            // FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            // StreamReader sr = new StreamReader(file);
            // ---------------------------------------------------------------------------

            // ----------------------------------
            // string xmlString = sr.ReadToEnd();
            // ----------------------------------
            var readToEndInvoke = CodeDomHelper.GetInvokeMethod("sr", "ReadToEnd");

            var xmlString = new CodeVariableDeclarationStatement(
                new CodeTypeReference(typeof(string)), "xmlString", readToEndInvoke);

            tryStatmanentsCol.Add(xmlString);
            tryStatmanentsCol.Add(CodeDomHelper.GetInvokeMethod("sr", "Close"));
            tryStatmanentsCol.Add(CodeDomHelper.GetInvokeMethod("file", "Close"));

            // ------------------------------------------------------
            // return Deserialize(xmlString, out obj, out exception);
            // ------------------------------------------------------            
            var xmlStringParam = new CodeSnippetExpression("xmlString");
            var objParam = new CodeDirectionExpression(
                FieldDirection.Out, new CodeFieldReferenceExpression(null, "obj"));

            var expParam = new CodeDirectionExpression(
                FieldDirection.Out, new CodeFieldReferenceExpression(null, "exception"));

            var deserializeInvoke =
                new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(null, GeneratorContext.GeneratorParams.DeserializeMethodName),
                    new CodeExpression[] { xmlStringParam, objParam, expParam });

            var rstmts = new CodeMethodReturnStatement(deserializeInvoke);
            tryStatmanentsCol.Add(rstmts);

            #endregion

            #region catch

            var trycatch = new CodeTryCatchFinallyStatement(
                CodeDomHelper.CodeStmtColToArray(tryStatmanentsCol), CodeDomHelper.GetCatchClause());

            loadFromFileMethod.Statements.Add(trycatch);

            #endregion

            #region Comments

            loadFromFileMethod.Comments.AddRange(
                CodeDomHelper.GetSummaryComment(
                    string.Format("Deserializes workflow markup from file into an {0} object", type.Name)));

            loadFromFileMethod.Comments.Add(CodeDomHelper.GetParamComment("xml", "string workflow markup to deserialize"));
            loadFromFileMethod.Comments.Add(CodeDomHelper.GetParamComment("obj",
                                                                          string.Format("Output {0} object", type.Name)));
            loadFromFileMethod.Comments.Add(CodeDomHelper.GetParamComment("exception",
                                                                          "output Exception value if deserialize failed"));

            loadFromFileMethod.Comments.Add(
                CodeDomHelper.GetReturnComment("true if this XmlSerializer can deserialize the object; otherwise, false"));

            #endregion

            return loadFromFileMethod;
        }

        /// <summary>
        /// Import namespaces
        /// </summary>
        /// <param name="code">Code namespace</param>
        protected virtual void ImportNamespaces(CodeNamespace code)
        {
            code.Imports.Add(new CodeNamespaceImport("System"));
            code.Imports.Add(new CodeNamespaceImport("System.Diagnostics"));
            code.Imports.Add(new CodeNamespaceImport("System.Xml.Serialization"));
            code.Imports.Add(new CodeNamespaceImport("System.Collections"));
            code.Imports.Add(new CodeNamespaceImport("System.Xml.Schema"));
            code.Imports.Add(new CodeNamespaceImport("System.ComponentModel"));

            if (GeneratorContext.GeneratorParams.CustomUsings != null)
            {
                foreach (var item in GeneratorContext.GeneratorParams.CustomUsings)
                    code.Imports.Add(new CodeNamespaceImport(item.NameSpace));
            }
            if (GeneratorContext.GeneratorParams.IncludeSerializeMethod)
                code.Imports.Add(new CodeNamespaceImport("System.IO"));

            switch (GeneratorContext.GeneratorParams.CollectionObjectType)
            {
                case CollectionType.List:
                    code.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
                    break;
                case CollectionType.ObservableCollection:
                    code.Imports.Add(new CodeNamespaceImport("System.Collections.ObjectModel"));
                    break;
                default:
                    break;
            }

            code.Name = GeneratorContext.GeneratorParams.NameSpace;
        }

        /// <summary>
        /// Create data contract attribute
        /// </summary>
        /// <param name="type">Code type declaration</param>
        /// <param name="schema">XML schema</param>
        protected virtual void CreateDataContractAttribute(CodeTypeDeclaration type, XmlSchema schema)
        {
            // abstract
        }

        protected virtual void CreateDataMemberAttribute(CodeMemberProperty prop)
        {
            // abstract
        }

        #endregion

        /// <summary>
        /// Search XmlElement in schema.
        /// </summary>
        /// <param name="type">Element to find</param>
        /// <param name="schema">schema object</param>
        /// <returns>return found XmlSchemaElement or null value</returns>
        protected virtual XmlSchemaElement SearchElementInSchema(CodeTypeDeclaration type, XmlSchema schema)
        {
            foreach (XmlSchemaObject item in schema.Items)
            {
                var xmlElement = item as XmlSchemaElement;
                if (xmlElement == null) continue;

                var xmlSubElement = this.SearchElement(type, xmlElement, string.Empty, string.Empty);
                if (xmlSubElement != null) return xmlSubElement;
            }

            // If not found search in schema inclusion
            foreach (var item in schema.Includes)
            {
                var schemaInc = item as XmlSchemaInclude;
                if (schemaInc == null) continue;

                var includeElmts = this.SearchElementInSchema(type, schemaInc.Schema);
                if (includeElmts != null) return includeElmts;
            }

            return null;
        }

        /// <summary>
        /// Recursive search of elemement.
        /// </summary>
        /// <param name="type">Element to search</param>
        /// <param name="xmlElement">Current element</param>
        /// <param name="CurrentElementName">Name of the current element.</param>
        /// <param name="HierarchicalElementName">Name of the hierarchical element.</param>
        /// <returns>
        /// return found XmlSchemaElement or null value
        /// </returns>
        protected virtual XmlSchemaElement SearchElement(CodeTypeDeclaration type, XmlSchemaElement xmlElement,
                                                         string CurrentElementName, string HierarchicalElementName)
        {
            bool found = false;
            if (type.IsClass)
            {
                if (xmlElement.Name == null)
                    return null;

                if (type.Name.Equals(HierarchicalElementName + xmlElement.Name) ||
                    (type.Name.Equals(xmlElement.Name)))
                    found = true;
            }
            else
            {
                if (type.Name.Equals(xmlElement.QualifiedName.Name))
                    found = true;
            }

            if (found)
                return xmlElement;

            var xmlComplexType = xmlElement.ElementSchemaType as XmlSchemaComplexType;
            if (xmlComplexType != null)
            {
                var xmlSequence = xmlComplexType.ContentTypeParticle as XmlSchemaSequence;
                if (xmlSequence != null)
                {
                    foreach (XmlSchemaObject item in xmlSequence.Items)
                    {
                        var currentItem = item as XmlSchemaElement;
                        if (currentItem != null)
                        {
                            if (HierarchicalElementName == xmlElement.QualifiedName.Name ||
                                CurrentElementName == xmlElement.QualifiedName.Name)
                                return null;

                            XmlSchemaElement subItem = this.SearchElement(type, currentItem,
                                                                          xmlElement.QualifiedName.Name,
                                                                          HierarchicalElementName
                                                                          + xmlElement.QualifiedName.Name);
                            if (subItem != null)
                                return subItem;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Create CodeCommentStatement from schema documentation.
        /// </summary>
        /// <param name="codeStatmentColl">CodeCommentStatementCollection collection</param>
        /// <param name="xmlDoc">Schema documentation</param>
        protected virtual void CreateCommentStatment(CodeCommentStatementCollection codeStatmentColl,
                                                     XmlSchemaDocumentation xmlDoc)
        {
            codeStatmentColl.Clear();
            foreach (XmlNode itemDoc in xmlDoc.Markup)
            {
                string textLine = itemDoc.InnerText.Trim();
                if (textLine.Length > 0)
                    CodeDomHelper.CreateSummaryComment(codeStatmentColl, textLine);
            }
        }

        /// <summary>
        /// Field process.
        /// </summary>
        /// <param name="member">CodeTypeMember member</param>
        /// <param name="ctor">CodeMemberMethod constructor</param>
        /// <param name="ns">CodeNamespace XSD</param>
        /// <param name="addedToConstructor">Indicates if create a new constructor</param>
        protected virtual void ProcessField(CodeTypeMember member, CodeMemberMethod ctor, CodeNamespace ns,
                                            ref bool addedToConstructor)
        {
            var field = (CodeMemberField)member;
            
            #region Add EditorBrowsable.Never for protected virtual  Attribute
            // ---------------------------------------------
            // [EditorBrowsable(EditorBrowsableState.Never)]
            // ---------------------------------------------
            if (member.Attributes == MemberAttributes.Private)
            {
                if (GeneratorContext.GeneratorParams.HidePrivateFieldInIde)
                {
                    var attributeType = new CodeTypeReference(
                        typeof(EditorBrowsableAttribute).Name.Replace("Attribute", string.Empty));

                    var argument = new CodeAttributeArgument
                                       {
                                           Value = new CodePropertyReferenceExpression(
                                               new CodeSnippetExpression(typeof(EditorBrowsableState).Name), "Never")
                                       };

                    field.CustomAttributes.Add(new CodeAttributeDeclaration(attributeType, new[] { argument }));
                }
            }
            #endregion

            #region Change to generic collection type
            // ------------------------------------------
            // protected virtual  List <Actor> nameField;
            // ------------------------------------------
            bool thisIsCollectionType = field.Type.ArrayElementType != null;
            if (thisIsCollectionType)
            {
                CodeTypeReference colType = this.GetCollectionType(field.Type.BaseType);
                if (colType != null)
                    field.Type = colType;
            }
            #endregion

            #region Object allocation in CTor
            // ---------------------------------------
            // if ((this.nameField == null))
            // {
            //    this.nameField = new List<Name>();
            // }
            // ---------------------------------------
            if (GeneratorContext.GeneratorParams.CollectionObjectType != CollectionType.Array)
            {
                CodeTypeDeclaration declaration = this.FindTypeInNamespace(field.Type.BaseType, ns);
                if (((thisIsCollectionType && field.Type.ArrayElementType == null)
                     ||
                     (((declaration != null) && declaration.IsClass)
                      && ((declaration.TypeAttributes & TypeAttributes.Abstract) != TypeAttributes.Abstract))))
                {
                    ctor.Statements.Insert(0, this.CreateInstanceCodeStatments(field.Name, field.Type));
                    addedToConstructor = true;
                }
            }
            #endregion
        }

        /// <summary>
        /// Add INotifyPropertyChanged implementation
        /// </summary>
        /// <param name="type">type of declaration</param>
        /// <returns>return CodeConstructor</returns>
        protected virtual CodeConstructor ProcessClass(CodeTypeDeclaration type)
        {
            if (GeneratorContext.GeneratorParams.EnableDataBinding)
                type.BaseTypes.Add(typeof(INotifyPropertyChanged));

            var ctor = new CodeConstructor { Attributes = MemberAttributes.Public };
            return ctor;
        }

        /// <summary>
        /// Create new instance of object
        /// </summary>
        /// <param name="name">Name of object</param>
        /// <param name="type">CodeTypeReference Type</param>
        /// <returns>return instance CodeConditionStatement</returns>
        protected virtual CodeConditionStatement CreateInstanceCodeStatments(string name, CodeTypeReference type)
        {
            var statement =
                new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), name),
                                        new CodeObjectCreateExpression(type, new CodeExpression[0]));
            return
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), name),
                        CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
                    new CodeStatement[] { statement });
        }

        /// <summary>
        /// Recherche le CodeTypeDeclaration d'un objet en fonction de son type de base (nom de classe)
        /// </summary>
        /// <param name="typeName">Search name</param>
        /// <param name="ns">Seach into</param>
        /// <returns>CodeTypeDeclaration found</returns>
        protected virtual CodeTypeDeclaration FindTypeInNamespace(string typeName, CodeNamespace ns)
        {
            foreach (CodeTypeDeclaration declaration in ns.Types)
            {
                if (declaration.Name == typeName)
                    return declaration;
            }

            return null;
        }

        /// <summary>
        /// Property process
        /// </summary>
        /// <param name="type">Represents a type declaration for a class, structure, interface, or enumeration</param>
        /// <param name="member">Type members include fields, methods, properties, constructors and nested types</param>
        /// <param name="xmlElement">Represent the root element in schema</param>
        /// <param name="schema">XML Schema</param>
        protected virtual void ProcessProperty(CodeTypeDeclaration type, CodeTypeMember member,
                                               XmlSchemaElement xmlElement, XmlSchema schema)
        {
            #region Find item in XmlSchema for summary documentation.

            if (GeneratorContext.GeneratorParams.EnableSummaryComment)
            {
                if (xmlElement != null)
                {
                    var xmlComplexType = xmlElement.ElementSchemaType as XmlSchemaComplexType;
                    bool foundInAttributes = false;
                    if (xmlComplexType != null)
                    {
                        #region Search property in attributes for summary comment generation

                        foreach (XmlSchemaObject attribute in xmlComplexType.Attributes)
                        {
                            var xmlAttrib = attribute as XmlSchemaAttribute;
                            if (xmlAttrib != null)
                            {
                                if (member.Name.Equals(xmlAttrib.QualifiedName.Name))
                                {
                                    this.CreateCommentFromAnnotation(xmlAttrib.Annotation, member.Comments);
                                    foundInAttributes = true;
                                }
                            }
                        }

                        #endregion

                        #region Search property in XmlSchemaElement for summary comment generation

                        if (!foundInAttributes)
                        {
                            var xmlSequence = xmlComplexType.ContentTypeParticle as XmlSchemaSequence;
                            if (xmlSequence != null)
                            {
                                foreach (XmlSchemaObject item in xmlSequence.Items)
                                {
                                    var currentItem = item as XmlSchemaElement;
                                    if (currentItem != null)
                                    {
                                        if (member.Name.Equals(currentItem.QualifiedName.Name))
                                            this.CreateCommentFromAnnotation(currentItem.Annotation, member.Comments);
                                    }
                                }
                            }
                        }

                        #endregion
                    }
                }
            }

            #endregion

            var prop = (CodeMemberProperty)member;

            if (prop.Type.ArrayElementType != null)
            {
                CodeTypeReference colType = this.GetCollectionType(prop.Type.BaseType);
                if (colType != null)
                    prop.Type = colType;
            }

            if (GeneratorContext.GeneratorParams.GenerateDataContracts)
                this.CreateDataMemberAttribute(prop);

            // Add OnPropertyChanged in setter
            if (GeneratorContext.GeneratorParams.EnableDataBinding)
            {
                #region Setter adaptaion for databinding
                if (type.BaseTypes.IndexOf(new CodeTypeReference(typeof(CollectionBase))) == -1)
                {
                    // -----------------------------
                    // if (handler != null) {
                    //    OnPropertyChanged("Name");
                    // -----------------------------
                    //CodeExpressionStatement propChange = new CodeExpressionStatement(new CodeSnippetExpression("OnPropertyChanged(\"" + prop.Name + "\")"));
                    //CodeMethodInvokeExpression canDeserialize = CodeDomHelper.GetInvokeMethod("xmlSerializer", "CanDeserialize", new CodeExpression[] { new CodeSnippetExpression("xmlTextReader") });
                    var propChange =
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), "OnPropertyChanged"),
                            new CodeExpression[] { new CodeSnippetExpression("\"" + prop.Name + "\"") });

                    var propAssignStatment = prop.SetStatements[0] as CodeAssignStatement;
                    if (propAssignStatment != null)
                    {
                        var cfreL = propAssignStatment.Left as CodeFieldReferenceExpression;
                        var cfreR = propAssignStatment.Right as CodePropertySetValueReferenceExpression;

                        if (cfreL != null)
                        {
                            var setValueCondition = new CodeStatementCollection { propAssignStatment, propChange };
                            /*
                            CodeStatement[] setValueCondition = new CodeStatement[2];
                            setValueCondition[0] = propAssignStatment;
                            setValueCondition[1] = propChange;
                            */

                            // ---------------------------------------------
                            // if ((xxxField.Equals(value) != true)) { ... }
                            // ---------------------------------------------
                            var condStatmentCondEquals = new CodeConditionStatement(
                                new CodeBinaryOperatorExpression(
                                    new CodeMethodInvokeExpression(
                                        new CodeFieldReferenceExpression(
                                            null,
                                            cfreL.FieldName),
                                        "Equals",
                                        cfreR),
                                    CodeBinaryOperatorType.IdentityInequality,
                                    new CodePrimitiveExpression(true)),
                                CodeDomHelper.CodeStmtColToArray(setValueCondition));

                            // ---------------------------------------------
                            // if ((xxxField != null)) { ... }
                            // ---------------------------------------------
                            var condStatmentCondNotNull =
                                new CodeConditionStatement(
                                    new CodeBinaryOperatorExpression(
                                        new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                                                                         cfreL.FieldName),
                                        CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null)),
                                    new CodeStatement[] { condStatmentCondEquals },
                                    CodeDomHelper.CodeStmtColToArray(setValueCondition));

                            var property = member as CodeMemberProperty;

                            if (property != null)
                            {
                                if (property.Type.BaseType != new CodeTypeReference(typeof(long)).BaseType &&
                                    property.Type.BaseType != new CodeTypeReference(typeof(DateTime)).BaseType &&
                                    property.Type.BaseType != new CodeTypeReference(typeof(float)).BaseType &&
                                    property.Type.BaseType != new CodeTypeReference(typeof(double)).BaseType &&
                                    property.Type.BaseType != new CodeTypeReference(typeof(int)).BaseType &&
                                    property.Type.BaseType != new CodeTypeReference(typeof(bool)).BaseType)
                                    prop.SetStatements[0] = condStatmentCondNotNull;
                                else
                                    prop.SetStatements[0] = condStatmentCondEquals;
                            }
                        }
                        else
                            prop.SetStatements.Add(propChange);
                    }
                }

                #endregion
            }
        }

        /// <summary>
        /// Removes the default XML attributes.
        /// </summary>
        /// <param name="prop">The prop.</param>
        protected virtual void RemoveDefaultXMLAttributes(CodeAttributeDeclarationCollection customAttributes)
        {
            var codeAttributes = new List<CodeAttributeDeclaration>();
            foreach (var attribute in customAttributes)
            {
                var attrib = attribute as CodeAttributeDeclaration;
                if (attrib != null)
                {
                    if (attrib.Name == "System.Xml.Serialization.XmlAttributeAttribute" ||
                        attrib.Name == "System.Xml.Serialization.XmlIgnoreAttribute" ||
                        attrib.Name == "System.Xml.Serialization.XmlTypeAttribute" ||
                        attrib.Name == "System.Xml.Serialization.XmlElementAttribute" ||
                        attrib.Name == "System.CodeDom.Compiler.GeneratedCodeAttribute" ||
                        attrib.Name == "System.Xml.Serialization.XmlRootAttribute")
                    {
                        codeAttributes.Add(attrib);
                    }
                }
            }

            foreach (var item in codeAttributes)
            {
                customAttributes.Remove(item);
            }

        }

        /// <summary>
        /// Generate summary comment from XmlSchemaAnnotation 
        /// </summary>
        /// <param name="xmlSchemaAnnotation">XmlSchemaAnnotation from XmlSchemaElement or XmlSchemaAttribute</param>
        /// <param name="codeCommentStatementCollection">codeCommentStatementCollection from member</param>
        protected virtual void CreateCommentFromAnnotation(XmlSchemaAnnotation xmlSchemaAnnotation,
                                                           CodeCommentStatementCollection codeCommentStatementCollection)
        {
            if (xmlSchemaAnnotation != null)
            {
                foreach (XmlSchemaObject annotation in xmlSchemaAnnotation.Items)
                {
                    var xmlDoc = annotation as XmlSchemaDocumentation;
                    if (xmlDoc != null)
                        this.CreateCommentStatment(codeCommentStatementCollection, xmlDoc);
                }
            }
        }

        /// <summary>
        /// Get CodeTypeReference for collection
        /// </summary>
        /// <param name="baseType">base type to generate</param>
        /// <returns>return CodeTypeReference of collection</returns>
        protected virtual CodeTypeReference GetCollectionType(string baseType)
        {
            #region Generic collection
            if (baseType == typeof(byte).FullName)
            {
                // Never change byte[] to List<byte> etc.
                // Fix bug when translating hexBinary and base64Binary 
                return null;
            }

            CodeTypeReference collTypeRef = null;
            switch (GeneratorContext.GeneratorParams.CollectionObjectType)
            {
                case CollectionType.List:
                    collTypeRef = new CodeTypeReference("List", new[] { new CodeTypeReference(baseType) });
                    break;

                case CollectionType.ObservableCollection:
                    collTypeRef = new CodeTypeReference("ObservableCollection", new[] { new CodeTypeReference(baseType) });
                    break;

                case CollectionType.DefinedType:
                    string typname = baseType.Replace(".", string.Empty) + "Collection";

                    if (!collectionTypesField.Keys.Contains(typname))
                        collectionTypesField.Add(typname, baseType);

                    collTypeRef = new CodeTypeReference(typname);
                    break;
            }

            return collTypeRef;

            #endregion
        }

        /// <summary>
        /// Search defaut constructor. If not exist, create a new ctor.
        /// </summary>
        /// <param name="type">CodeTypeDeclaration type</param>
        /// <param name="newCTor">Indicates if new constructor</param>
        /// <returns>return current or new CodeConstructor</returns>
        protected virtual CodeConstructor GetConstructor(CodeTypeDeclaration type, ref bool newCTor)
        {
            #region get or set Constructor

            CodeConstructor ctor = null;
            foreach (CodeTypeMember member in type.Members)
            {
                if (member is CodeConstructor)
                    ctor = member as CodeConstructor;
            }

            if (ctor == null)
            {
                newCTor = true;
                ctor = this.ProcessClass(type);
            }

            if (GeneratorContext.GeneratorParams.EnableSummaryComment)
                CodeDomHelper.CreateSummaryComment(ctor.Comments, string.Format("{0} class constructor", ctor.Name));

            return ctor;

            #endregion
        }
    }
}
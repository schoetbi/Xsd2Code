using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Xsd2Code.Library.Helpers
{
    /// <summary>
    /// Code DOM manipulation helper methods 
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Modified 2009-02-20 by Ruslan Urban
    ///     Updated GetCloneMethod
    ///     Performed code cleanup to improve code readability
    /// 
    /// </remarks>
    internal static class CodeDomHelper
    {
        /// <summary>
        /// Add to CodeCommentStatementCollection summary documentation
        /// </summary>
        /// <param name="codeStatmentColl">Collection of CodeCommentStatement</param>
        /// <param name="comment">summary text</param>
        internal static void CreateSummaryComment(CodeCommentStatementCollection codeStatmentColl, string comment)
        {
            codeStatmentColl.Add(new CodeCommentStatement("<summary>", true));
            string[] lines = comment.Split(new[] {'\n'});

            foreach (string line in lines)
                codeStatmentColl.Add(new CodeCommentStatement(line.Trim(), true));

            codeStatmentColl.Add(new CodeCommentStatement("</summary>", true));
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="ctorParams">The c tor parameter.</param>
        /// <returns>return  the statment of new object</returns>
        internal static CodeVariableDeclarationStatement CreateObject(Type objectType, string objectName,
                                                                      params string[] ctorParams)
        {
            var ce = new List<CodeExpression>();

            foreach (var item in ctorParams)
                ce.Add(new CodeTypeReferenceExpression(item));

            return CreateObject(objectType, objectName, ce.ToArray());
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectName">Name of the object.</param>
        /// <param name="ctorParams">The ctor params.</param>
        /// <returns>return  the statment of new object</returns>
        internal static CodeVariableDeclarationStatement CreateObject(Type objectType, string objectName,
                                                                      params CodeExpression[] ctorParams)
        {
            return new CodeVariableDeclarationStatement(
                new CodeTypeReference(objectType),
                objectName,
                new CodeObjectCreateExpression(new CodeTypeReference(objectType), ctorParams));
        }

        /// <summary>
        /// Get CodeMethodInvokeExpression
        /// </summary>
        /// <param name="targetObject">Name of target object. Use this if empty</param>
        /// <param name="methodName">Name of method to invoke</param>
        /// <returns>CodeMethodInvokeExpression value</returns>
        internal static CodeMethodInvokeExpression GetInvokeMethod(string targetObject, string methodName)
        {
            return GetInvokeMethod(targetObject, methodName, null);
        }

        /// <summary>
        /// Get CodeMethodInvokeExpression
        /// </summary>
        /// <param name="targetObject">Name of target object. Use this if empty</param>
        /// <param name="methodName">Name of method to invoke</param>
        /// <param name="parameters">method params</param>
        /// <returns>CodeMethodInvokeExpression value</returns>
        internal static CodeMethodInvokeExpression GetInvokeMethod(string targetObject, string methodName,
                                                                   CodeExpression[] parameters)
        {
            var methodInvoke =
                parameters != null
                    ? new CodeMethodInvokeExpression(
                          new CodeMethodReferenceExpression(new CodeSnippetExpression(targetObject),
                                                            methodName), parameters)
                    : new CodeMethodInvokeExpression(
                          new CodeMethodReferenceExpression(new CodeSnippetExpression(targetObject),
                                                            methodName));

            return methodInvoke;
        }

        /// <summary>
        /// Getr return true statment
        /// </summary>
        /// <returns>statment of return code</returns>
        internal static CodeMethodReturnStatement GetReturnTrue()
        {
            return new CodeMethodReturnStatement(new CodeSnippetExpression("true"));
        }

        /// <summary>
        /// Get return false startment
        /// </summary>
        /// <returns>statment of return code</returns>
        internal static CodeMethodReturnStatement GetReturnFalse()
        {
            return new CodeMethodReturnStatement(new CodeSnippetExpression("false"));
        }

        /// <summary>
        /// Get Deserialize method
        /// </summary>
        /// <param name="type">represent a type declaration of class</param>
        /// <returns>Deserialize CodeMemberMethod</returns>
        internal static CodeMemberMethod GetDeserialize(CodeTypeDeclaration type)
        {
            var deserializeMethod = new CodeMemberMethod
                                        {
// ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
                                            Attributes = (MemberAttributes.Public | MemberAttributes.Static),
// ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
                                            Name = GeneratorContext.GeneratorParams.DeserializeMethodName
                                        };

            deserializeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (string), "xml"));

            var param = new CodeParameterDeclarationExpression(type.Name, "obj") {Direction = FieldDirection.Out};
            deserializeMethod.Parameters.Add(param);

            param = new CodeParameterDeclarationExpression(typeof (Exception), "exception")
                        {Direction = FieldDirection.Out};

            deserializeMethod.Parameters.Add(param);

            deserializeMethod.ReturnType = new CodeTypeReference(typeof (bool));

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

            tryStatmanentsCol.Add(CreateObject(typeof (StringReader), "stringReader", new[] {"xml"}));

            var serializerParameter = "xmlTextReader";
            switch (GeneratorContext.GeneratorParams.TargetFramework)
            {
                case TargetFramework.Silverlight20:
                    serializerParameter = "stringReader";
                    break;
                default:
                    tryStatmanentsCol.Add(CreateObject(typeof (XmlTextReader), "xmlTextReader", new[] {"stringReader"}));
                    break;
            }


            tryStatmanentsCol.Add(CreateObject(typeof (XmlSerializer), "xmlSerializer",
                                               new CodeExpression[] {typeofValue}));

            // ----------------------------------------------------------------------
            // if (xmlSerializer.CanDeserialize(xmlTextReader))
            // { ... } else { ... }
            // ----------------------------------------------------------------------
            // TODO: Not being used. Remove?
            var canDeserialize = GetInvokeMethod("xmlSerializer", "CanDeserialize",
                                                 new CodeExpression[]
                                                     {new CodeSnippetExpression("xmlTextReader")});

            // ----------------------------------------------------------
            // obj = (ClassName)xmlSerializer.Deserialize(xmlTextReader);
            // return true;
            // ----------------------------------------------------------
            var deserialize = GetInvokeMethod("xmlSerializer", "Deserialize",
                                              new CodeExpression[]
                                                  {
                                                      new CodeSnippetExpression(serializerParameter)
                                                  });

            var castExpr = new CodeCastExpression(type.Name, deserialize);
            var cdtAssignStmt = new CodeAssignStatement(new CodeSnippetExpression("obj"), castExpr);

            tryStatmanentsCol.Add(cdtAssignStmt);
            tryStatmanentsCol.Add(GetReturnTrue());

            // catch
            var catchClauses = GetCatchClause();

            var tryStatments = new CodeStatement[tryStatmanentsCol.Count];
            tryStatmanentsCol.CopyTo(tryStatments, 0);

            var trycatch = new CodeTryCatchFinallyStatement(tryStatments, catchClauses);
            deserializeMethod.Statements.Add(trycatch);

            // --------
            // Comments
            // --------
            deserializeMethod.Comments.AddRange(
                GetSummaryComment(string.Format("Deserializes workflow markup into an {0} object", type.Name)));

            deserializeMethod.Comments.Add(GetParamComment("xml", "string workflow markup to deserialize"));
            deserializeMethod.Comments.Add(GetParamComment("obj", string.Format("Output {0} object", type.Name)));
            deserializeMethod.Comments.Add(GetParamComment("exception", "output Exception value if deserialize failed"));

            deserializeMethod.Comments.Add(
                GetReturnComment("true if this XmlSerializer can deserialize the object; otherwise, false"));

            return deserializeMethod;
        }

        /// <summary>
        /// Return catch statments
        /// </summary>
        /// <returns>CodeCatchClause statments</returns>
        internal static CodeCatchClause[] GetCatchClause()
        {
            var catchStatmanents = new CodeStatement[2];

            catchStatmanents[0] = new CodeAssignStatement(
                new CodeSnippetExpression("exception"),
                new CodeSnippetExpression("ex"));

            catchStatmanents[1] = GetReturnFalse();

            var catchClause = new CodeCatchClause(
                "ex",
                new CodeTypeReference(typeof (Exception)),
                catchStatmanents);

            var catchClauses = new[] {catchClause};

            return catchClauses;
        }

        /// <summary>
        /// Codes the STMT col to array.
        /// </summary>
        /// <param name="statmentCollection">The statment collection.</param>
        /// <returns>return CodeStmtColToArray</returns>
        internal static CodeStatement[] CodeStmtColToArray(CodeStatementCollection statmentCollection)
        {
            var tryFinallyStatmanents = new CodeStatement[statmentCollection.Count];
            statmentCollection.CopyTo(tryFinallyStatmanents, 0);
            return tryFinallyStatmanents;
        }

        /// <summary>
        /// Get return CodeCommentStatement comment
        /// </summary>
        /// <param name="text">Return text comment</param>
        /// <returns>return return comment statment</returns>
        internal static CodeCommentStatement GetReturnComment(string text)
        {
            var comments = new CodeCommentStatement(string.Format("<returns>{0}</returns>", text));
            return comments;
        }

        #region GetSerialize herlper

        /// <summary>
        /// Gets the serialize CodeDOM method.
        /// </summary>
        /// <param name="type">The type object to serilize.</param>
        /// <returns>return the CodeDOM serialize method</returns>
        internal static CodeMemberMethod GetSerializeCodeDomMethod(CodeTypeDeclaration type)
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
                    new CodeTypeReference(typeof (XmlSerializer)), "xmlSerializer",
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof (XmlSerializer)), getTypeMethod)));

            serializeMethod.Statements.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (MemoryStream)), "memoryStream",
                    new CodeObjectCreateExpression(
                        new CodeTypeReference(typeof (MemoryStream)))));

            // --------------------------------------------------------------------------
            // xmlSerializer = new System.Xml.Serialization.XmlSerializer(this.GetType());
            // xmlSerializer.Serialize(memoryStream, this);
            // --------------------------------------------------------------------------
            serializeMethod.Statements.Add(
                GetInvokeMethod("xmlSerializer", "Serialize",
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
                GetInvokeMethod("memoryStream",
                                "Seek",
                                new CodeExpression[]
                                    {
                                        new CodeTypeReferenceExpression("0"),
                                        new CodeTypeReferenceExpression("System.IO.SeekOrigin.Begin")
                                    }));

            serializeMethod.Statements.Add(
                CreateObject(typeof (StreamReader), "streamReader", new[] {"memoryStream"}));


            var readToEnd = GetInvokeMethod("streamReader", "ReadToEnd");
            serializeMethod.Statements.Add(new CodeMethodReturnStatement(readToEnd));
            serializeMethod.ReturnType = new CodeTypeReference(typeof (string));

            // --------
            // Comments
            // --------
            serializeMethod.Comments.AddRange(
                GetSummaryComment(string.Format("Serializes current {0} object into an XML document", type.Name)));

            serializeMethod.Comments.Add(GetReturnComment("string XML value"));
            return serializeMethod;
        }

        #endregion

        #region LoadFromFile helper

        /// <summary>
        /// Gets the load from file CodeDOM method.
        /// </summary>
        /// <param name="type">The type CodeTypeDeclaration.</param>
        /// <returns>return the codeDom LoadFromFile method</returns>
        internal static CodeMemberMethod GetLoadFromFileCodeDomMethod(CodeTypeDeclaration type)
        {
            #region Method declaration

            var loadFromFileMethod = new CodeMemberMethod
                                         {
// ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
                                             Attributes = (MemberAttributes.Public | MemberAttributes.Static),
// ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
                                             Name = GeneratorContext.GeneratorParams.LoadFromFileMethodName
                                         };

            loadFromFileMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (string), "fileName"));

            var param = new CodeParameterDeclarationExpression(type.Name, "obj") {Direction = FieldDirection.Out};
            loadFromFileMethod.Parameters.Add(param);

            param = new CodeParameterDeclarationExpression(typeof (Exception), "exception")
                        {Direction = FieldDirection.Out};

            loadFromFileMethod.Parameters.Add(param);
            loadFromFileMethod.ReturnType = new CodeTypeReference(typeof (bool));

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
                                            CreateObject(typeof (FileStream),
                                                         "file",
                                                         new[] {"fileName", "FileMode.Open", "FileAccess.Read"}),
                                            CreateObject(typeof (StreamReader), "sr", new[] {"file"})
                                        };

            // ---------------------------------------------------------------------------
            // FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            // StreamReader sr = new StreamReader(file);
            // ---------------------------------------------------------------------------

            // ----------------------------------
            // string xmlString = sr.ReadToEnd();
            // ----------------------------------
            var readToEndInvoke = GetInvokeMethod("sr", "ReadToEnd");

            var xmlString = new CodeVariableDeclarationStatement(
                new CodeTypeReference(typeof (string)), "xmlString", readToEndInvoke);

            tryStatmanentsCol.Add(xmlString);
            tryStatmanentsCol.Add(GetInvokeMethod("sr", "Close"));
            tryStatmanentsCol.Add(GetInvokeMethod("file", "Close"));

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
                    new CodeExpression[] {xmlStringParam, objParam, expParam});

            var rstmts = new CodeMethodReturnStatement(deserializeInvoke);
            tryStatmanentsCol.Add(rstmts);

            #endregion

            #region catch

            var trycatch = new CodeTryCatchFinallyStatement(
                CodeStmtColToArray(tryStatmanentsCol), GetCatchClause());

            loadFromFileMethod.Statements.Add(trycatch);

            #endregion

            #region Comments

            loadFromFileMethod.Comments.AddRange(
                GetSummaryComment(string.Format("Deserializes workflow markup from file into an {0} object", type.Name)));

            loadFromFileMethod.Comments.Add(GetParamComment("xml", "string workflow markup to deserialize"));
            loadFromFileMethod.Comments.Add(GetParamComment("obj", string.Format("Output {0} object", type.Name)));
            loadFromFileMethod.Comments.Add(GetParamComment("exception", "output Exception value if deserialize failed"));

            loadFromFileMethod.Comments.Add(
                GetReturnComment("true if this XmlSerializer can deserialize the object; otherwise, false"));

            #endregion

            return loadFromFileMethod;
        }

        #endregion

        #region Comment helper

        /// <summary>
        /// Get summary CodeCommentStatementCollection comment
        /// </summary>
        /// <param name="text">Summary text comment</param>
        /// <returns>CodeCommentStatementCollection comment</returns>
        internal static CodeCommentStatementCollection GetSummaryComment(string text)
        {
            var comments = new CodeCommentStatementCollection
                               {
                                   new CodeCommentStatement("<summary>", true),
                                   new CodeCommentStatement(text, true),
                                   new CodeCommentStatement("</summary>", true)
                               };
            return comments;
        }

        /// <summary>
        /// Get param comment statment
        /// </summary>
        /// <param name="paramName">Param Name</param>
        /// <param name="text">param summary</param>
        /// <returns>CodeCommentStatement param</returns>
        internal static CodeCommentStatement GetParamComment(string paramName, string text)
        {
            var comments = new CodeCommentStatement(string.Format("<param name=\"{0}\">{1}</param>", paramName, text));
            return comments;
        }

        #endregion

        #region SaveToFile Helper

        /// <summary>
        /// Gets the save to file code DOM method.
        /// </summary>
        /// <param name="type">CodeTypeDeclaration type.</param>
        /// <returns>return the save to file code DOM method statment </returns>
        internal static CodeMemberMethod GetSaveToFileCodeDomMethod(CodeTypeDeclaration type)
        {
            var saveToFileMethod = new CodeMemberMethod
                                       {
                                           Attributes = MemberAttributes.Public,
                                           Name = GeneratorContext.GeneratorParams.SaveToFileMethodName
                                       };

            saveToFileMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof (string), "fileName"));

            var paramException = new CodeParameterDeclarationExpression(
                typeof (Exception), "exception") {Direction = FieldDirection.Out};

            saveToFileMethod.Parameters.Add(paramException);

            saveToFileMethod.ReturnType = new CodeTypeReference(typeof (bool));

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
                new CodeTypeReference(typeof (string)), "xmlString", serializeMethodInvoke);

            tryExpression.Add(xmlString);

            // --------------------------------------------------------------
            // System.IO.FileInfo xmlFile = new System.IO.FileInfo(fileName);
            // --------------------------------------------------------------
            tryExpression.Add(CreateObject(typeof (FileInfo), "xmlFile", new[] {"fileName"}));

            // ----------------------------------
            // StreamWriter Tex = xmlFile.CreateText();
            // ----------------------------------
            CodeMethodInvokeExpression createTextMethodInvoke = GetInvokeMethod("xmlFile", "CreateText");
            tryExpression.Add(
                new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (StreamWriter)),
                    "streamWriter",
                    createTextMethodInvoke));

            // ----------------------------------
            // streamWriter.WriteLine(xmlString);
            // ----------------------------------
            CodeMethodInvokeExpression writeLineMethodInvoke =
                GetInvokeMethod("streamWriter", "WriteLine",
                                new CodeExpression[]
                                    {
                                        new CodeSnippetExpression("xmlString")
                                    });
            tryExpression.Add(writeLineMethodInvoke);

            CodeMethodInvokeExpression closeMethodInvoke = GetInvokeMethod("streamWriter", "Close");

            tryExpression.Add(closeMethodInvoke);
            tryExpression.Add(GetReturnTrue());

            var tryStatment = new CodeStatement[tryExpression.Count];
            tryExpression.CopyTo(tryStatment, 0);

            // -----------
            // Catch {...}
            // -----------
            var catchstmts = new CodeStatement[2];
            catchstmts[0] = new CodeAssignStatement(new CodeSnippetExpression("exception"),
                                                    new CodeSnippetExpression("e"));

            catchstmts[1] = GetReturnFalse();
            var codeCatchClause = new CodeCatchClause("e", new CodeTypeReference(typeof (Exception)), catchstmts);

            var codeCatchClauses = new[] {codeCatchClause};

            var trycatch = new CodeTryCatchFinallyStatement(tryStatment, codeCatchClauses);
            saveToFileMethod.Statements.Add(trycatch);

            saveToFileMethod.Comments.AddRange(
                GetSummaryComment(string.Format("Serializes current {0} object into file", type.Name)));
            saveToFileMethod.Comments.Add(GetParamComment("fileName", "full path of outupt xml file"));
            saveToFileMethod.Comments.Add(GetParamComment("exception", "output Exception value if failed"));
            saveToFileMethod.Comments.Add(GetReturnComment("true if can serialize and save into file; otherwise, false"));

            return saveToFileMethod;
        }

        #endregion

        /// <summary>
        /// Create attribute
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static CodeAttributeDeclaration CreateSimpleAttribute(Type type)
        {
            var typeReference = new CodeTypeReference(type.Name.Replace("Attribute", string.Empty));
            return new CodeAttributeDeclaration(typeReference);
        }
    }
}
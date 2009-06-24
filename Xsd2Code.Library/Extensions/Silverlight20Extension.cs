// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Silverlight20Extension.cs" company="Xsd2Code">
//   N/A
// </copyright>
// <summary>
//   Implements code generation extension for Silverlight 2.0
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Xsd2Code.Library.Extensions
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;
    using Helpers;

    /// <summary>
    /// Implements code generation extension for Silverlight 2.0
    /// </summary>
    [CodeExtension(TargetFramework.Silverlight20)]
    public class Silverlight20Extension : CodeExtension
    {
        /// <summary>
        /// Override creation of the Deserialize method because Silverlight 2.0 does not support XmlTextReader object
        /// </summary>
        /// <param name="type">Code type declaration</param>
        /// <returns>return deserilize methods<see cref="CodeMemberMethod"/></returns>
        protected override CodeMemberMethod[] CreateDeserializeMethod(CodeTypeDeclaration type)
        {
            var deserializeMethodList = new List<CodeMemberMethod>();
            var deserializeMethod = new CodeMemberMethod
                                        {
                                            Attributes = MemberAttributes.Public | MemberAttributes.Static,
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

            tryStatmanentsCol.Add(CodeDomHelper.CreateObject(typeof(StringReader), "stringReader", new[] { "xml" }));
            tryStatmanentsCol.Add(CodeDomHelper.CreateObject(typeof(XmlSerializer), "xmlSerializer", new CodeExpression[] { typeofValue }));

            // ----------------------------------------------------------
            // obj = (ClassName)xmlSerializer.Deserialize(xmlTextReader);
            // return true;
            // ----------------------------------------------------------
            var deserialize = CodeDomHelper.GetInvokeMethod(
                                                            "xmlSerializer",
                                                            "Deserialize",
                                                            new CodeExpression[] { new CodeSnippetExpression("stringReader") });

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
                CodeDomHelper.GetSummaryComment(string.Format("Deserializes workflow markup into an {0} object", type.Name)));

            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("xml", "string workflow markup to deserialize"));
            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("obj", string.Format("Output {0} object", type.Name)));
            deserializeMethod.Comments.Add(CodeDomHelper.GetParamComment("exception", "output Exception value if deserialize failed"));

            deserializeMethod.Comments.Add(
                CodeDomHelper.GetReturnComment("true if this XmlSerializer can deserialize the object; otherwise, false"));

            deserializeMethodList.Add(deserializeMethod);
            return deserializeMethodList.ToArray();
        }
    }
}
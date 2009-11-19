// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CodeDomHelper.cs" company="Xsd2Code">
//   N/A
// </copyright>
// <summary>
//   Code DOM manipulation helper methods
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Xsd2Code.Library.Helpers
{
    using System;
    using System.CodeDom;
    using System.Collections.Generic;

    /// <summary>
    /// Code DOM manipulation helper methods 
    /// </summary>
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
            string[] lines = comment.Split(new[] { '\n' });
            foreach (string line in lines)
                codeStatmentColl.Add(new CodeCommentStatement(line.Trim(), true));
            codeStatmentColl.Add(new CodeCommentStatement("</summary>", true));
        }

        /// <summary>
        /// Creates the object.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="objectName">object name.</param>
        /// <param name="ctorParams">The c tor parameter.</param>
        /// <returns>return variable declaration</returns>
        internal static CodeVariableDeclarationStatement CreateObject(Type objectType, string objectName, params string[] ctorParams)
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
        /// <returns>return variable declaration</returns>
        internal static CodeVariableDeclarationStatement CreateObject(Type objectType, string objectName, params CodeExpression[] ctorParams)
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
        internal static CodeMethodInvokeExpression GetInvokeMethod(string targetObject, string methodName, CodeExpression[] parameters)
        {
            var methodInvoke =
                parameters != null
                    ? new CodeMethodInvokeExpression(
                          new CodeMethodReferenceExpression(new CodeSnippetExpression(targetObject), methodName), parameters)
                    : new CodeMethodInvokeExpression(
                          new CodeMethodReferenceExpression(new CodeSnippetExpression(targetObject), methodName));

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
                                                    new CodeTypeReference(typeof(Exception)),
                                                    catchStatmanents);

            var catchClauses = new[] { catchClause };
            return catchClauses;
        }

        /// <summary>
        /// Gets the throw clause.
        /// </summary>
        /// <returns>return catch...throw statment</returns>
        internal static CodeCatchClause[] GetThrowClause()
        {
            var catchStatmanents = new CodeStatementCollection();
            catchStatmanents.Add(new CodeThrowExceptionStatement(new CodeSnippetExpression("ex")));
            var catchClause = new CodeCatchClause(
                                                    "ex",
                                                    new CodeTypeReference(typeof(Exception)),
                                                    catchStatmanents.ToArray());

            var catchClauses = new[] { catchClause };
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

        /// <summary>
        /// Transform CodeStatementCollection into CodeStatement[]
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>array of CodeStatement</returns>
        internal static CodeStatement[] ToArray(this CodeStatementCollection collection)
        {
            CodeStatement[] cdst = null;
            if (collection != null)
            {
                cdst = new CodeStatement[collection.Count];
                collection.CopyTo(cdst, 0);
            }

            return cdst;
        }

        /// <summary>
        /// Gets the dispose.
        /// </summary>
        /// <param name="objectName">Name of the object.</param>
        /// <returns>return dispose CodeDom</returns>
        internal static CodeConditionStatement GetDispose(string objectName)
        {
            var statments = new CodeStatementCollection();
            statments.Add(GetInvokeMethod(objectName, "Dispose"));
            return
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeVariableReferenceExpression(objectName),
                        CodeBinaryOperatorType.IdentityInequality,
                        new CodePrimitiveExpression(null)),
                        statments.ToArray());
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="ctorParams">The ctor params.</param>
        /// <returns>return code of new objectinstance</returns>
        internal static CodeObjectCreateExpression CreateInstance(Type type)
        {
            return new CodeObjectCreateExpression(new CodeTypeReference(type));
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="ctorParams">The ctor params.</param>
        /// <returns>return code of new objectinstance</returns>
        internal static CodeObjectCreateExpression CreateInstance(Type type, params string[] ctorParams)
        {
            var ce = new List<CodeTypeReferenceExpression>();
            foreach (var item in ctorParams)
                ce.Add(new CodeTypeReferenceExpression(item));

            return CreateInstance(type, ce.ToArray());
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="ctorParams">The ctor params.</param>
        /// <returns>return code of new objectinstance</returns>
        internal static CodeObjectCreateExpression CreateInstance(Type type, CodeTypeReferenceExpression[] ctorParams)
        {
            return new CodeObjectCreateExpression(new CodeTypeReference(type), ctorParams);
        }
    }
}
using System;

namespace Xsd2Code.Library.Extensions
{
    /// <summary>
    /// Target framework attribute   class 
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Created 2009-03-16 by Ruslan Urban
    /// 
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class CodeExtensionAttribute : Attribute
    {
        public CodeExtensionAttribute(TargetFramework targetFramework)
        {
            this.targetFramework = targetFramework;
        }

        #region Property : TargetFramework

        /// <summary>
        /// Member field targetFramework
        /// </summary>
        private readonly TargetFramework targetFramework;

        /// <summary>
        /// TargetFramework
        /// </summary>
        public TargetFramework TargetFramework
        {
            get { return this.targetFramework; }
        }

        #endregion
    }
}
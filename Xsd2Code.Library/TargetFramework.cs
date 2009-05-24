﻿using System.ComponentModel;

namespace Xsd2Code.Library
{
    /// <summary>
    /// Code generation code base type enumeration
    /// </summary>
    /// <remarks>
    /// Revision history:
    /// 
    ///     Created 2009-02-20 by Ruslan Urban
    /// 
    /// </remarks>
    [DefaultValue(Net20)]
    public enum TargetFramework
    {
        [Description(".Net Framework 2.0")]
        Net20,

        [Description(".Net Framework 3.0")]
        Net30,
        
        [Description(".Net Framework 3.5")]
        Net35,

        [Description("Silverlight 2.0")]
        Silverlight20
    }
}
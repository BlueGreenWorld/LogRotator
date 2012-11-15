using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace LogRotator
{
    /// <summary>
    /// Action to perform on pattern matched files
    /// </summary>
    public enum PatternAction
    {
        /// <summary>
        /// Compress file and delete it permanently
        /// </summary>
        [Description("Compress file and delete it permanently")]
        Rotate,

        /// <summary>
        /// Delete file permenantly from disk
        /// </summary>
        [Description("Delete file permenantly from disk")]
        Delete
    }
}

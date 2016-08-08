// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the minimum events to support logging and diagnostics.
    /// </summary>
    public interface IMinimalEventSource
    {
        /// <summary>
        /// Helper method to display an informational message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Message(string msg, params object[] args);

        /// <summary>
        /// Helper method to display a verbose message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Debug(string msg, params object[] args);

        /// <summary>
        /// Helper method to display an error message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Error(string msg, params object[] args);

        /// <summary>
        /// Helper method to display an error message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Critical(string msg, params object[] args);

        /// <summary>
        /// Helper method to display an error message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Always(string msg, params object[] args);

        /// <summary>
        /// Helper method to display an error message.
        /// </summary>
        /// <param name="msg">String format template.</param>
        /// <param name="args">Parameters to fill in the values of the template.</param>
        void Warning(string msg, params object[] args);
    }
}

// NAnt - A .NET build tool
// Copyright (C) 2001-2004 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Gert Driesen (gert.driesen@ardatis.com)

using System;
using System.IO;
using System.Reflection;

using NAnt.Core;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

namespace NAnt.Core.Functions {
    /// <summary>
    /// Functions that return information about an operating system.
    /// </summary>
    [FunctionSet("operating-system", "Operating System")]
    public class OperatingSystemFunctions : FunctionSetBase {
        #region Public Instance Constructors

        public OperatingSystemFunctions(Project project, Location location, PropertyDictionary properties) : base(project, location, properties) { }

        #endregion Public Instance Constructors

        #region Public Static Methods
        
        /// <summary>
        /// Gets a <see cref="PlatformID" /> value that identifies the operating 
        /// system platform.
        /// </summary>
        /// <param name="operatingSystem">The operating system.</param>
        /// <returns>
        /// <see cref="PlatformID" /> value that identifies the operating system
        /// platform.
        /// </returns>
        /// <seealso cref="EnvironmentFunctions.GetOperatingSystem()" />
        [Function("get-platform")]
        public static PlatformID GetPlatform(OperatingSystem operatingSystem) {
            return operatingSystem.Platform; 
        }

        /// <summary>
        /// Gets a <see cref="Version" /> object that identifies this operating
        /// system.
        /// </summary>
        /// <param name="operatingSystem">The operating system.</param>
        /// <returns>
        /// A <see cref="Version" /> object that describes the major version, 
        /// minor version, build, and revision of the operating system.
        /// </returns>
        /// <seealso cref="EnvironmentFunctions.GetOperatingSystem()" />
        [Function("get-version")]
        public static Version GetVersion(OperatingSystem operatingSystem) {
            return operatingSystem.Version; 
        }

        /// <summary>
        /// Converts the value of the specified operating system to its equivalent
        /// <see cref="string" /> representation.
        /// </summary>
        /// <param name="operatingSystem">The operating system.</param>
        /// <returns>
        /// The <see cref="string" /> representation of 
        /// <paramref name="operatingSystem" />.
        /// </returns>
        /// <example>
        ///   <para>
        ///   Output string representation of the current operating system.
        ///   </para>
        ///   <code>
        ///     <![CDATA[
        /// <echo message="OS=${operating-system::to-string(environment::get-operating-system())}" />
        ///     ]]>
        ///   </code>
        ///   <para>If the operating system is Windows 2000, the output is:</para>
        ///   <code>
        /// Microsoft Windows NT 5.0.2195.0
        ///   </code>
        /// </example>
        /// <seealso cref="EnvironmentFunctions.GetOperatingSystem()" />
        [Function("to-string")]
        public static string ToString(OperatingSystem operatingSystem) {
            return operatingSystem.ToString();
        }
        
        #endregion Public Static Methods
    }
}

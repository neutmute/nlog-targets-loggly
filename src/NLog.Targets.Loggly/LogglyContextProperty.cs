// -----------------------------------------------------------------------
// Copyright 2013 Joe Fitzgerald
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
// -----------------------------------------------------------------------

using NLog.Config;
using NLog.Layouts;

namespace NLog.Targets
{
    /// <summary>
    /// Context property details for <see cref="LogglyTarget"/> 
    /// </summary>
    [NLogConfigurationItem]
    [ThreadAgnostic]
    public class LogglyContextProperty
    {
        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <docgen category='Property Options' order='10' />
        [RequiredParameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the layout that will be rendered as the attribute's value.
        /// </summary>
        /// <docgen category='Property Options' order='10' />
        [RequiredParameter]
        public Layout Layout { get; set; }

        /// <summary>
        /// Gets or sets when an empty value should cause the property to be included
        /// </summary>
        public bool IncludeEmptyValue { get; set; }
    }
}

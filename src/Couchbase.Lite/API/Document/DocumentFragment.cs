﻿// 
// DocumentFragment.cs
// 
// Author:
//     Jim Borden  <jim.borden@couchbase.com>
// 
// Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
namespace Couchbase.Lite
{
    /// <summary>
    /// A class representing a <see cref="Document"/> for use in the 
    /// unified data API (i.e. subscript operator)
    /// </summary>
    public sealed class DocumentFragment : IDictionaryFragment
    {
        #region Properties

        /// <summary>
        /// Gets the document that this fragment holds
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// Gets whether or not this document exists
        /// </summary>
        public bool Exists => Document != null;

#pragma warning disable 1591

        public Fragment this[string key] => Exists ? Document[key] : new Fragment(null, this, key);

#pragma warning restore 1591

        #endregion

        #region Constructors

        internal DocumentFragment(Document document)
        {
            Document = document;
        }

        #endregion
    }
}
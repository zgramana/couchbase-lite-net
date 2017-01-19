﻿//
//  LiteCoreQueryExecutor.cs
//
//  Author:
//  	Jim Borden  <jim.borden@couchbase.com>
//
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionVisitors;

namespace Couchbase.Lite.Linq
{
    internal class LiteCoreQueryExecutor : IQueryExecutor
    {
        private readonly Database _db;

        internal LiteCoreQueryExecutor(Database db)
        {
            _db = db;
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var visitor = new LiteCoreQueryModelVisitor();
            visitor.VisitQueryModel(queryModel);
            return Enumerable.Empty<T>();
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).Single();
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }
    }

    internal sealed class LiteCoreDebugExecutor : IQueryExecutor
    {
        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var visitor = new LiteCoreQueryModelVisitor();
            visitor.VisitQueryModel(queryModel);
            return new[] { visitor.GetJsonQuery() } as IEnumerable<T>;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).Single();
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }
    }
}
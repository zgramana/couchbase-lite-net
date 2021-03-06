﻿// 
//  ReplicatorConfiguration.cs
// 
//  Copyright (c) 2018 Couchbase, Inc All rights reserved.
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
using System.Security.Cryptography.X509Certificates;

using Couchbase.Lite.Internal.Logging;
using Couchbase.Lite.Support;
using Couchbase.Lite.Util;

using JetBrains.Annotations;

using LiteCore.Interop;

namespace Couchbase.Lite.Sync
{
    /// <summary>
    /// An enum representing the direction of a <see cref="Replicator"/>
    /// </summary>
    [Flags]
    public enum ReplicatorType
    {
        /// <summary>
        /// The replication will push data from local to remote
        /// </summary>
        Push = 1 << 0,

        /// <summary>
        /// The replication will pull data from remote to local
        /// </summary>
        Pull = 1 << 1,

        /// <summary>
        /// The replication will operate in both directions
        /// </summary>
        PushAndPull = Push | Pull
    }

    /// <summary>
    /// A set of flags describing the properties of a replicated
    /// document.
    /// </summary>
    [Flags]
    public enum DocumentFlags
    {
        /// <summary>
        /// The replication action represents a deletion of the
        /// document in question
        /// </summary>
        Deleted = 1 << 0,

        /// <summary>
        /// The replication action represents a loss of access from
        /// the server for the document in question (i.e. no more access
        /// granted from the sync function)
        /// </summary>
        AccessRemoved = 1 << 1
    }

    /// <summary>
    /// A class representing configuration options for a <see cref="Replicator"/>
    /// </summary>
    public sealed partial class ReplicatorConfiguration
    {
        #region Constants

        private const string Tag = nameof(ReplicatorConfiguration);

        #endregion

        #region Variables

        [NotNull]private readonly Freezer _freezer = new Freezer();
        private Authenticator _authenticator;
        private bool _continuous;
        private Func<Document, DocumentFlags, bool> _pushFilter;
        private Func<Document, DocumentFlags, bool> _pullValidator;
        private Database _otherDb;
        private Uri _remoteUrl;
        private ReplicatorType _replicatorType = ReplicatorType.PushAndPull;
        private C4SocketFactory _socketFactory;
        private IConflictResolver _resolver;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the class which will authenticate the replication
        /// </summary>
        [CanBeNull]
        public Authenticator Authenticator
        {
            get => _authenticator;
            set => _freezer.SetValue(ref _authenticator, value);
        }

        /// <summary>
        /// A set of Sync Gateway channel names to pull from.  Ignored for push replicatoin.
        /// The default value is null, meaning that all accessible channels will be pulled.
        /// Note: channels that are not accessible to the user will be ignored by Sync Gateway.
        /// </summary>
        [CanBeNull]
        public IList<string> Channels
        {
            get => Options.Channels;
            set => _freezer.PerformAction(() => Options.Channels = value);
        }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Replicator"/> should stay
        /// active indefinitely.  The default is <c>false</c>
        /// </summary>
        public bool Continuous
        {
            get => _continuous;
            set =>_freezer.SetValue(ref _continuous, value);
        }

        /// <summary>
        /// Gets the local database participating in the replication. 
        /// </summary>
        [NotNull]
        public Database Database { get; }

        /// <summary>
        /// A set of document IDs to filter by.  If not null, only documents with these IDs will be pushed
        /// and/or pulled
        /// </summary>
        [CanBeNull]
        public IList<string> DocumentIDs
        {
            get => Options.DocIDs;
            set => _freezer.PerformAction(() =>  Options.DocIDs = value);
        }

        /// <summary>
        /// Extra HTTP headers to send in all requests to the remote target
        /// </summary>
        [NotNull]
        public IDictionary<string, string> Headers
        {
            get => Options.Headers;
            set => _freezer.PerformAction(() => Options.Headers = CBDebug.MustNotBeNull(WriteLog.To.Sync, Tag, nameof(Headers), value));
        }

        /// <summary>
        /// Gets or sets a certificate to trust.  All other certificates received
        /// by a <see cref="Replicator"/> with this configuration will be rejected.
        /// </summary>
        [CanBeNull]
        public X509Certificate2 PinnedServerCertificate
        {
            get => Options.PinnedServerCertificate;
            set => _freezer.PerformAction(() => Options.PinnedServerCertificate = value);
        }

        /// <summary>
        /// Func delegate that takes Document input parameter and bool output parameter
        /// Document pull will be allowed if output is true, othewise, Document pull 
        /// will not be allowed
        /// </summary>
        [CanBeNull]
        public Func<Document, DocumentFlags, bool> PullFilter
        {
            get => _pullValidator;
            set => _freezer.PerformAction(() => _pullValidator = value);
        }

        /// <summary>
        /// Func delegate that takes Document input parameter and bool output parameter
        /// Document push will be allowed if output is true, othewise, Document push 
        /// will not be allowed
        /// </summary>
        [CanBeNull]
        public Func<Document, DocumentFlags, bool> PushFilter
        {
            get => _pushFilter;
            set => _freezer.PerformAction(() => _pushFilter = value);
        }

        /// <summary>
        /// A value indicating the direction of the replication.  The default is
        /// <see cref="ReplicatorType.PushAndPull"/> which is bidirectional
        /// </summary>
        public ReplicatorType ReplicatorType
        {
            get => _replicatorType;
            set => _freezer.SetValue(ref _replicatorType, value);
        }

        /// <summary>
        /// Gets or sets the value to enable/disable the auto-purge feature. 
        /// The default value is true which means that the document will be automatically purged 
        /// by the pull replicator when the user loses access to the document from both removed 
        /// and revoked scenarios. When the value is set to false, the document will not be 
        /// purged when the user loses access to the document.
        /// Note: Changing the value of the replicator will only affect the future change; 
        /// For example, changing from false to true will not purge the documents to which the 
        /// user’s access has been previously revoked even if the replicator is restarted with 
        /// reset checkpoint equals to true. 
        /// </summary>
        public bool EnableAutoPurge
        {
            get => Options.EnableAutoPurge;
            set => _freezer.PerformAction(() => Options.EnableAutoPurge = value);
        }

        /// <summary>
        /// Gets or sets the replicator heartbeat keep-alive interval. 
        /// The default is null (5 min interval is applied). 
        /// * <c>5</c> min interval is applied when Heartbeat is set to null.
        /// * null will be returned when default <c>5</c> min interval is applied.
        /// </summary>
        /// <exception cref="ArgumentException"> 
        /// Throw if set the Heartbeat to less or equal to 0 full seconds.
        /// </exception>
        public TimeSpan? Heartbeat
        {
            get => Options.Heartbeat;
            set => _freezer.PerformAction(() => Options.Heartbeat = value);
        }

        /// <summary>
        /// Gets or sets the Max number of retry attempts. The retry attempts will reset
        /// after the replicator is connected to a remote peer. 
        /// The default is <c>0</c> (<c>10</c> for a single shot replicator or 
        /// <see cref="int.MaxValue" /> for a continuous replicator is applied.)
        /// * <c>10</c> for a single shot replicator or <see cref="int.MaxValue" /> for a 
        /// continuous replicator is applied when user set MaxAttempts to 0.
        /// * 0 will be returned when default <c>10</c> for a single shot replicator or 
        /// <see cref="int.MaxValue" /> for a continuous replicator is applied.
        /// * Setting the value to 1 means that the replicator will try connect once and 
        /// the replicator will stop if there is a transient error.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Throw if set the MaxAttempts to a negative value.
        /// </exception>
        public int MaxAttempts
        {
            get => Options.MaxAttempts;
            set => _freezer.PerformAction(() => Options.MaxAttempts = value);
        }

        /// <summary>
        /// Gets or sets the Max delay between retries.
        /// The default is null (5 min interval is applied).
        /// * <c>5</c> min interval is applied when MaxAttemptsWaitTime is set to null.
        /// * null will be returned when default <c>5</c> min interval is applied.
        /// </summary>
        /// <exception cref="ArgumentException"> 
        /// Throw if set the MaxRetryWaitTime to less than 0 full seconds.
        /// </exception>
        public TimeSpan? MaxAttemptsWaitTime
        {
            get => Options.MaxAttemptsWaitTime;
            set => _freezer.PerformAction(() => Options.MaxAttemptsWaitTime = value);
        }

        /// <summary>
        /// Gets the target to replicate with (either <see cref="Database"/>
        /// or <see cref="Uri"/>
        /// </summary>
        [NotNull]
        public IEndpoint Target { get; }

        /// <summary>
        /// The implemented custom conflict resolver object can be registered to the replicator 
        /// at ConflictResolver property. The default value of the conflictResolver is null. 
        /// When the value is null, the default conflict resolution will be applied.
        /// </summary>
        [CanBeNull]
        public IConflictResolver ConflictResolver
        {
            get => _resolver;
            set => _freezer.PerformAction(() => _resolver = value);
        }

        internal TimeSpan CheckpointInterval
        {
            get => Options.CheckpointInterval;
            set => _freezer.PerformAction(() => Options.CheckpointInterval = value);
        }

        [NotNull]
        internal ReplicatorOptionsDictionary Options { get; set; } = new ReplicatorOptionsDictionary();

        [CanBeNull]
        internal Database OtherDB
        {
            get => _otherDb;
            set => _freezer.SetValue(ref _otherDb, value);
        }


        [CanBeNull]
        internal Uri RemoteUrl
        {
            get => _remoteUrl;
            set => _freezer.SetValue(ref _remoteUrl, value);
        }

        internal C4SocketFactory SocketFactory
        {
            get => _socketFactory.open != IntPtr.Zero ? _socketFactory : LiteCore.Interop.SocketFactory.InternalFactory;
            set => _freezer.SetValue(ref _socketFactory, value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new builder object with the required properties
        /// </summary>
        /// <param name="database">The database that will serve as the local side of the replication</param>
        /// <param name="target">The endpoint to replicate to, either local or remote</param>
        /// <exception cref="ArgumentException">Thrown if an unsupported <see cref="IEndpoint"/> implementation
        /// is provided as <paramref name="target"/></exception>
        public ReplicatorConfiguration([NotNull] Database database, [NotNull] IEndpoint target)
        {
            Database = CBDebug.MustNotBeNull(WriteLog.To.Sync, Tag, nameof(database), database);
            Target = CBDebug.MustNotBeNull(WriteLog.To.Sync, Tag, nameof(target), target);

            var castTarget = Misc.TryCast<IEndpoint, IEndpointInternal>(target);
            castTarget.Visit(this);
        }

        #endregion

        #region Internal Methods

        [NotNull]
        internal ReplicatorConfiguration Freeze()
        {
            var retVal = new ReplicatorConfiguration(Database, Target)
            {
                Authenticator = Authenticator,
                #if COUCHBASE_ENTERPRISE
                AcceptOnlySelfSignedServerCertificate = AcceptOnlySelfSignedServerCertificate,
                #endif
                Continuous = Continuous,
                PushFilter = PushFilter,
                PullFilter = PullFilter,
                ReplicatorType = ReplicatorType,
                ConflictResolver = ConflictResolver,
                Options = Options
            };

            retVal._freezer.Freeze("Cannot modify a ReplicatorConfiguration that is in use");
            return retVal;
        }

        #endregion
    }
}

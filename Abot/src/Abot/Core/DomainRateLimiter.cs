﻿using Abot.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Abot.Core
{
    /// <summary>
    /// Rate limits or throttles on a per domain basis
    /// </summary>
    public interface IDomainRateLimiter
    {
        /// <summary>
        /// If the domain of the param has been flagged for rate limiting, it will be rate limited according to the configured minimum crawl delay
        /// </summary>
        void RateLimit(Uri uri);

        /// <summary>
        /// Add a domain entry so that domain may be rate limited according the the param minumum crawl delay
        /// </summary>
        void AddDomain(Uri uri, long minCrawlDelayInMillisecs);

        /// <summary>
        /// Add/Update a domain entry so that domain may be rate limited according the the param minumum crawl delay
        /// </summary>
        void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs);

        /// <summary>
        /// Remove a domain entry so that it will no longer be rate limited
        /// </summary>
        void RemoveDomain(Uri uri);
    }

    public class DomainRateLimiter : IDomainRateLimiter
    {
        static ILogger _logger = new LoggerFactory().CreateLogger("AbotLogger");
        ConcurrentDictionary<string, IRateLimiter> _rateLimiterLookup = new ConcurrentDictionary<string, IRateLimiter>();
        long _defaultMinCrawlDelayInMillisecs;

        public DomainRateLimiter(long minCrawlDelayMillisecs)
        {
            if (minCrawlDelayMillisecs < 0)
                throw new ArgumentException("minCrawlDelayMillisecs");

            if(minCrawlDelayMillisecs > 0)
                _defaultMinCrawlDelayInMillisecs = minCrawlDelayMillisecs + 20;//IRateLimiter is always a little under so adding a little more time
        }

        public void RateLimit(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            var rateLimiter = GetRateLimter(uri, _defaultMinCrawlDelayInMillisecs);
            if (rateLimiter == null)
                return;

            var timer = Stopwatch.StartNew();
            rateLimiter.WaitToProceed();
            timer.Stop();

            if(timer.ElapsedMilliseconds > 10)
                _logger.LogDebug($"Rate limited [{uri.AbsoluteUri}] [{timer.ElapsedMilliseconds}] milliseconds");
        }

        public void AddDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            GetRateLimter(uri, Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs));//just calling this method adds the new domain
        }

        public void AddOrUpdateDomain(Uri uri, long minCrawlDelayInMillisecs)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (minCrawlDelayInMillisecs < 1)
                throw new ArgumentException("minCrawlDelayInMillisecs");

            var delayToUse = Math.Max(minCrawlDelayInMillisecs, _defaultMinCrawlDelayInMillisecs);
            if (delayToUse > 0)
            {
                var rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(delayToUse));

                _rateLimiterLookup.AddOrUpdate(uri.Authority, rateLimiter, (key, oldValue) => rateLimiter);
                _logger.LogDebug($"Added/updated domain [{uri.Authority}] with minCrawlDelayInMillisecs of [{delayToUse}] milliseconds");
            }
        }

        public void RemoveDomain(Uri uri)
        {
            IRateLimiter rateLimiter;
            _rateLimiterLookup.TryRemove(uri.Authority, out rateLimiter);
        }


        private IRateLimiter GetRateLimter(Uri uri, long minCrawlDelayInMillisecs)
        {
            IRateLimiter rateLimiter;
            _rateLimiterLookup.TryGetValue(uri.Authority, out rateLimiter);

            if (rateLimiter == null && minCrawlDelayInMillisecs > 0)
            {
                rateLimiter = new RateLimiter(1, TimeSpan.FromMilliseconds(minCrawlDelayInMillisecs));

                if (_rateLimiterLookup.TryAdd(uri.Authority, rateLimiter))
                    _logger.LogDebug($"Added new domain [{uri.Authority}] with minCrawlDelayInMillisecs of [{minCrawlDelayInMillisecs}] milliseconds");
                else
                    _logger.LogWarning($"Unable to add new domain [{uri.Authority}] with minCrawlDelayInMillisecs of [{minCrawlDelayInMillisecs}] milliseconds");
            }

            return rateLimiter;
        }
    }
}

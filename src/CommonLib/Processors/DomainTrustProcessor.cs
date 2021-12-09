﻿using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using SharpHoundCommonLib.Enums;
using SharpHoundCommonLib.LDAPQueries;
using SharpHoundCommonLib.OutputTypes;

namespace SharpHoundCommonLib.Processors
{
    public class DomainTrustProcessor
    {
        private readonly ILogger _log;
        private readonly ILDAPUtils _utils;

        public DomainTrustProcessor(ILDAPUtils utils, ILogger log = null)
        {
            _utils = utils;
            _log = log ?? Logging.LogProvider.CreateLogger("DomainTrustProc");
        }

        /// <summary>
        ///     Processes domain trusts for a domain object
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public IEnumerable<DomainTrust> EnumerateDomainTrusts(string domain)
        {
            var query = CommonFilters.TrustedDomains;
            foreach (var result in _utils.QueryLDAP(query, SearchScope.Subtree, CommonProperties.DomainTrustProps,
                domain))
            {
                var trust = new DomainTrust();
                var targetSidBytes = result.GetByteProperty("securityIdentifier");
                if (targetSidBytes == null || targetSidBytes.Length == 0)
                {
                    _log.LogTrace("Trust sid is null or empty for target: {domain}", domain);
                    continue;
                }

                string sid;
                try
                {
                    sid = new SecurityIdentifier(targetSidBytes, 0).Value;
                }
                catch
                {
                    _log.LogTrace("Failed to convert bytes to SID for target: {domain}", domain);
                    continue;
                }

                trust.TargetDomainSid = sid;

                if (int.TryParse(result.GetProperty("trustdirection"), out var td))
                {
                    trust.TrustDirection = (TrustDirection)td;
                }
                else
                {
                    _log.LogTrace("Failed to convert trustdirection for target: {domain}", domain);
                    continue;
                }


                TrustAttributes attributes;

                if (int.TryParse(result.GetProperty("trustattributes"), out var ta))
                {
                    attributes = (TrustAttributes)ta;
                }
                else
                {
                    _log.LogTrace("Failed to convert trustattributes for target: {domain}", domain);
                    continue;
                }

                trust.IsTransitive = (attributes & TrustAttributes.NonTransitive) == 0;
                var name = result.GetProperty("cn")?.ToUpper();
                if (name != null)
                    trust.TargetDomainName = name;

                trust.SidFilteringEnabled = (attributes & TrustAttributes.FilterSids) != 0;
                trust.TrustType = TrustAttributesToType(attributes);

                yield return trust;
            }
        }

        public static TrustType TrustAttributesToType(TrustAttributes attributes)
        {
            TrustType trustType;

            if ((attributes & TrustAttributes.WithinForest) != 0)
                trustType = TrustType.ParentChild;
            else if ((attributes & TrustAttributes.ForestTransitive) != 0)
                trustType = TrustType.Forest;
            else if ((attributes & TrustAttributes.TreatAsExternal) != 0 ||
                     (attributes & TrustAttributes.CrossOrganization) != 0)
                trustType = TrustType.External;
            else
                trustType = TrustType.Unknown;

            return trustType;
        }
    }
}
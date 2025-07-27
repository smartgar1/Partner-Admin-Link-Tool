// This file is no longer needed since Microsoft Graph dependency has been removed
// to avoid consent issues in organizations that block Microsoft Graph.
// PAL functionality works perfectly with just Azure Management API.

/*
using Microsoft.Kiota.Abstractions.Authentication;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PartnerAdminLinkTool.Core.Services
{
    public class SimpleAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _accessToken;
        public SimpleAccessTokenProvider(string accessToken)
        {
            _accessToken = accessToken;
        }

        public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_accessToken);
        }
    }
}
*/

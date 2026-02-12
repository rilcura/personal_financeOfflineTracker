using System.Net.Http.Headers;

namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed class AuthHeaderHandler(AuthSession authSession) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (authSession.IsAuthenticated)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

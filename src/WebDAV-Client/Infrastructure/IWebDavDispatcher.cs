using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebDav.Infrastructure;

internal interface IWebDavDispatcher
{
    Uri? BaseAddress { get; }

    Task<HttpResponse> SendAsync(Uri requestUri, HttpMethod method, RequestParameters requestParams, CancellationToken cancellationToken);
}
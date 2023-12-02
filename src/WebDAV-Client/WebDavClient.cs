using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using WebDav.Infrastructure;
using WebDav.Request;
using WebDav.Response;
using RequestHeaders = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>;

namespace WebDav;

/// <summary>
/// Represents a WebDAV client that can perform WebDAV operations.
/// </summary>
public class WebDavClient : IDisposable
{
    private const string MediaTypeXml = "application/xml";
    private static readonly Encoding DefaultEncoding = Encoding.UTF8;
    private static readonly Encoding FallbackEncoding = Encoding.UTF8;

    private IWebDavDispatcher _dispatcher = null!;
    private IResponseParser<PropfindResponse> _propfindResponseParser = null!;
    private IResponseParser<ProppatchResponse> _proppatchResponseParser = null!;
    private IResponseParser<LockResponse> _lockResponseParser = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebDavClient"/> class.
    /// </summary>
    [PublicAPI]
    public WebDavClient() : this(new WebDavClientParams())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebDavClient"/> class.
    /// </summary>
    /// <param name="params">The parameters of the WebDAV client.</param>
    [PublicAPI]
    public WebDavClient(WebDavClientParams @params) : this(ConfigureHttpClient(@params))
    {
        Check.NotNull(@params, nameof(@params));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebDavClient"/> class using a HttpClient.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    [PublicAPI]
    public WebDavClient(HttpClient httpClient)
    {
        Check.NotNull(httpClient, nameof(httpClient));

        SetWebDavDispatcher(new WebDavDispatcher(httpClient));

        var lockResponseParser = new LockResponseParser();
        SetPropfindResponseParser(new PropfindResponseParser(lockResponseParser));
        SetProppatchResponseParser(new ProppatchResponseParser());
        SetLockResponseParser(lockResponseParser);
    }

    /// <summary>
    /// Retrieves properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="PropfindResponse" /></returns>
    public Task<PropfindResponse> Propfind(string requestUri)
    {
        return Propfind(CreateUri(requestUri), new PropfindParameters());
    }

    /// <summary>
    /// Retrieves properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="PropfindResponse" /></returns>
    public Task<PropfindResponse> Propfind(Uri requestUri)
    {
        return Propfind(requestUri, new PropfindParameters());
    }

    /// <summary>
    /// Retrieves properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the PROPFIND operation.</param>
    /// <returns>An instance of <see cref="PropfindResponse" /></returns>
    public Task<PropfindResponse> Propfind(string requestUri, PropfindParameters parameters)
    {
        return Propfind(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Retrieves properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the PROPFIND operation.</param>
    /// <returns>An instance of <see cref="PropfindResponse" /></returns>
    public async Task<PropfindResponse> Propfind(Uri requestUri, PropfindParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var applyTo = parameters.ApplyTo ?? ApplyTo.Propfind.ResourceAndChildren;
        var headers = new RequestHeaders
        {
            new("Depth", DepthHeaderHelper.GetValueForPropfind(applyTo))
        };
        string requestBody = PropfindRequestBuilder.BuildRequestBody(parameters.CustomProperties, parameters.Namespaces);
        var requestParams = new RequestParameters { Headers = headers, Content = new StringContent(requestBody, DefaultEncoding, MediaTypeXml) };
        var response = await _dispatcher.SendAsync(requestUri, WebDavMethod.Propfind, requestParams, parameters.CancellationToken);
        var responseContent = await ReadContentAsString(response.Content).ConfigureAwait(false);
        return _propfindResponseParser.Parse(responseContent, response.StatusCode, response.Description);
    }

    /// <summary>
    /// Sets and/or removes properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the PROPPATCH operation.</param>
    /// <returns>An instance of <see cref="ProppatchResponse" /></returns>
    public Task<ProppatchResponse> Proppatch(string requestUri, ProppatchParameters parameters)
    {
        return Proppatch(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Sets and/or removes properties defined on the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the PROPPATCH operation.</param>
    /// <returns>An instance of <see cref="ProppatchResponse" /></returns>
    public async Task<ProppatchResponse> Proppatch(Uri requestUri, ProppatchParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders();
        if (!string.IsNullOrEmpty(parameters.LockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.LockToken)));
        }

        string requestBody = ProppatchRequestBuilder.BuildRequestBody(
            parameters.PropertiesToSet,
            parameters.PropertiesToRemove,
            parameters.Namespaces);

        var requestParams = new RequestParameters { Headers = headers, Content = new StringContent(requestBody, DefaultEncoding, MediaTypeXml) };

        var response = await _dispatcher.SendAsync(requestUri, WebDavMethod.Proppatch, requestParams, parameters.CancellationToken);
        var responseContent = await ReadContentAsString(response.Content).ConfigureAwait(false);

        return _proppatchResponseParser.Parse(responseContent, response.StatusCode, response.Description);
    }

    /// <summary>
    /// Creates a new collection resource at the location specified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Mkcol(string requestUri)
    {
        return Mkcol(CreateUri(requestUri), new MkColParameters());
    }

    /// <summary>
    /// Creates a new collection resource at the location specified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Mkcol(Uri requestUri)
    {
        return Mkcol(requestUri, new MkColParameters());
    }

    /// <summary>
    /// Creates a new collection resource at the location specified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the MKCOL operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Mkcol(string requestUri, MkColParameters parameters)
    {
        return Mkcol(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Creates a new collection resource at the location specified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the MKCOL operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> Mkcol(Uri requestUri, MkColParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders();
        if (!string.IsNullOrEmpty(parameters.LockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.LockToken)));
        }

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(requestUri, WebDavMethod.Mkcol, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return it without processing.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetRawFile(string requestUri)
    {
        return GetFileAsync(CreateUri(requestUri), false, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return it without processing.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetRawFile(Uri requestUri)
    {
        return GetFileAsync(requestUri, false, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return it without processing.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the GET operation.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetRawFile(string requestUri, GetFileParameters parameters)
    {
        return GetFileAsync(CreateUri(requestUri), false, parameters.CancellationToken);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return it without processing.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the GET operation.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetRawFile(Uri requestUri, GetFileParameters parameters)
    {
        return GetFileAsync(requestUri, false, parameters.CancellationToken);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return a processed response, if possible.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetProcessedFile(string requestUri)
    {
        return GetFileAsync(CreateUri(requestUri), true, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return a processed response, if possible.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetProcessedFile(Uri requestUri)
    {
        return GetFileAsync(requestUri, true, CancellationToken.None);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return a processed response, if possible.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the GET operation.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetProcessedFile(string requestUri, GetFileParameters parameters)
    {
        return GetFileAsync(CreateUri(requestUri), true, parameters.CancellationToken);
    }

    /// <summary>
    /// Retrieves the file identified by the request URI telling the server to return a processed response, if possible.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the GET operation.</param>
    /// <returns>An instance of <see cref="WebDavStreamResponse" /></returns>
    public Task<WebDavStreamResponse> GetProcessedFile(Uri requestUri, GetFileParameters parameters)
    {
        return GetFileAsync(requestUri, true, parameters.CancellationToken);
    }

    internal virtual async Task<WebDavStreamResponse> GetFileAsync(Uri requestUri, bool translate, CancellationToken cancellationToken)
    {
        Check.NotNull(requestUri, nameof(requestUri));

        var headers = new RequestHeaders
        {
            new("Translate", translate ? "t" : "f")
        };

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(requestUri, HttpMethod.Get, requestParams, cancellationToken);

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        return new WebDavStreamResponse(response.StatusCode, response.Description, stream);
    }

    /// <summary>
    /// Deletes the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Delete(string requestUri)
    {
        return Delete(CreateUri(requestUri), new DeleteParameters());
    }

    /// <summary>
    /// Deletes the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Delete(Uri requestUri)
    {
        return Delete(requestUri, new DeleteParameters());
    }

    /// <summary>
    /// Deletes the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the DELETE operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Delete(string requestUri, DeleteParameters parameters)
    {
        return Delete(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Deletes the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the DELETE operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> Delete(Uri requestUri, DeleteParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders();
        if (!string.IsNullOrEmpty(parameters.LockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.LockToken)));
        }

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(requestUri, HttpMethod.Delete, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> PutFile(string requestUri, Stream stream)
    {
        return PutFile(CreateUri(requestUri), stream, new PutFileParameters());
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> PutFile(Uri requestUri, Stream stream)
    {
        return PutFile(requestUri, stream, new PutFileParameters());
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <param name="contentType">The content type of the request body.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> PutFile(string requestUri, Stream stream, string contentType)
    {
        return PutFile(CreateUri(requestUri), stream, new PutFileParameters { ContentType = contentType });
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <param name="contentType">The content type of the request body.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> PutFile(Uri requestUri, Stream stream, string contentType)
    {
        return PutFile(requestUri, stream, new PutFileParameters { ContentType = contentType });
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <param name="parameters">Parameters of the PUT operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> PutFile(string requestUri, Stream stream, PutFileParameters parameters)
    {
        return PutFile(CreateUri(requestUri), stream, parameters);
    }

    /// <summary>
    /// Requests the resource to be stored under the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="stream">The stream of content of the resource.</param>
    /// <param name="parameters">Parameters of the PUT operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> PutFile(Uri requestUri, Stream stream, PutFileParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(stream, nameof(stream));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders();
        if (!string.IsNullOrEmpty(parameters.LockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.LockToken)));
        }

        var requestParams = new RequestParameters { Headers = headers, Content = new StreamContent(stream), ContentType = parameters.ContentType };

        var response = await _dispatcher.SendAsync(requestUri, HttpMethod.Put, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Creates a duplicate of the source resource identified by the source URI in the destination resource identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">A string that represents the source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">A string that represents the destination <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Copy(string sourceUri, string destUri)
    {
        return Copy(CreateUri(sourceUri), CreateUri(destUri), new CopyParameters());
    }

    /// <summary>
    /// Creates a duplicate of the source resource identified by the source URI in the destination resource identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">The source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">The destination <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Copy(Uri sourceUri, Uri destUri)
    {
        return Copy(sourceUri, destUri, new CopyParameters());
    }

    /// <summary>
    /// Creates a duplicate of the source resource identified by the source URI in the destination resource identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">A string that represents the source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">A string that represents the destination <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the COPY operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Copy(string sourceUri, string destUri, CopyParameters parameters)
    {
        return Copy(CreateUri(sourceUri), CreateUri(destUri), parameters);
    }

    /// <summary>
    /// Creates a duplicate of the source resource identified by the source URI in the destination resource identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">The source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">The destination <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the COPY operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> Copy(Uri sourceUri, Uri destUri, CopyParameters parameters)
    {
        Check.NotNull(sourceUri, nameof(sourceUri));
        Check.NotNull(destUri, nameof(destUri));
        Check.NotNull(parameters, nameof(parameters));

        var applyTo = parameters.ApplyTo ?? ApplyTo.Copy.ResourceAndAncestors;
        var headers = new RequestHeaders
        {
            new("Destination", GetAbsoluteUri(destUri).ToString()),
            new("Depth", DepthHeaderHelper.GetValueForCopy(applyTo)),
            new("Overwrite", parameters.Overwrite ? "T" : "F")
        };

        if (!string.IsNullOrEmpty(parameters.DestLockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.DestLockToken)));
        }

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(sourceUri, WebDavMethod.Copy, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Moves the resource identified by the source URI to the destination identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">A string that represents the source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">A string that represents the destination <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Move(string sourceUri, string destUri)
    {
        return Move(CreateUri(sourceUri), CreateUri(destUri), new MoveParameters());
    }

    /// <summary>
    /// Moves the resource identified by the source URI to the destination identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">The source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">The destination <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Move(Uri sourceUri, Uri destUri)
    {
        return Move(sourceUri, destUri, new MoveParameters());
    }

    /// <summary>
    /// Moves the resource identified by the source URI to the destination identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">A string that represents the source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">A string that represents the destination <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the MOVE operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Move(string sourceUri, string destUri, MoveParameters parameters)
    {
        return Move(CreateUri(sourceUri), CreateUri(destUri), parameters);
    }

    /// <summary>
    /// Moves the resource identified by the source URI to the destination identified by the destination URI.
    /// </summary>
    /// <param name="sourceUri">The source <see cref="T:System.Uri"/>.</param>
    /// <param name="destUri">The destination <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the MOVE operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> Move(Uri sourceUri, Uri destUri, MoveParameters parameters)
    {
        Check.NotNull(sourceUri, nameof(sourceUri));
        Check.NotNull(destUri, nameof(destUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders
        {
            new("Destination", GetAbsoluteUri(destUri).ToString()),
            new("Overwrite", parameters.Overwrite ? "T" : "F")
        };

        if (!string.IsNullOrEmpty(parameters.SourceLockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.SourceLockToken)));
        }

        if (!string.IsNullOrEmpty(parameters.DestLockToken))
        {
            headers.Add(new KeyValuePair<string, string>("If", IfHeaderHelper.GetHeaderValue(parameters.DestLockToken)));
        }

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(sourceUri, WebDavMethod.Move, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Takes out a shared lock or refreshes an existing lock of the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <returns>An instance of <see cref="LockResponse" /></returns>
    public Task<LockResponse> Lock(string requestUri)
    {
        return Lock(CreateUri(requestUri), new LockParameters());
    }

    /// <summary>
    /// Takes out a shared lock or refreshes an existing lock of the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <returns>An instance of <see cref="LockResponse" /></returns>
    public Task<LockResponse> Lock(Uri requestUri)
    {
        return Lock(requestUri, new LockParameters());
    }

    /// <summary>
    /// Takes out a lock of any type or refreshes an existing lock of the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the LOCK operation.</param>
    /// <returns>An instance of <see cref="LockResponse" /></returns>
    public Task<LockResponse> Lock(string requestUri, LockParameters parameters)
    {
        return Lock(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Takes out a lock of any type or refreshes an existing lock of the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the LOCK operation.</param>
    /// <returns>An instance of <see cref="LockResponse" /></returns>
    public async Task<LockResponse> Lock(Uri requestUri, LockParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders();
        if (parameters.ApplyTo.HasValue)
        {
            headers.Add(new KeyValuePair<string, string>("Depth", DepthHeaderHelper.GetValueForLock(parameters.ApplyTo.Value)));
        }

        if (parameters.Timeout.HasValue)
        {
            headers.Add(new KeyValuePair<string, string>("Timeout", $"Second-{parameters.Timeout.Value.TotalSeconds}"));
        }

        string requestBody = LockRequestBuilder.BuildRequestBody(parameters);

        var requestParams = new RequestParameters { Headers = headers, Content = new StringContent(requestBody, DefaultEncoding, MediaTypeXml) };

        var response = await _dispatcher.SendAsync(requestUri, WebDavMethod.Lock, requestParams, parameters.CancellationToken);

        if (!response.IsSuccessful)
        {
            return new LockResponse(response.StatusCode, response.Description);
        }

        var responseContent = await ReadContentAsString(response.Content).ConfigureAwait(false);

        return _lockResponseParser.Parse(responseContent, response.StatusCode, response.Description);
    }

    /// <summary>
    /// Removes the lock identified by the lock token from the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="lockToken">The resource lock token.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Unlock(string requestUri, string lockToken)
    {
        return Unlock(CreateUri(requestUri), new UnlockParameters(lockToken));
    }

    /// <summary>
    /// Removes the lock identified by the lock token from the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="lockToken">The resource lock token.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Unlock(Uri requestUri, string lockToken)
    {
        return Unlock(requestUri, new UnlockParameters(lockToken));
    }

    /// <summary>
    /// Removes the lock identified by the lock token from the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">A string that represents the request <see cref="T:System.Uri"/>.</param>
    /// <param name="parameters">Parameters of the UNLOCK operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public Task<WebDavResponse> Unlock(string requestUri, UnlockParameters parameters)
    {
        return Unlock(CreateUri(requestUri), parameters);
    }

    /// <summary>
    /// Removes the lock identified by the lock token from the resource identified by the request URI.
    /// </summary>
    /// <param name="requestUri">The <see cref="System.Uri"/> to request.</param>
    /// <param name="parameters">Parameters of the UNLOCK operation.</param>
    /// <returns>An instance of <see cref="WebDavResponse" /></returns>
    public async Task<WebDavResponse> Unlock(Uri requestUri, UnlockParameters parameters)
    {
        Check.NotNull(requestUri, nameof(requestUri));
        Check.NotNull(parameters, nameof(parameters));

        var headers = new RequestHeaders
        {
            new("Lock-Token", $"<{parameters.LockToken}>")
        };

        var requestParams = new RequestParameters { Headers = headers };

        var response = await _dispatcher.SendAsync(requestUri, WebDavMethod.Unlock, requestParams, parameters.CancellationToken);

        return new WebDavResponse(response.StatusCode, response.Description);
    }

    /// <summary>
    /// Sets the dispatcher of WebDAV requests.
    /// </summary>
    /// <param name="dispatcher">The dispatcher of WebDAV http requests.</param>
    /// <returns>This instance of <see cref="WebDavClient" /> to support chain calls.</returns>
    internal WebDavClient SetWebDavDispatcher(IWebDavDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        return this;
    }

    /// <summary>
    /// Sets the parser of PROPFIND responses.
    /// </summary>
    /// <param name="responseParser">The parser of WebDAV PROPFIND responses.</param>
    /// <returns>This instance of <see cref="WebDavClient" /> to support chain calls.</returns>
    internal WebDavClient SetPropfindResponseParser(IResponseParser<PropfindResponse> responseParser)
    {
        Check.NotNull(responseParser, nameof(responseParser));
        _propfindResponseParser = responseParser;
        return this;
    }

    /// <summary>
    /// Sets the parser of PROPPATCH responses.
    /// </summary>
    /// <param name="responseParser">The parser of WebDAV PROPPATCH responses.</param>
    /// <returns>This instance of <see cref="WebDavClient" /> to support chain calls.</returns>
    internal WebDavClient SetProppatchResponseParser(IResponseParser<ProppatchResponse> responseParser)
    {
        Check.NotNull(responseParser, nameof(responseParser));
        _proppatchResponseParser = responseParser;
        return this;
    }

    /// <summary>
    /// Sets the parser of LOCK responses.
    /// </summary>
    /// <param name="responseParser">The parser of WebDAV LOCK responses.</param>
    /// <returns>This instance of <see cref="WebDavClient" /> to support chain calls.</returns>
    internal WebDavClient SetLockResponseParser(IResponseParser<LockResponse> responseParser)
    {
        Check.NotNull(responseParser, nameof(responseParser));
        _lockResponseParser = responseParser;
        return this;
    }

    private static HttpClient ConfigureHttpClient(WebDavClientParams @params)
    {
        HttpMessageHandler httpMessageHandler;
        if (@params.HttpMessageHandler == null)
        {
            var httpHandler = new HttpClientHandler();

            // Fixes for Blazor WASM
            if (!RuntimeUtils.IsBlazorWASM)
            {
                httpHandler.UseDefaultCredentials = @params.UseDefaultCredentials;
                httpHandler.PreAuthenticate = @params.PreAuthenticate;
                httpHandler.UseProxy = @params.UseProxy;

                if (@params.Credentials != null)
                {
                    httpHandler.Credentials = @params.Credentials;
                }

                if (@params.Proxy != null)
                {
                    httpHandler.Proxy = @params.Proxy;
                }
            }

            // Fix for Blazor WASM
            if (httpHandler.SupportsAutomaticDecompression)
            {
                httpHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            }

            httpMessageHandler = httpHandler;
        }
        else
        {
            httpMessageHandler = @params.HttpMessageHandler;
        }

        HttpClient httpClient;
        if (RuntimeUtils.IsBlazorWASM)
        {
            httpClient = new HttpClient
            {
                BaseAddress = @params.BaseAddress,
                Timeout = @params.Timeout
            };
        }
        else
        {
            httpClient = new HttpClient(httpMessageHandler, true)
            {
                BaseAddress = @params.BaseAddress,
                Timeout = @params.Timeout
            };
        }

        foreach (var header in @params.DefaultRequestHeaders)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        return httpClient;
    }

    private static Uri CreateUri(string requestUri)
    {
        if (!string.IsNullOrEmpty(requestUri))
        {
            return new Uri(requestUri, UriKind.RelativeOrAbsolute);
        }

        throw CreateInvalidUriException();
    }

    private static Exception CreateInvalidUriException()
    {
        return new InvalidOperationException("An invalid request URI was provided. The request URI must either be an absolute URI or BaseAddress must be set.");
    }

    private static Encoding GetResponseEncoding(HttpContent content)
    {
        if (content.Headers.ContentType?.CharSet == null)
        {
            return FallbackEncoding;
        }

        try
        {
            return Encoding.GetEncoding(content.Headers.ContentType.CharSet);
        }
        catch (ArgumentException)
        {
            return FallbackEncoding;
        }
    }

    private static async Task<string> ReadContentAsString(HttpContent content)
    {
        var bytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var encoding = GetResponseEncoding(content);

#if NETSTANDARD1_1 || NETSTANDARD1_2 || PORTABLE
            return encoding.GetString(bytes, 0, bytes.Length);
#else
        return encoding.GetString(bytes);
#endif
    }

    private Uri GetAbsoluteUri(Uri uri)
    {
        if (uri == null && _dispatcher.BaseAddress == null)
        {
            throw CreateInvalidUriException();
        }

        if (uri == null)
        {
            return _dispatcher.BaseAddress!;
        }

        if (uri.IsAbsoluteUri)
        {
            return uri;
        }

        if (_dispatcher.BaseAddress == null)
        {
            throw CreateInvalidUriException();
        }

        return new Uri(_dispatcher.BaseAddress, uri);
    }

    #region IDisposable

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting managed/unmanaged resources.
    /// Disposes the underlying HttpClient.
    /// </summary>
    public void Dispose()
    {
        DisposeManagedResources();
    }

    /// <summary>
    /// Disposes the managed resources.
    /// </summary>
    protected virtual void DisposeManagedResources()
    {
        if (_dispatcher is IDisposable disposableDispatcher)
        {
            disposableDispatcher.Dispose();
        }
    }

    #endregion
}
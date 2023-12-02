﻿using NSubstitute;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebDav.Infrastructure;

namespace WebDav.Client.Tests.TestDoubles
{
    public static class Dispatcher
    {
        internal static IWebDavDispatcher Mock(string content = "", int statusCode = 200, string description = "OK")
        {
            var dispatcher = Substitute.For<IWebDavDispatcher>();
            dispatcher
                .SendAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<RequestParameters>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(new HttpResponse(new StringContent(content, Encoding.UTF8, "application/xml"), statusCode, description)));
            return dispatcher;
        }

        internal static IWebDavDispatcher MockFaulted()
        {
            var dispatcher = Substitute.For<IWebDavDispatcher>();
            dispatcher
                .SendAsync(Arg.Any<Uri>(), Arg.Any<HttpMethod>(), Arg.Any<RequestParameters>(), Arg.Any<CancellationToken>())
                .Returns(x => Task.FromResult(new HttpResponse(new StringContent(string.Empty), 500, "Internal Server Error")));
            return dispatcher;
        }
    }
}

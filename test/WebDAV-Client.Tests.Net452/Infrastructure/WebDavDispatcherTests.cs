﻿using System;
using NSubstitute;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebDav.Infrastructure;
using Xunit;

namespace WebDav.Client.Tests.Infrastructure
{
    public class WebDavDispatcherTests
    {
        [Fact]
        public async void When_RequestIsSent_Should_ReceiveResponseAndStatusCode200()
        {
            using (var dispatcher = new WebDavDispatcher(ConfigureHttpClient()))
            {
                var response = await dispatcher.SendAsync(new Uri("http://example.com"), HttpMethod.Get, new RequestParameters(), CancellationToken.None);

                Assert.IsType(typeof(HttpResponse), response);
                Assert.Equal(200, response.StatusCode);
            }
        }

        [Fact]
        public async void When_ContentIsSent_Should_ReceiveResponseAndStatusCode200()
        {
            using (var dispatcher = new WebDavDispatcher(ConfigureHttpClient()))
            {
                var requestParams = new RequestParameters { Content = new StringContent("content", Encoding.UTF8, "application/xml") };
                var response = await dispatcher.SendAsync(new Uri("http://example.com"), HttpMethod.Put, requestParams, CancellationToken.None);

                Assert.IsType(typeof(HttpResponse), response);
                Assert.Equal(200, response.StatusCode);
            }
        }

        [Fact]
        public async void When_ResponseIsReceived_Should_ReadContent()
        {
            const string responseContent = "content";
            using (var dispatcher = new WebDavDispatcher(ConfigureHttpClient(responseContent)))
            {
                var response = await dispatcher.SendAsync(new Uri("http://example.com"), HttpMethod.Get, new RequestParameters(), CancellationToken.None);
                Assert.Equal(responseContent, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async void When_CancellationIsRequested_Should_CancelRequest()
        {
            using (var dispatcher = new WebDavDispatcher(ConfigureHttpClient()))
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();

                await Assert.ThrowsAsync<OperationCanceledException>(
                    () => dispatcher.SendAsync(new Uri("http://example.com"), HttpMethod.Get, new RequestParameters(), cts.Token));
            }
        }

        [Fact]
        public async void When_DisposeIsCalled_Should_DisposeHttpClient()
        {
            var httpClient = ConfigureHttpClient();
            using (var dispatcher = new WebDavDispatcher(httpClient))
            {
                await dispatcher.SendAsync(new Uri("http://example.com"), HttpMethod.Get, new RequestParameters(), CancellationToken.None);
                httpClient.DidNotReceive().Dispose();
            }

            httpClient.Received().Dispose();
        }

        private static HttpClient ConfigureHttpClient(string responseContent = "")
        {
            var httpClient = Substitute.For<HttpClient>();
            httpClient
                .SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
                .Returns(x =>
                {
                    x.Arg<CancellationToken>().ThrowIfCancellationRequested();
                    return Task.FromResult(new HttpResponseMessage { Content = new StringContent(responseContent, Encoding.UTF8, "application/xml") });
                });
            return httpClient;
        }
    }
}

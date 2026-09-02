using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class SportarrApiClientEpisodeNumberTests
{
    [Fact]
    public async Task GetEpisodeNumbersFromApiAsync_IndexesHubAndLegacyIds()
    {
        const string responseBody = """
            {
              "episodes": [
                {
                  "id": "ev-361479",
                  "tsdb_id": "2368487",
                  "episode_number": 99
                },
                {
                  "id": "ev-361480",
                  "tsdb_id": "2368488",
                  "episode_number": 100
                },
                {
                  "id": "ev-without-slot",
                  "tsdb_id": "9999999",
                  "episode_number": null
                }
              ]
            }
            """;

        using var handler = new StubHttpMessageHandler(request =>
        {
            request.RequestUri.Should().Be(
                new Uri("https://metadata.example/api/metadata/plex/series/4407/season/2026/episodes"));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var dataPath = Path.Combine(Path.GetTempPath(), $"sportarr-tests-{Guid.NewGuid():N}");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sportarr:DataPath"] = dataPath,
                ["SportarrApi:BaseUrl"] = "https://metadata.example/api/v2/json"
            })
            .Build();
        var configService = new ConfigService(configuration, NullLogger<ConfigService>.Instance);
        var client = new SportarrApiClient(
            httpClient,
            NullLogger<SportarrApiClient>.Instance,
            configuration,
            configService,
            cache);

        try
        {
            var result = await client.GetEpisodeNumbersFromApiAsync("4407", "2026");

            result.Should().NotBeNull();
            result!["ev-361479"].Should().Be(99);
            result["2368487"].Should().Be(99);
            result["ev-361480"].Should().Be(100);
            result["2368488"].Should().Be(100);
            result.Should().NotContainKey("ev-without-slot");
            result.Should().NotContainKey("9999999");
        }
        finally
        {
            if (Directory.Exists(dataPath))
            {
                Directory.Delete(dataPath, recursive: true);
            }
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}

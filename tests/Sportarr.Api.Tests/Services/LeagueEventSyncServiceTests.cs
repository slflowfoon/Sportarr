using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sportarr.Api.Data;
using Sportarr.Api.Models;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class LeagueEventSyncServiceTests
{
    [Fact]
    public async Task SyncLeagueEventsAsync_MatchesLegacyIdWithoutReplacingLocalState()
    {
        var options = new DbContextOptionsBuilder<SportarrDbContext>()
            .UseInMemoryDatabase($"sportarr-tests-{Guid.NewGuid():N}")
            .Options;
        await using var db = new SportarrDbContext(options);

        var league = new League
        {
            ExternalId = "4407",
            Name = "MotoGP",
            Sport = "Motorsport",
            Monitored = false,
            MonitorType = MonitorType.None
        };
        db.Leagues.Add(league);
        await db.SaveChangesAsync();

        var legacyEvent = new Event
        {
            ExternalId = "2368487",
            Title = "Aragon Sprint Race",
            Sport = "Motorsport",
            LeagueId = league.Id,
            Season = "2026",
            SeasonNumber = 2026,
            EpisodeNumber = 40,
            EventDate = new DateTime(2026, 8, 29, 13, 0, 0, DateTimeKind.Utc),
            Monitored = true
        };
        db.Events.Add(legacyEvent);
        await db.SaveChangesAsync();
        var legacyDatabaseId = legacyEvent.Id;

        const string scheduleResponse = """
            {
              "data": {
                "schedule": [
                  {
                    "idEvent": "ev-361479",
                    "tsdbId": "2368487",
                    "strEvent": "Aragon - Sprint Race",
                    "strSport": "Motorsport",
                    "strSeason": "2026",
                    "strTimestamp": "2026-08-29T13:00:00+00:00",
                    "dateEvent": "2026-08-29"
                  }
                ]
              }
            }
            """;
        const string episodesResponse = """
            {
              "episodes": [
                {
                  "id": "ev-361479",
                  "tsdb_id": "2368487",
                  "episode_number": 99
                }
              ]
            }
            """;

        using var handler = new StubHttpMessageHandler(request =>
        {
            var body = request.RequestUri!.AbsolutePath.Contains("/schedule/", StringComparison.Ordinal)
                ? scheduleResponse
                : episodesResponse;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
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
        var apiClient = new SportarrApiClient(
            httpClient,
            NullLogger<SportarrApiClient>.Instance,
            configuration,
            configService,
            cache);
        var fileNamingService = new FileNamingService(NullLogger<FileNamingService>.Instance);
        var fileRenameService = new FileRenameService(
            db,
            fileNamingService,
            apiClient,
            NullLogger<FileRenameService>.Instance);
        var syncService = new LeagueEventSyncService(
            db,
            apiClient,
            fileRenameService,
            NullLogger<LeagueEventSyncService>.Instance);

        try
        {
            var result = await syncService.SyncLeagueEventsAsync(league.Id, ["2026"]);

            result.Success.Should().BeTrue();
            result.NewCount.Should().Be(0);
            result.RemovedCount.Should().Be(0);

            var syncedEvents = await db.Events.ToListAsync();
            syncedEvents.Should().ContainSingle();
            var syncedEvent = syncedEvents[0];
            syncedEvent.Id.Should().Be(legacyDatabaseId);
            syncedEvent.ExternalId.Should().Be("2368487");
            syncedEvent.Title.Should().Be("Aragon - Sprint Race");
            syncedEvent.EpisodeNumber.Should().Be(99);
            syncedEvent.Monitored.Should().BeTrue();
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

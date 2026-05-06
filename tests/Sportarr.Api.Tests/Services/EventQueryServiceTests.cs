using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sportarr.Api.Models;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class EventQueryServiceTests
{
    [Fact]
    public void BuildEventQueries_ShouldUseBsbPrefixForBritishSuperbike()
    {
        var service = new EventQueryService(NullLogger<EventQueryService>.Instance);
        var evt = new Event
        {
            Title = "Oulton Park Race 1",
            Sport = "Motorsport",
            EventDate = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc),
            Round = "1",
            League = new League
            {
                Name = "British Superbike Championship",
                Sport = "Motorsport"
            }
        };

        var queries = service.BuildEventQueries(evt);

        queries.Should().Equal("BSB 2026 Round01", "BSB 2026");
    }

    [Fact]
    public void BuildQueryFromTemplate_ShouldNormalizeBritishSuperbikeLeagueToBsb()
    {
        var service = new EventQueryService(NullLogger<EventQueryService>.Instance);
        var evt = new Event
        {
            Title = "Oulton Park Race 1",
            Sport = "Motorsport",
            EventDate = new DateTime(2026, 5, 4, 12, 0, 0, DateTimeKind.Utc),
            Round = "1",
            League = new League
            {
                Name = "British Superbike Championship",
                Sport = "Motorsport"
            }
        };

        var query = service.BuildQueryFromTemplate("{League} {Year} Round{Round}", evt);

        query.Should().Be("BSB 2026 Round01");
    }
}

using FluentAssertions;
using Sportarr.Api.Models;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class ReleaseMatchScorerTests
{
    [Fact]
    public void CalculateMatchScore_ShouldRejectMotorsportGearUpReleaseForRaceEvent()
    {
        var scorer = new ReleaseMatchScorer();
        var evt = new Event
        {
            Title = "Monaco Grand Prix Race",
            Sport = "Motorsport",
            EventDate = new DateTime(2026, 5, 24, 13, 0, 0, DateTimeKind.Utc),
            League = new League
            {
                Name = "Formula 1",
                Sport = "Motorsport"
            }
        };

        var score = scorer.CalculateMatchScore(
            "Formula.1.2026.Monaco.Grand.Prix.Gear.Up.1080p.WEB-DL",
            evt);

        score.Should().Be(0);
    }

    [Fact]
    public void CalculateMatchScore_ShouldMatchBritishSuperbikeAbbreviation()
    {
        var scorer = new ReleaseMatchScorer();
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

        var score = scorer.CalculateMatchScore(
            "BSB 2026 Round01 Oulton Park International Race One TNT WEB-DL 1080p H264 DDP5 1 English-MWR",
            evt);

        score.Should().BeGreaterThanOrEqualTo(ReleaseMatchScorer.MinimumMatchScore);
    }
}

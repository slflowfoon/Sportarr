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
}

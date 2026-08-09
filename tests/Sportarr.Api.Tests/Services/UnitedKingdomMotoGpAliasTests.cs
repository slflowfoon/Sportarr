using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sportarr.Api.Models;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class UnitedKingdomMotoGpAliasTests
{
    [Theory]
    [InlineData("MotoGP 2026 Round12 Great Britain Sprint TNT WEB-DL 1080p H264 DDP5 1 English-MWR")]
    [InlineData("MotoGP 2026 Round12 Great Britain Sprint WEB-DL 1080p H264 English-MWR")]
    public void CalculateMatchScore_ShouldMatchGreatBritainReleaseForUnitedKingdomSprintEvent(string releaseTitle)
    {
        var scorer = new ReleaseMatchScorer();
        var evt = new Event
        {
            Title = "United Kingdom Sprint Race",
            Sport = "Motorsport",
            EventDate = new DateTime(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc),
            Round = "12",
            League = new League
            {
                Name = "MotoGP",
                Sport = "Motorsport"
            }
        };

        var score = scorer.CalculateMatchScore(releaseTitle, evt);

        score.Should().BeGreaterThanOrEqualTo(ReleaseMatchScorer.AutoGrabMatchScore);
    }

    [Fact]
    public void ValidateRelease_ShouldNotRejectGreatBritainReleaseForUnitedKingdomSprintEvent()
    {
        var matcher = new ReleaseMatchingService(
            NullLogger<ReleaseMatchingService>.Instance,
            new SportsFileNameParser(NullLogger<SportsFileNameParser>.Instance),
            new EventPartDetector(NullLogger<EventPartDetector>.Instance));
        var release = new ReleaseSearchResult
        {
            Title = "MotoGP 2026 Round12 Great Britain Sprint WEB-DL 1080p H264 English-MWR",
            Guid = "test-guid",
            DownloadUrl = "https://example.invalid/download",
            Indexer = "test"
        };
        var evt = new Event
        {
            Title = "United Kingdom Sprint Race",
            Sport = "Motorsport",
            EventDate = new DateTime(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc),
            Round = "12",
            League = new League
            {
                Name = "MotoGP",
                Sport = "Motorsport"
            }
        };

        var result = matcher.ValidateRelease(release, evt);

        result.IsMatch.Should().BeTrue();
        result.Rejections.Should().NotContain(r => r.Contains("Location mismatch", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateSearchVariations_ShouldIncludeGreatBritainAliasesForUnitedKingdom()
    {
        var variations = SearchNormalizationService.GenerateSearchVariations("MotoGP 2026 United Kingdom Sprint Race");

        variations.Should().Contain("MotoGP 2026 Great Britain Sprint Race");
        variations.Should().Contain("MotoGP 2026 Britain Sprint Race");
        variations.Should().Contain("MotoGP 2026 UK Sprint Race");
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sportarr.Api.Services;

namespace Sportarr.Api.Tests.Services;

public class SportsFileNameParserTests
{
    [Fact]
    public void Parse_ShouldExtractBritishSuperbikeRoundRelease()
    {
        var parser = new SportsFileNameParser(NullLogger<SportsFileNameParser>.Instance);

        var result = parser.Parse("BSB 2026 Round01 Oulton Park International Race One TNT WEB-DL 1080p H264 DDP5 1 English-MWR");

        result.Sport.Should().Be("Motorsport");
        result.Organization.Should().Be("BSB");
        result.EventYear.Should().Be(2026);
        result.RoundNumber.Should().Be(1);
        result.Location.Should().Be("Oulton Park International Race One");
        result.Session.Should().Be("Race");
    }
}

using AwesomeAssertions;
using Xunit;
using static Blindbudet.Domain.Tests.Fixtures;

namespace Blindbudet.Domain.Tests;

/// <summary>
/// Non-spec test: mini-pack sampling asserts count + containment, which the spec's
/// value-pinning `tests:` cannot express (like tank's SolverTests).
/// </summary>
public class SamplingTests
{
    [Fact]
    public void mini_pack_is_sampled_to_a_short_round()
    {
        var result = Decider.Decide(
            AuctionState.Initial, new OpenAuction("Martin", "blindbudet-mini"), Context);

        if (result is not Ok<AuctionEvent[]> ok)
        {
            Assert.Fail("expected Ok (events), got an error");
            return;
        }

        // Concrete `is` pattern only (the union runtime-type trap).
        AuctionOpened? opened = null;
        foreach (var e in ok.Value)
            if (e is AuctionOpened a)
                opened = a;

        opened.Should().NotBeNull();
        opened!.Lots.Should().HaveCount(Decider.MiniAuctionSize);
        opened.Lots.Should().OnlyContain(l => MiniPack.Lots.Contains(l));
    }
}

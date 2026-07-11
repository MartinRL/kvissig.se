using System.Text.Json;
using AwesomeAssertions;
using Xunit;
using static TankTillTusen.Domain.Tests.Fixtures;

namespace TankTillTusen.Domain.Tests;

/// <summary>
/// Regression for "fel poäng #3": the client posts camelCase JSON (PuzzleScreen.razor), and a
/// case-sensitive default Deserialize bound NOTHING — Steps=null, AnswerIndex=0 — which the web
/// Parse turned into Solution([],0) = "lock Numbers[0]" for every player (all scored 100). This
/// locks the contract: the actual client payload MUST bind under JsonSerializerOptions.Web and
/// replay to the intended value. DTO shape is a local copy of TankEndpoints' private DTOs.
/// </summary>
public class TankSolutionParseTests
{
    private sealed record SolutionDto(IReadOnlyList<StepDto>? Steps, int AnswerIndex);
    private sealed record StepDto(int LeftIndex, int Op, int RightIndex);

    // Payload exactly as PuzzleScreen.razor posts it: JSON.stringify({ steps, answerIndex })
    private const string ClientJson = """{"steps":[{"leftIndex":0,"op":0,"rightIndex":1}],"answerIndex":2}""";

    [Fact]
    public void client_camelcase_payload_binds_and_replays_under_web_options()
    {
        var dto = JsonSerializer.Deserialize<SolutionDto>(ClientJson, JsonSerializerOptions.Web)!;

        dto.Steps.Should().NotBeNullOrEmpty();
        dto.AnswerIndex.Should().Be(2);

        var solution = new Solution(
            dto.Steps!.Select(s => new Step(s.LeftIndex, (Operator)s.Op, s.RightIndex)).ToList(),
            dto.AnswerIndex);

        SolutionValidator.Validate(Puzzle0, solution).Should().Be(20); // 10+10 on puzzle0
    }

    [Fact]
    public void default_options_do_not_bind_camelcase_which_is_the_bug()
    {
        // Documents WHY Web options are mandatory: default = case-sensitive PascalCase.
        JsonSerializer.Deserialize<SolutionDto>(ClientJson)!.Steps.Should().BeNull();
    }
}

using AwesomeAssertions;
using MerEllerMindre.Domain;
using MerEllerMindre.Web.Presentation;
using Xunit;

namespace MerEllerMindre.Web.Tests;

public class GameSurfacesLogoTests
{
    private static GameState Started(string packId) => new()
    {
        QuestionPackId = packId,
        Phase = GamePhase.Started,
        CurrentQuestionIndex = 0,
        JoinCode = Guid.NewGuid(),
        Questions = [new QuestionRound { Card = new Question("q", "Volvo", "Ericsson", 473m, 263m, "kr", "d") }],
    };

    [Fact]
    public void Question_LogoPack_ResolvesLogoUrls()
    {
        var vm = GameSurfaces.Question(Started("loggor-mini-1"), "tok", QuestionStage.Direction, name => $"/logos/{name}.png");

        vm.LogoA.Should().Be("/logos/Volvo.png");
        vm.LogoB.Should().Be("/logos/Ericsson.png");
    }

    [Fact]
    public void Question_TextPack_LeavesLogosNull()
    {
        var vm = GameSurfaces.Question(Started("mer-eller-mindre"), "tok", QuestionStage.Direction, _ => "/logos/x.png");

        vm.LogoA.Should().BeNull();
        vm.LogoB.Should().BeNull();
    }

    [Fact]
    public void DirectionResults_CarriesItemNames_SoTheScreenCanRevealThemNextToTheLogo()
    {
        var player = Guid.NewGuid();
        var state = Started("loggor-mini-1") with
        {
            HostPlayerId = player,
            Players = [new Player(player, "Martin", IsHost: true)],
            Questions =
            [
                new QuestionRound
                {
                    Card = new Question("q", "Volvo", "Ericsson", 473m, 263m, "kr", "d"),
                    Directions = new Dictionary<Guid, Direction> { [player] = Direction.Mer },
                    CorrectDirection = Direction.Mer,
                    DirectionScores = new Dictionary<Guid, int> { [player] = -10 },
                }
            ],
        };

        var vm = GameSurfaces.DirectionResults(state, player, name => $"/logos/{name}.png");

        // Mer = Volvo (larger value); the names must be present for the logo+name reveal.
        vm.MerItem.Should().Be("Volvo");
        vm.MindreItem.Should().Be("Ericsson");
    }
}

using AwesomeAssertions;
using MerEllerMindre.Domain;
using MerEllerMindre.Web.Presentation;
using Xunit;

namespace MerEllerMindre.Web.Tests;

public class GameScreensLogoTests
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
        var vm = GameScreens.Question(Started("loggor-blandat-1"), "tok", QuestionStage.Direction, name => $"/logos/{name}.png");

        vm.LogoA.Should().Be("/logos/Volvo.png");
        vm.LogoB.Should().Be("/logos/Ericsson.png");
    }

    [Fact]
    public void Question_TextPack_LeavesLogosNull()
    {
        var vm = GameScreens.Question(Started("mer-eller-mindre"), "tok", QuestionStage.Direction, _ => "/logos/x.png");

        vm.LogoA.Should().BeNull();
        vm.LogoB.Should().BeNull();
    }
}

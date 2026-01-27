using FluentAssertions;

namespace MerEllerMindre.Domain.Tests;

/// <summary>
/// Tests derived from ?Test? blocks in specs/game-flows.em
/// </summary>
public class DeciderTests
{
    private readonly GameContext _context = GameContext.Default;

    /// <summary>
    /// ?GameCanBeCreated?
    ///   [CreateGame]
    ///     hostName:Martin
    ///     questionPackId:pack-1
    ///   &lt;Game:GameCreated&gt;
    ///     hostPlayerId:player-1
    /// </summary>
    [Fact]
    public void GameCanBeCreated()
    {
        // Given: no prior events (initial state)
        var state = GameState.Initial;
        
        // When: CreateGame command
        var command = new CreateGame(HostName: "Martin", QuestionPackId: "pack-1");
        var result = Decider.Decide(state, command, _context);
        
        // Then: GameCreated event
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().BeOfType<GameCreated>()
            .Which.QuestionPackId.Should().Be("pack-1");
    }

    /// <summary>
    /// ?PlayerCanJoinLobby?
    ///   &lt;Game:GameCreated&gt;
    ///     gameId:game-1
    ///     joinCode:ABC123
    ///   [JoinGame]
    ///     joinCode:ABC123
    ///     playerName:Anna
    ///   &lt;Game:PlayerJoined&gt;
    ///     gameId:game-1
    ///     playerName:Anna
    /// </summary>
    [Fact]
    public void PlayerCanJoinLobby()
    {
        // Given: game created
        var events = new GameEvent[]
        {
            new GameCreated("game-1", "host-1", "ABC123", "pack-1")
        };
        var state = Decider.Fold(events);
        
        // When: JoinGame command
        var command = new JoinGame(JoinCode: "ABC123", PlayerName: "Anna");
        var result = Decider.Decide(state, command, _context);
        
        // Then: PlayerJoined event
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle()
            .Which.Should().BeOfType<PlayerJoined>()
            .Which.PlayerName.Should().Be("Anna");
    }

    /// <summary>
    /// ?CannotJoinNonexistentGame?
    ///   [JoinGame]
    ///     joinCode:INVALID
    ///     playerName:Erik
    ///   !GameNotFound!
    ///     joinCode:INVALID
    /// </summary>
    [Fact]
    public void CannotJoinNonexistentGame()
    {
        // Given: no game exists
        var state = GameState.Initial;
        
        // When: JoinGame command with invalid code
        var command = new JoinGame(JoinCode: "INVALID", PlayerName: "Erik");
        var result = Decider.Decide(state, command, _context);
        
        // Then: GameNotFound error
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<GameNotFound>()
            .Which.JoinCode.Should().Be("INVALID");
    }

    /// <summary>
    /// ?CannotJoinStartedGame?
    ///   &lt;Game:GameCreated&gt;
    ///     gameId:game-1
    ///     joinCode:ABC123
    ///   &lt;Game:GameStarted&gt;
    ///     gameId:game-1
    ///   [JoinGame]
    ///     joinCode:ABC123
    ///     playerName:Erik
    ///   !GameAlreadyStarted!
    ///     gameId:game-1
    /// </summary>
    [Fact]
    public void CannotJoinStartedGame()
    {
        // Given: game created and started
        var events = new GameEvent[]
        {
            new GameCreated("game-1", "host-1", "ABC123", "pack-1"),
            new PlayerJoined("game-1", "player-2", "Anna"),
            new GameStarted("game-1", FirstQuestionIndex: 0)
        };
        var state = Decider.Fold(events);
        
        // When: JoinGame command
        var command = new JoinGame(JoinCode: "ABC123", PlayerName: "Erik");
        var result = Decider.Decide(state, command, _context);
        
        // Then: GameAlreadyStarted error
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<GameAlreadyStarted>()
            .Which.GameId.Should().Be("game-1");
    }

    /// <summary>
    /// ?GuessOutOfRangeRejected?
    ///   &lt;Game:GameCreated&gt;
    ///     gameId:game-1
    ///   &lt;Game:GameStarted&gt;
    ///     gameId:game-1
    ///   [SubmitGuess]
    ///     gameId:game-1
    ///     playerId:player-1
    ///     guess:150
    ///   !GuessOutOfRange!
    ///     guess:150
    ///     min:0
    ///     max:100
    /// </summary>
    [Fact]
    public void GuessOutOfRangeRejected()
    {
        // Given: game in progress
        var events = new GameEvent[]
        {
            new GameCreated("game-1", "host-1", "ABC123", "pack-1"),
            new PlayerJoined("game-1", "player-2", "Anna"),
            new GameStarted("game-1", FirstQuestionIndex: 0)
        };
        var state = Decider.Fold(events);
        
        // When: SubmitGuess with out of range value
        var command = new SubmitGuess(GameId: "game-1", PlayerId: "host-1", Guess: 150);
        var result = Decider.Decide(state, command, _context);
        
        // Then: GuessOutOfRange error
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().BeOfType<GuessOutOfRange>()
            .Which.Should().Match<GuessOutOfRange>(e => 
                e.Guess == 150 && e.Min == 0 && e.Max == 100);
    }
}

using MerEllerMindre.Domain;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Imperative-shell command side: loads state via the <see cref="GameRepository"/>, runs the
/// pure <see cref="Decider.Decide"/>, and persists the resulting events. Also drives the
/// System processors ("gears"): when every player has guessed the current question the
/// scoring gear fires; once a question is scored the progression gear asks the next question
/// or ends the game. The game is co-located (one room) so the gears fire on a STATE condition
/// (AllGuessesIn / scored), never a timer.
///
/// The <see cref="GameContext"/> wires the Decider's external dependencies — real Guid/clock
/// plus the pack resolver from the file-system catalog.
/// </summary>
public sealed class GameApplicationService
{
    private readonly GameRepository _repo;
    private readonly GameContext _context;

    public GameApplicationService(GameRepository repo, FileSystemQuestionPackCatalog catalog)
    {
        _repo = repo;
        _context = new GameContext(Guid.NewGuid, () => DateTimeOffset.UtcNow, catalog.Find);
    }

    public GameState Load(Guid gameId) => _repo.Load(gameId);

    public Guid? ResolveJoinCode(Guid joinCode) => _repo.ResolveJoinCode(joinCode);

    /// <summary>Open a new lobby. The Decider mints gameId/joinCode/hostPlayerId itself, so
    /// the stream id comes off the produced LobbyOpened event.</summary>
    public Result<GameEvent[]> Open(OpenLobby command)
    {
        var result = Decider.Decide(GameState.Initial, command, _context);
        if (result is Ok<GameEvent[]> ok && ok.Value is [LobbyOpened opened, ..])
            _repo.Append(opened.GameId, ok.Value);
        return result;
    }

    /// <summary>Execute a command against an existing game stream.</summary>
    public Result<GameEvent[]> Execute(Guid gameId, GameCommand command)
    {
        var state = _repo.Load(gameId);
        var result = Decider.Decide(state, command, _context);
        if (result is Ok<GameEvent[]> ok && ok.Value.Length > 0)
            _repo.Append(gameId, ok.Value);
        return result;
    }

    /// <summary>Scoring gear: fire ScoreQuestion the moment the last guess lands.</summary>
    public void RunScoreGear(Guid gameId)
    {
        var state = _repo.Load(gameId);
        if (state.Phase != GamePhase.Started)
            return;

        var i = state.CurrentQuestionIndex;
        if (state.AllGuessesIn(i) && !state.Questions[i].Scored)
            Execute(gameId, new ScoreQuestion(gameId, i));
    }

    /// <summary>Progression gear: once the current question is scored, ask the next question
    /// or end the game. Host-paced (fired from the Round-results "next" action) so everyone
    /// reads the result together before advancing — co-located, no timer.</summary>
    public void RunProgressionGear(Guid gameId)
    {
        var state = _repo.Load(gameId);
        if (state.Phase != GamePhase.Started || !state.CurrentQuestionScored)
            return;

        if (state.HasNextQuestion)
            Execute(gameId, new AskNextQuestion(gameId));
        else
            Execute(gameId, new EndGame(gameId));
    }
}

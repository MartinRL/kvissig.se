namespace Blindbudet.Domain;

/// <summary>
/// Events are facts that happened — immutable history. Native C# 15 union (closed,
/// exhaustive switches, no default arm). Each event carries its own explicit timestamp
/// (*At) where the spec has one. Derived from event (`e:`) elements in
/// specs/blindbudet-event-model.yaml. Wire events stay flat with an explicit playerId;
/// only the folded State keys per-player data by playerId.
/// </summary>
public record AuctionOpened(
    Guid GameId,
    Guid HostPlayerId,
    string HostName,
    Guid JoinCode,
    string PackId,
    IReadOnlyList<Lot> Lots,
    DateTimeOffset OpenedAt
);

public record PlayerJoined(
    Guid GameId,
    Guid PlayerId,
    string PlayerName,
    DateTimeOffset JoinedAt
);

public record AuctionStarted(
    Guid GameId,
    int FirstLotIndex,
    DateTimeOffset StartedAt
);

public record BidPlaced(
    Guid GameId,
    Guid PlayerId,
    int LotIndex,
    decimal Amount,
    DateTimeOffset BidAt
);

public record LotRevealed(
    Guid GameId,
    int LotIndex,
    decimal TrueWorth,
    IReadOnlyList<Guid> WinnerIds,
    decimal PricePaid
);

public record RoundScored(
    Guid GameId,
    int LotIndex,
    Guid PlayerId,
    int Profit,
    int TotalScore
);

public record NextLotStarted(
    Guid GameId,
    int LotIndex
);

public record AuctionEnded(
    Guid GameId,
    IReadOnlyList<ScoreboardEntry> FinalScoreboard,
    IReadOnlyList<Guid> WinnerIds,
    DateTimeOffset EndedAt
);

public union AuctionEvent(
    AuctionOpened,
    PlayerJoined,
    AuctionStarted,
    BidPlaced,
    LotRevealed,
    RoundScored,
    NextLotStarted,
    AuctionEnded
);

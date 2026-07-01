namespace Blindbudet.Domain;

/// <summary>
/// Commands represent player/system intentions for a sealed-bid auction. Modeled as a
/// native C# 15 union (closed, exhaustive switches, no default arm). Derived from command
/// (`c:`) elements in specs/blindbudet-event-model.yaml. Sister to MEM's GameCommand — a
/// separate Decider, never touching MEM's.
/// </summary>
public record OpenAuction(
    string HostName,
    string PackId
);

public record JoinAuction(
    Guid JoinCode,
    string PlayerName
);

public record StartAuction(
    Guid GameId
);

public record PlaceBid(
    Guid GameId,
    Guid PlayerId,
    int LotIndex,
    decimal Amount
);

public record RevealLot(
    Guid GameId,
    int LotIndex
);

public record AskNextLot(
    Guid GameId
);

public record EndAuction(
    Guid GameId
);

public union AuctionCommand(
    OpenAuction,
    JoinAuction,
    StartAuction,
    PlaceBid,
    RevealLot,
    AskNextLot,
    EndAuction
);

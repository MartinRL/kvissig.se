namespace Blindbudet.Domain;

/// <summary>
/// Errors are why a command was rejected — values on the Result failure track (ROP, never
/// thrown; see ADR 006). Native C# 15 union of parameterless marker records (the spec
/// asserts no props). Derived from exception (`x:`) elements in
/// specs/blindbudet-event-model.yaml.
/// </summary>
public record AuctionPackNotFound;

public record AuctionNotFound;

public record AuctionAlreadyStarted;

public record NameAlreadyTaken;

public record NotEnoughPlayers;

public record BidNegative;

public record AlreadyBid;

public record LotAlreadyResolved;

public record NotAllBidsIn;

public union AuctionError(
    AuctionPackNotFound,
    AuctionNotFound,
    AuctionAlreadyStarted,
    NameAlreadyTaken,
    NotEnoughPlayers,
    BidNegative,
    AlreadyBid,
    LotAlreadyResolved,
    NotAllBidsIn
);

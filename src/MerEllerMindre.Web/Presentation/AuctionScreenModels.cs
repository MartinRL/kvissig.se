namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// View-models for the hand-written Blindbudet catalog page (SEO residue, ADR 019).
/// All in-game screens render through the xm runtime interpreter — see AuctionSurfaces.
/// </summary>
public sealed record AuctionPackVm(string PackId, string Name, int LotCount);

public sealed record AuctionCatalogVm(IReadOnlyList<AuctionPackVm> Packs);

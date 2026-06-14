using QRCoder;

namespace MerEllerMindre.Web.Presentation;

/// <summary>
/// Pure server-side QR rendering: a string payload (the absolute join URL) → inline SVG markup.
/// Uses QRCoder's <see cref="SvgQRCode"/> renderer (no System.Drawing → Linux/fly-safe). ViewBox
/// sizing lets the SVG scale into the existing fixed-size <c>.qr</c> box. No IO, no state, so it
/// stays inside the functional core's spirit and is trivially assertable in tests.
/// </summary>
public static class QrCode
{
    public static string SvgFor(string payload)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(
            10,
            "#2b2118",
            "#ffffff",
            drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}

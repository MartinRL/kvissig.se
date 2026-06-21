#:package Svg.Skia@*

// One-shot icon rasterizer. Run once from repo root after editing icon.svg:
//   dotnet run tools/icons.cs
// Reads wwwroot/icon.svg, writes the raster files the manifest + <head> reference.
// ponytail: build-time generator, NOT a Web runtime dep — PNG/ICO encoding can't be
// hand-written. The outputs are committed; rerun only when the SVG changes.

using SkiaSharp;
using Svg.Skia;

const string wwwroot = "src/MerEllerMindre.Web/wwwroot";
var svg = new SKSvg();
svg.Load(Path.Combine(wwwroot, "icon.svg"));

SKBitmap Render(int size)
{
    var bmp = new SKBitmap(size, size);
    using var canvas = new SKCanvas(bmp);
    canvas.Clear(SKColors.Transparent);
    var pic = svg.Picture!;
    var scale = size / pic.CullRect.Width;
    canvas.Scale(scale);
    canvas.DrawPicture(pic);
    return bmp;
}

void WritePng(string path, int size)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    using var bmp = Render(size);
    using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
    using var fs = File.Create(path);
    data.SaveTo(fs);
    Console.WriteLine($"wrote {path} ({size}x{size})");
}

WritePng(Path.Combine(wwwroot, "icons", "icon-192.png"), 192);
WritePng(Path.Combine(wwwroot, "icons", "icon-512.png"), 512);
WritePng(Path.Combine(wwwroot, "apple-touch-icon.png"), 180);

// favicon.ico: a single 32x32 PNG wrapped in a minimal ICO container (PNG-in-ICO is
// supported by all modern browsers). ponytail: hand-rolled 22-byte header beats pulling
// an ICO-encoder dependency for one file.
using (var bmp = Render(32))
using (var png = bmp.Encode(SKEncodedImageFormat.Png, 100))
{
    var pngBytes = png.ToArray();
    using var ico = File.Create(Path.Combine(wwwroot, "favicon.ico"));
    using var w = new BinaryWriter(ico);
    w.Write((short)0);          // reserved
    w.Write((short)1);          // type: icon
    w.Write((short)1);          // image count
    w.Write((byte)32);          // width
    w.Write((byte)32);          // height
    w.Write((byte)0);           // palette
    w.Write((byte)0);           // reserved
    w.Write((short)1);          // color planes
    w.Write((short)32);         // bits per pixel
    w.Write(pngBytes.Length);   // image size
    w.Write(22);                // offset to image data
    w.Write(pngBytes);
    Console.WriteLine($"wrote favicon.ico (32x32, {pngBytes.Length} bytes)");
}

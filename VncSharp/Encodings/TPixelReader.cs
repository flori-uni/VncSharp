using System.IO;

namespace VncSharp.Encodings
{
    public class TPixelReader : PixelReader
    {
        public TPixelReader(BinaryReader binaryReader, Framebuffer framebuffer) : base(binaryReader, framebuffer)
        {
        }

        public override int ReadPixel()
        {
            var b = reader.ReadBytes(3);
            return ToGdiPlusOrder(b[0], b[1], b[2]);
        }
    }
}
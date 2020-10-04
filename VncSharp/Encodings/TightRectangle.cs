using System;
using System.Drawing;
using System.IO;

namespace VncSharp.Encodings
{
    public class TightRectangle : EncodedRectangle
    {
        public TightRectangle(RfbProtocol rfbProtocol, Framebuffer framebuffer, Rectangle rectangle) : base(rfbProtocol,
            framebuffer, rectangle, RfbProtocol.TIGHT_ENCODING)
        {
        }

        public override void Decode()
        {
            var decoder = TightDecoder.Instance;
            var ctl = rfb.ReadByte();
            for (var i = 0; i < 4; i++)
                if (((ctl >> i) & 1) != 0)
                {
                    //Reset?
                }

            ctl = (byte) (ctl >> 4);
            if (ctl == 0x08)
            {
                ParseFillRectangle();
            }
            else if (ctl == 0x09)
            {
                ParseJpegRectangle();
            }
            else if ((ctl & 0x08) == 0)
            {
                var streamId = ctl & 0x3;
                byte filter = 0;
                if ((ctl & 0x4) != 0) filter = rfb.ReadByte();

                switch (filter)
                {
                    case 0:
                        ParseCopyFilter(streamId, decoder);
                        break;
                    case 1:
                        ParsePaletteFilter(streamId, decoder);
                        break;
                    case 2:
                        ParseGradientFilter(streamId, decoder);
                        break;
                }
            }
        }

        private void ParsePaletteFilter(int streamId, TightDecoder decoder)
        {
            var numColors = rfb.ReadByte() + 1;
            var palette = Array.ConvertAll(rfb.ReadBytes(numColors * 3), Convert.ToInt32);
            var size = numColors == 2
                ? rectangle.Height * ((rectangle.Width + 7) / 8)
                : rectangle.Width * rectangle.Height;
            if (size == 0)
                return;
            var data = ReadBasicData(size, streamId, decoder);
            if (numColors == 2)
                MonoRect(data, palette);
            else
                PaletteRect(data, palette);
        }

        private byte[] ReadBasicData(int size,int streamId, TightDecoder decoder)
        {
            byte[] data;
            if (size < 12)
            {
                data = rfb.ReadBytes(size);
            }
            else
            {
                data = ReadCompactData();
                decoder.Streams[streamId].SetData(data);
                data = decoder.Streams[streamId].Inflate(size);
            }

            return data;
        }

        private void PaletteRect(byte[] data, int[] palette)
        {
            var tile = new int[rectangle.Width * rectangle.Height];
            for (var i = 0; i < rectangle.Width * rectangle.Height; i++)
            {
                var sp = data[i] * 3;
                tile[i] = (palette[sp + 2] & 0xFF) | (palette[sp + 1] << 8) | (palette[sp] << 16) | (0xFF << 24);
            }

            FillRectangle(rectangle, tile);
        }

        private void MonoRect(byte[] data, int[] palette)
        {
            var tile = new int[rectangle.Width * rectangle.Height];
            var w = (rectangle.Width + 7) / 8;
            var w1 = rectangle.Width / 8;
            for (var y = 0; y < rectangle.Height; y++)
            {
                int dp, sp, x;
                for (x = 0; x < w1; x++)
                for (var b = 7; b >= 0; b--)
                {
                    dp = y * rectangle.Width + x * 8 + 7 - b;
                    sp = ((data[y * w + x] >> b) & 1) * 3;
                    tile[dp] = (palette[sp + 2] & 0xFF) | (palette[sp + 1] << 8) | (palette[sp] << 16) | (0xFF << 24);
                }

                for (var b = 7; b >= 8 - rectangle.Width % 8; b--)
                {
                    dp = y * rectangle.Width + x * 8 + 7 - b;
                    sp = ((data[y * w + x] >> b) & 1) * 3;
                    tile[dp] = (palette[sp + 2] & 0xFF) | (palette[sp + 1] << 8) | (palette[sp] << 16) | (0xFF << 24);
                }
            }

            FillRectangle(rectangle, tile);
        }

        private void ParseGradientFilter(int streamId, TightDecoder decoder)
        {
        }

        private void ParseCopyFilter(int streamId, TightDecoder decoder)
        {
            var size = rectangle.Width * rectangle.Height * 3;
            if (size == 0)
                return;
            var data = ReadBasicData(size, streamId, decoder);

            var tile = new int[rectangle.Width * rectangle.Height];
            var j = 0;
            for (var y = 0; y < rectangle.Height; y++)
            for (var x = 0; x < rectangle.Width; x++)
            {
                tile[y * rectangle.Width + x] =
                    (data[j + 2] & 0xFF) | (data[j + 1] << 8) | (data[j] << 16) | (0xFF << 24);
                j += 3;
            }

            FillRectangle(rectangle, tile);
        }

        private void ParseJpegRectangle()
        {
            var data = ReadCompactData();
            using (var inStream = new MemoryStream(data))
            {
                var imageStream = Image.FromStream(inStream);
                var bitmap = new Bitmap(imageStream);
                for (var y = 0; y < rectangle.Height; y++)
                for (var x = 0; x < rectangle.Width; x++)
                {
                    //Todo!
                }
            }
        }

        private void ParseFillRectangle()
        {
            FillRectangle(rectangle, preader.ReadPixel());
        }

        private byte[] ReadCompactData()
        {
            var data = rfb.ReadByte();
            var length = data & 0x7f;
            if ((data & 0x80) != 0)
            {
                data = rfb.ReadByte();
                length |= (data & 0x7f) << 7;
                if ((data & 0x80) != 0)
                {
                    data = rfb.ReadByte();
                    length |= data << 14;
                }
            }

            return rfb.ReadBytes(length);
        }
    }
}
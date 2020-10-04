using Ionic.Zlib;

namespace VncSharp.Encodings
{
    public class Inflator
    {
        private int chunkSize = 1024 * 10 * 10;

        private byte[] output;

        private readonly ZlibCodec stream;
        
        public Inflator()
        {
            output = new byte[chunkSize];
            stream = new ZlibCodec(CompressionMode.Decompress);
            stream.InitializeInflate();

            output = new byte[chunkSize];
        }

        public void SetData(byte[] data)
        {
            stream.InputBuffer = data;
            stream.AvailableBytesIn = data.Length;
            stream.NextIn = 0;
        }

        public byte[] Inflate(int expected)
        {
            if (expected > chunkSize)
            {
                chunkSize = expected;
                output = new byte[chunkSize];
            }

            stream.OutputBuffer = output;
            stream.NextOut = 0;
            stream.AvailableBytesOut = expected;
            stream.Inflate(FlushType.Sync);
            return output;
        }
    }
}
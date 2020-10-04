namespace VncSharp.Encodings
{
    public class TightDecoder
    {
        public static readonly TightDecoder Instance = new TightDecoder();

        internal Inflator[] Streams { get; }

        private TightDecoder()
        {
            Streams = new Inflator[4];
            for (var i = 0; i < 4; i++) Streams[i] = new Inflator();
        }
    }
}
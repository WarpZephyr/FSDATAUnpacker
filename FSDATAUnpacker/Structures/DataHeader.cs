namespace Structures
{
    public record DataHeader(long Offset, long Length)
    {
        public byte[] GetBytes(Stream stream, long baseAddress)
        {
            if ((baseAddress + Offset + Length) > stream.Length)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }

            long currentPosition = stream.Position;
            stream.Position = baseAddress + Offset;
            var bytes = new byte[Length];
            stream.Read(bytes);
            stream.Position = currentPosition;
            return bytes;
        }

        public byte[] GetBytes(Stream stream)
        {
            if ((Offset + Length) > stream.Length)
            {
                throw new EndOfStreamException("Cannot read beyond the end of the stream.");
            }

            if (Length < 1)
            {
                return [];
            }

            long currentPosition = stream.Position;
            stream.Position = Offset;
            var bytes = new byte[Length];
            stream.Read(bytes);
            stream.Position = currentPosition;
            return bytes;
        }
    }
}

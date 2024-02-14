namespace Handlers
{
    internal static class StreamHandler
    {
        internal static void Fill(this Stream stream, long count)
        {
            if (count < 1)
            {
                return;
            }

            if (count <= short.MaxValue)
            {
                stream.Write(new byte[count]);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                stream.WriteByte(0);
            }
        }

        internal static void Pad(this Stream stream, int align)
        {
            while (stream.Position % align > 0)
            {
                stream.WriteByte(0);
            }
        }
    }
}

namespace Helpers
{
    internal static class MathHelper
    {
        internal static long Align(long value, long alignment)
        {
            var remainder = value % alignment;
            if (remainder > 0)
            {
                return alignment - remainder;
            }
            return value;
        }
    }
}

namespace Theraot.Core
{
    public static class NumericHelper
    {
        /// <summary>
        /// Calculates the next the power of 2 after the specified number.
        /// </summary>
        /// <param name="number">The number.</param>
        [System.Diagnostics.DebuggerNonUserCode]
        internal static int NextPowerOf2(int number)
        {
            int result = 1;
            while (true)
            {
                if (number <= result)
                {
                    return result;
                }
                result = result << 1;
            }
        }
    }
}

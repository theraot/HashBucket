using System.Threading;

namespace Theraot.Threading
{
    [System.Diagnostics.DebuggerNonUserCode]
    public static class ThreadingHelper
    {
        /// <summary>
        /// Reads the value of a field. The value is the latest written by any processor in a computer, regardless of the number of processors or the state of processor cache.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="address">The field to be read.</param>
        /// <returns>The latest value written to the field by any processor.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Justification = "By Design")]
        public static T VolatileRead<T>(ref T address)
            where T : class
        {
            T copy = address;
            Thread.MemoryBarrier();
            return copy;
        }

        /// <summary>
        /// Writes a value to a field immediately, so that the value is visible to all processors in the computer.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="address">The field to which the value is to be written.</param>
        /// <param name="value">The value to be written.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", Justification = "By Design")]
        public static void VolatileWrite<T>(ref T address, T value)
            where T : class
        {
            Thread.MemoryBarrier();
            address = value;
        }
    }
}
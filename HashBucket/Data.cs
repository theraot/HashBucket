using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Theraot.Threading
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Data
    {
        [FieldOffset(0)]
        internal long UsedLocked;
        [FieldOffset(0)]
        internal int Locked;
        [FieldOffset(4)]
        internal int Used;
    }
}

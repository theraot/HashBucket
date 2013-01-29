using System.Collections.Generic;
using System.Threading;

using Theraot.Threading;

namespace Threaot.Threading
{
    /// <summary>
    /// Represent a thread-safe lock-free deque.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    public class Deque<T>
    {
        private const int INT_DefaultCapacity = 64;
        private const int INT_SpinWaitHint = 80;

        private int _copyingThreads;
        private int _count;
        private FixedSizeDeque<T> _entriesNew;
        private FixedSizeDeque<T> _entriesOld;
        private volatile int _revision;
        private int _status;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}" /> class.
        /// </summary>
        public Deque()
            : this(INT_DefaultCapacity)
        {
            //Empty
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}" /> class.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public Deque(int initialCapacity)
        {
            _entriesOld = null;
            _entriesNew = new FixedSizeDeque<T>(initialCapacity);
        }

        /// <summary>
        /// Gets the capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _entriesNew.Capacity;
            }
        }

        /// <summary>
        /// Gets the number of keys actually contained.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// Gets the items contained in this object.
        /// </summary>
        public IList<T> Values
        {
            get
            {
                return _entriesNew.Values;
            }
        }

        /// <summary>
        /// Attempts to Adds the specified item at the back.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was added; otherwise, <c>false</c>.
        /// </returns>
        public void AddBack(T item)
        {
            bool result = false;
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        if (entries.AddBack(item))
                        {
                            result = true;
                        }
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            if (result)
                            {
                                Interlocked.Increment(ref _count);
                                done = true;
                            }
                            else
                            {
                                var oldStatus = Interlocked.CompareExchange(ref _status, 1, 0);
                                if (oldStatus == 0)
                                {
                                    _revision++;
                                }
                            }
                        }
                    }
                    if (done)
                    {
                        return;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Attempts to Adds the specified item at the front.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was added; otherwise, <c>false</c>.
        /// </returns>
        public void AddFront(T item)
        {
            bool result = false;
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        if (entries.AddFront(item))
                        {
                            result = true;
                        }
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            if (result)
                            {
                                Interlocked.Increment(ref _count);
                                done = true;
                            }
                            else
                            {
                                var oldStatus = Interlocked.CompareExchange(ref _status, 1, 0);
                                if (oldStatus == 0)
                                {
                                    _revision++;
                                }
                            }
                        }
                    }
                    if (done)
                    {
                        return;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Removes all the elements.
        /// </summary>
        public void Clear()
        {
            _entriesOld = null;
            _entriesNew = new FixedSizeDeque<T>(INT_DefaultCapacity);
            _revision++;
        }

        /// <summary>
        /// Gets the an <see cref="IEnumerable{T}" /> that allows to iterate over the contained items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _entriesNew.GetEnumerator();
        }

        /// <summary>
        /// Returns the next item to be taken from the back without removing it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No more items to be taken.</exception>
        public T PeekBack(T item)
        {
            T result = default(T);
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        result = entries.PeekBack();
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            done = true;
                        }
                    }
                    if (done)
                    {
                        return result;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Returns the next item to be taken from the front without removing it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No more items to be taken.</exception>
        public T PeekFront(T item)
        {
            T result = default(T);
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        result = entries.PeekFront();
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            done = true;
                        }
                    }
                    if (done)
                    {
                        return result;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Tries the retrieve the item at an specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the value was retrieved; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Although items are ordered, they are not guaranteed to start at index 0.
        /// </remarks>
        public bool TryGet(int index, out T item)
        {
            item = default(T);
            bool result = false;
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        T tmpItem;
                        if (entries.TryGet(index, out tmpItem))
                        {
                            item = tmpItem;
                            result = true;
                        }
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            done = true;
                        }
                    }
                    if (done)
                    {
                        return result;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve and remove the next item from the back.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was taken; otherwise, <c>false</c>.
        /// </returns>
        public bool TryTakeBack(out T item)
        {
            item = default(T);
            bool result = false;
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        T tmpItem;
                        if (entries.TryTakeBack(out tmpItem))
                        {
                            item = tmpItem;
                            result = true;
                        }
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            done = true;
                        }
                    }
                    if (done)
                    {
                        return result;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve and remove the next item from the front.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was taken; otherwise, <c>false</c>.
        /// </returns>
        public bool TryTakeFront(out T item)
        {
            item = default(T);
            bool result = false;
            while (true)
            {
                bool done = false;
                int revision = _revision;
                if (IsOperationSafe() == 0)
                {
                    var entries = ThreadingHelper.VolatileRead(ref _entriesNew);
                    try
                    {
                        T tmpItem;
                        if (entries.TryTakeFront(out tmpItem))
                        {
                            item = tmpItem;
                            result = true;
                        }
                    }
                    finally
                    {
                        var isOperationSafe = IsOperationSafe(entries, revision);
                        if (isOperationSafe == 0)
                        {
                            done = true;
                        }
                    }
                    if (done)
                    {
                        return result;
                    }
                }
                else
                {
                    CooperativeGrow();
                }
            }
        }

        private void CooperativeGrow()
        {
            int status = 0;
            do
            {
                status = Thread.VolatileRead(ref _status);
                int oldStatus;
                switch (status)
                {
                    case 1:
                        var priority = Thread.CurrentThread.Priority;
                        oldStatus = Interlocked.CompareExchange(ref _status, 2, 1);
                        if (oldStatus == 1)
                        {
                            try
                            {
                                Thread.CurrentThread.Priority = ThreadPriority.Highest;
                                var newCapacity = _entriesNew.Capacity * 2;
                                _entriesOld = Interlocked.Exchange(ref _entriesNew, new FixedSizeDeque<T>(newCapacity));
                                oldStatus = Interlocked.CompareExchange(ref _status, 3, 2);
                            }
                            finally
                            {
                                Thread.CurrentThread.Priority = priority;
                                _revision++;
                            }
                        }
                        break;

                    case 2:
                        Thread.SpinWait(INT_SpinWaitHint);
                        break;

                    case 3:
                        var old = _entriesOld;
                        if (old != null)
                        {
                            _revision++;
                            Interlocked.Increment(ref _copyingThreads);
                            T item;
                            while (old.TryTakeFront(out item))
                            {
                                AddFront(item);
                            }
                            _revision++;
                            oldStatus = Interlocked.CompareExchange(ref _status, 4, 3);
                            Interlocked.Decrement(ref _copyingThreads);
                        }
                        break;

                    case 4:
                        oldStatus = Interlocked.CompareExchange(ref _status, 2, 4);
                        if (oldStatus == 4)
                        {
                            _revision++;
                            Interlocked.Exchange(ref _entriesOld, null);
                            oldStatus = Interlocked.CompareExchange(ref _status, 0, 2);
                        }
                        break;

                    default:
                        break;
                }
            }
            while (status != 0);
        }

        private int IsOperationSafe(FixedSizeDeque<T> entries, int revision)
        {
            int result = 5;
            bool check = _revision != revision;
            if (check)
            {
                result = 4;
            }
            else
            {
                var newEntries = Interlocked.CompareExchange(ref _entriesNew, null, null);
                if (entries != newEntries)
                {
                    result = 3;
                }
                else
                {
                    var newStatus = Interlocked.CompareExchange(ref _status, 0, 0);
                    if (newStatus != 0)
                    {
                        result = 2;
                    }
                    else
                    {
                        if (Thread.VolatileRead(ref _copyingThreads) > 0)
                        {
                            _revision++;
                            result = 1;
                        }
                        else
                        {
                            result = 0;
                        }
                    }
                }
            }
            return result;
        }

        private int IsOperationSafe()
        {
            var newStatus = Interlocked.CompareExchange(ref _status, 0, 0);
            if (newStatus != 0)
            {
                return 2;
            }
            else
            {
                if (Thread.VolatileRead(ref _copyingThreads) > 0)
                {
                    _revision++;
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
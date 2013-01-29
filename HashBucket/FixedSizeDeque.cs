using System.Collections.Generic;
using System.Threading;

namespace Theraot.Threading
{
    /// <summary>
    /// Represent a fixed size thread-safe wait-free deque.
    /// </summary>
    public class FixedSizeDeque<T> : IEnumerable<T>
    {
        private readonly int _capacity;

        private Bucket<T> _bucket;
        private int _indexBack;
        private int _indexFront;
        private int _preCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="FixedSizeDeque{T}" /> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public FixedSizeDeque(int capacity)
        {
            _capacity = IntHelper.NextPowerOf2(capacity);
            _preCount = 0;
            _indexFront = 0;
            _indexBack = _capacity - 1;
            _bucket = new Bucket<T>(_capacity);
        }

        /// <summary>
        /// Gets the capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        /// <summary>
        /// Gets the number of items actually contained.
        /// </summary>
        public int Count
        {
            get
            {
                return _bucket.Count;
            }
        }

        /// <summary>
        /// Gets the values contained in this object.
        /// </summary>
        public IList<T> Values
        {
            get
            {
                return _bucket.Values;
            }
        }

        /// <summary>
        /// Attempts to Adds the specified item at the back.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the item was added; otherwise, <c>false</c>.
        /// </returns>
        public bool AddBack(T item)
        {
            var preCount = Interlocked.Increment(ref _preCount);
            if (preCount > _capacity)
            {
                return false;
            }
            else
            {
                var index = (Interlocked.Decrement(ref _indexBack) + 1) & (_capacity - 1);
                if (_bucket.Insert(index, item))
                {
                    return true;
                }
                else
                {
                    return false;
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
        public bool AddFront(T item)
        {
            var preCount = Interlocked.Increment(ref _preCount);
            if (preCount > _capacity)
            {
                return false;
            }
            else
            {
                var index = (Interlocked.Increment(ref _indexFront) - 1) & (_capacity - 1);
                if (_bucket.Insert(index, item))
                {
                    return true;
                }
                else
                {
                    Interlocked.Decrement(ref _preCount);
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets the an <see cref="IEnumerable{T}" /> that allows to iterate over the contained items.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _bucket.GetEnumerator();
        }

        /// <summary>
        /// Returns the next item to be taken from the back without removing it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No more items to be taken.</exception>
        public T PeekBack()
        {
            T item;
            int index = Interlocked.Add(ref _indexFront, 0);
            if (index < _capacity && index > 0 && _bucket.TryGet(index, out item))
            {
                return item;
            }
            else
            {
                throw new System.InvalidOperationException("Empty");
            }
        }

        /// <summary>
        /// Returns the next item to be taken from the front without removing it.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">No more items to be taken.</exception>
        public T PeekFront()
        {
            T item;
            int index = Interlocked.Add(ref _indexFront, 0);
            if (index < _capacity && index > 0 && _bucket.TryGet(index, out item))
            {
                return item;
            }
            else
            {
                throw new System.InvalidOperationException("Empty");
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
            return _bucket.TryGet(index, out item);
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
            var index = Interlocked.Increment(ref _indexBack) & (_capacity - 1);
            if (_bucket.RemoveAt(index, out item))
            {
                Interlocked.Decrement(ref _preCount);
                return true;
            }
            else
            {
                return false;
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
            var index = Interlocked.Decrement(ref _indexFront) & (_capacity - 1);
            if (_bucket.RemoveAt(index, out item))
            {
                Interlocked.Decrement(ref _preCount);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
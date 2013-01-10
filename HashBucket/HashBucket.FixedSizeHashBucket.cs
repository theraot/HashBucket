using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Theraot.Threading
{
    public partial class HashBucket<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        internal class FixedSizeHashBucket
        {
            private readonly int _capacity;
            private readonly IEqualityComparer<TKey> _keyComparer;

            private Bucket<KeyValuePair<TKey, TValue>> _entries;

            public FixedSizeHashBucket(int capacity, IEqualityComparer<TKey> keyComparer)
            {
                _capacity = IntHelper.NextPowerOf2(capacity);
                _entries = new Bucket<KeyValuePair<TKey, TValue>>(_capacity);
                _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            }

            public int Capacity
            {
                get
                {
                    return _capacity;
                }
            }

            public int Count
            {
                get
                {
                    return _entries.Count;
                }
            }

            public int Add(TKey key, TValue value, out bool isCollision)
            {
                return Add(key, value, 0, out isCollision);
            }

            public int Add(TKey key, TValue value, int offset, out bool isCollision)
            {
                int index = Index(key, offset);
                var entry = new KeyValuePair<TKey, TValue>(key, value);
                KeyValuePair<TKey, TValue> previous;
                if (_entries.Insert(index, entry, out previous))
                {
                    isCollision = false;
                    return index;
                }
                else
                {
                    isCollision = !_keyComparer.Equals(previous.Key, key);
                    return -1;
                }
            }

            public int ContainsKey(TKey key)
            {
                return ContainsKey(key, 0);
            }

            public int ContainsKey(TKey key, int offset)
            {
                int index = Index(key, offset);
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    if (_keyComparer.Equals(entry.Key, key))
                    {
                        return index;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }

            public IEnumerable<TKey> GetKeyEnumerable()
            {
                for (int index = 0; index < _capacity; index++)
                {
                    KeyValuePair<TKey, TValue> entry;
                    if (_entries.TryGet(index, out entry))
                    {
                        yield return entry.Key;
                    }
                }
            }

            public IEnumerable<KeyValuePair<TKey, TValue>> GetKeyValuePairEnumerable()
            {
                for (int index = 0; index < _capacity; index++)
                {
                    KeyValuePair<TKey, TValue> entry;
                    if (_entries.TryGet(index, out entry))
                    {
                        yield return entry;
                    }
                }
            }

            public IEnumerable<TValue> GetValueEnumerable()
            {
                for (int index = 0; index < _capacity; index++)
                {
                    KeyValuePair<TKey, TValue> entry;
                    if (_entries.TryGet(index, out entry))
                    {
                        yield return entry.Value;
                    }
                }
            }

            public int Index(TKey key, int offset)
            {
                var hash = _keyComparer.GetHashCode(key);
                var index = (hash + offset) & (_capacity - 1);
                return index;
            }

            public int Remove(TKey key)
            {
                return Remove(key, 0);
            }

            public int Remove(TKey key, int offset)
            {
                int index = Index(key, offset);
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    if (_keyComparer.Equals(entry.Key, key))
                    {
                        if (_entries.RemoveAt(index))
                        {
                            return index;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }

            public int Set(TKey key, TValue value, int offset, out bool isNew)
            {
                int index = Index(key, offset);
                KeyValuePair<TKey, TValue> oldEntry;
                isNew = !_entries.TryGet(index, out oldEntry);
                if ((isNew || _keyComparer.Equals(key, oldEntry.Key)) && _entries.Set(index, new KeyValuePair<TKey, TValue>(key, value), out isNew))
                {
                    return index;
                }
                else
                {
                    return -1;
                }
            }

            public int Set(TKey key, TValue value, out bool isNew)
            {
                return Set(key, value, 0, out isNew);
            }

            public bool TryGet(int index, out TKey key, out TValue value)
            {
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    key = entry.Key;
                    value = entry.Value;
                    return true;
                }
                else
                {
                    key = default(TKey);
                    value = default(TValue);
                    return false;
                }
            }

            public int TryGetValue(TKey key, out TValue value)
            {
                return TryGetValue(key, 0, out value);
            }

            public int TryGetValue(TKey key, int offset, out TValue value)
            {
                int index = Index(key, offset);
                KeyValuePair<TKey, TValue> entry;
                if (_entries.TryGet(index, out entry))
                {
                    if (_keyComparer.Equals(entry.Key, key))
                    {
                        value = entry.Value;
                        return index;
                    }
                    else
                    {
                        value = default(TValue);
                        return -1;
                    }
                }
                else
                {
                    value = default(TValue);
                    return -1;
                }
            }
        }

        // This class will not shrink, the reason for this is that shrinking may fail, supporting it may require to add locks. [Not solved problem]
        // Enumerating this class gives no guaranties:
        //  Items may be added or removed during enumeration without causing an exception.
        //  A version mechanism is not in place.
        //  This can be added by a wrapper.
    }
}
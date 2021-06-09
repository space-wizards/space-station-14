using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace Content.Shared.Collections
{
    // It's a Remie Queue now.

    /// <summary>
    ///     Simple helper struct for "iterate collection and have a queue of things to remove when you're done",
    ///     to avoid concurrent iteration/modification.
    /// </summary>
    public struct RemQueue<T>
    {
        public List<T>? List;

        public void Add(T t)
        {
            List ??= new();
            List.Add(t);
        }

        public Enumerator GetEnumerator()
        {
            return new(List);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly bool _filled;
            private List<T>.Enumerator _enumerator;

            public Enumerator(List<T>? list)
            {
                if (list == null)
                {
                    _filled = false;
                    _enumerator = default;
                }
                else
                {
                    _filled = true;
                    _enumerator = list.GetEnumerator();
                }
            }

            public bool MoveNext()
            {
                if (!_filled)
                {
                    return false;
                }

                return _enumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                if (_filled)
                {
                    ((IEnumerator) _enumerator).Reset();
                }
            }

            public T Current => _enumerator.Current;

            object? IEnumerator.Current => Current;

            void IDisposable.Dispose()
            {
            }
        }
    }
}

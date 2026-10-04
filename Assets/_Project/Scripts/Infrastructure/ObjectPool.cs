using System;
using System.Collections.Generic;

namespace Match3.Infrastructure
{
    /// <summary>
    /// A small generic object pool. Instead of creating and destroying objects over and over
    /// (which makes garbage and hitches on mobile), objects are made once, handed out with Get,
    /// and put back with Release to be reused.
    ///
    /// The pool does not know about Unity. What "create", "take" and "return" mean is passed in as methods:
    ///   create    - makes a brand new object (for tiles: Instantiate the prefab)
    ///   onGet     - runs every time an object is handed out (for tiles: set it active)
    ///   onRelease - runs every time an object comes back (for tiles: stop its tweens, set it inactive)
    ///
    /// Not thread-safe. Use it from the main thread only.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        // Stack: the most recently released object is handed out first.
        private readonly Stack<T> _inactive = new Stack<T>();

        // Same objects as in the stack, for a fast "is it already back in the pool?" check.
        private readonly HashSet<T> _inactiveSet = new HashSet<T>();

        /// <summary>How many objects the pool has ever created (handed out + waiting).</summary>
        public int TotalCreated { get; private set; }

        public int InactiveCount => _inactive.Count;

        /// <summary>Objects currently handed out and not yet released.</summary>
        public int ActiveCount => TotalCreated - _inactive.Count;

        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _onGet = onGet;
            _onRelease = onRelease;
        }

        /// <summary>
        /// Makes sure the pool has created at least totalCount objects. Missing ones are created now and
        /// left waiting in the pool. Call it while loading, so nothing has to be created during play.
        /// </summary>
        public void Prewarm(int totalCount)
        {
            while (TotalCreated < totalCount)
            {
                T item = _create();
                TotalCreated++;
                _onRelease?.Invoke(item); // a waiting object should look "released" (for tiles: inactive)
                _inactive.Push(item);
                _inactiveSet.Add(item);
            }
        }

        /// <summary>Hands out a waiting object, or creates a new one if none is waiting.</summary>
        public T Get()
        {
            T item;
            if (_inactive.Count > 0)
            {
                item = _inactive.Pop();
                _inactiveSet.Remove(item);
            }
            else
            {
                item = _create();
                TotalCreated++;
            }

            _onGet?.Invoke(item);
            return item;
        }

        /// <summary>Takes an object back so it can be reused. Releasing the same object twice is a bug and throws.</summary>
        public void Release(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (!_inactiveSet.Add(item))
            {
                throw new InvalidOperationException("This object was released twice. It would be handed out to two users at once.");
            }

            _onRelease?.Invoke(item);
            _inactive.Push(item);
        }
    }
}

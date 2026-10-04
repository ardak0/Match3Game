using System;
using System.Collections.Generic;
using Match3.Infrastructure;
using NUnit.Framework;

namespace Match3.Tests
{
    public class ObjectPoolTests
    {
        private sealed class Thing
        {
            public int Number;
            public bool IsActive;
        }

        private int _createdCount;
        private List<Thing> _gotten;
        private List<Thing> _released;
        private ObjectPool<Thing> _pool;

        [SetUp]
        public void SetUp()
        {
            _createdCount = 0;
            _gotten = new List<Thing>();
            _released = new List<Thing>();

            _pool = new ObjectPool<Thing>(
                create: () => new Thing { Number = ++_createdCount },
                onGet: thing => { thing.IsActive = true; _gotten.Add(thing); },
                onRelease: thing => { thing.IsActive = false; _released.Add(thing); });
        }

        [Test]
        public void Get_OnAnEmptyPool_CreatesANewObject()
        {
            Thing thing = _pool.Get();

            Assert.That(thing, Is.Not.Null);
            Assert.That(_createdCount, Is.EqualTo(1));
            Assert.That(_pool.TotalCreated, Is.EqualTo(1));
            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
            Assert.That(_pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void ReleasedObject_IsReused_NotRecreated()
        {
            Thing first = _pool.Get();
            _pool.Release(first);

            Thing second = _pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(_createdCount, Is.EqualTo(1));
        }

        [Test]
        public void MostRecentlyReleasedObject_IsHandedOutFirst()
        {
            Thing a = _pool.Get();
            Thing b = _pool.Get();
            _pool.Release(a);
            _pool.Release(b);

            Assert.That(_pool.Get(), Is.SameAs(b));
            Assert.That(_pool.Get(), Is.SameAs(a));
        }

        [Test]
        public void Callbacks_RunOnEveryGetAndRelease()
        {
            Thing thing = _pool.Get();
            Assert.That(thing.IsActive, Is.True);

            _pool.Release(thing);
            Assert.That(thing.IsActive, Is.False);

            _pool.Get();
            Assert.That(_gotten.Count, Is.EqualTo(2)); // onGet ran for both Gets, including the reused object
            Assert.That(_released.Count, Is.EqualTo(1));
        }

        [Test]
        public void Counts_TrackActiveAndInactive()
        {
            Thing a = _pool.Get();
            Thing b = _pool.Get();
            _pool.Get();
            _pool.Release(a);
            _pool.Release(b);

            Assert.That(_pool.TotalCreated, Is.EqualTo(3));
            Assert.That(_pool.ActiveCount, Is.EqualTo(1));
            Assert.That(_pool.InactiveCount, Is.EqualTo(2));
        }

        [Test]
        public void Prewarm_CreatesWaitingObjects_AndRunsTheReleaseCallbackOnEach()
        {
            _pool.Prewarm(4);

            Assert.That(_createdCount, Is.EqualTo(4));
            Assert.That(_pool.TotalCreated, Is.EqualTo(4));
            Assert.That(_pool.InactiveCount, Is.EqualTo(4));
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
            Assert.That(_released.Count, Is.EqualTo(4)); // waiting objects look "released" (for tiles: hidden)
            Assert.That(_gotten, Is.Empty);
        }

        [Test]
        public void AfterPrewarm_GetDoesNotCreate_UntilThePoolRunsDry()
        {
            _pool.Prewarm(3);

            _pool.Get();
            _pool.Get();
            _pool.Get();
            Assert.That(_createdCount, Is.EqualTo(3));

            _pool.Get(); // a fourth one: the pool grows
            Assert.That(_createdCount, Is.EqualTo(4));
        }

        [Test]
        public void Prewarm_CountsObjectsAlreadyCreated()
        {
            Thing taken = _pool.Get();
            _pool.Prewarm(3);

            Assert.That(_pool.TotalCreated, Is.EqualTo(3));
            Assert.That(_pool.InactiveCount, Is.EqualTo(2));
            Assert.That(taken.IsActive, Is.True); // the handed-out object is untouched

            _pool.Prewarm(2); // asking for less than exists does nothing
            Assert.That(_pool.TotalCreated, Is.EqualTo(3));
        }

        [Test]
        public void ReleasingTheSameObjectTwice_Throws()
        {
            Thing thing = _pool.Get();
            _pool.Release(thing);

            Assert.Throws<InvalidOperationException>(() => _pool.Release(thing));
            Assert.That(_pool.InactiveCount, Is.EqualTo(1)); // the pool was not corrupted
        }

        [Test]
        public void ReleasingNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _pool.Release(null));
        }

        [Test]
        public void CreateMethodIsRequired()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Thing>(null));
        }

        [Test]
        public void PoolWorksWithoutCallbacks()
        {
            ObjectPool<Thing> plain = new ObjectPool<Thing>(() => new Thing());

            Thing thing = plain.Get();
            plain.Release(thing);

            Assert.That(plain.Get(), Is.SameAs(thing));
        }

        [Test]
        public void ManyCyclesOfGetAndRelease_NeverCreateMoreThanThePeak()
        {
            // The tile situation: the same few objects are handed out and returned again and again.
            const int peak = 10;
            _pool.Prewarm(peak);

            List<Thing> inUse = new List<Thing>();
            for (int cycle = 0; cycle < 100; cycle++)
            {
                for (int i = 0; i < peak; i++) inUse.Add(_pool.Get());
                for (int i = 0; i < peak; i++) _pool.Release(inUse[i]);
                inUse.Clear();
            }

            Assert.That(_createdCount, Is.EqualTo(peak));
            Assert.That(_pool.ActiveCount, Is.EqualTo(0));
        }
    }
}

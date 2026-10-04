using System;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class MoveCounterTests
    {
        [Test]
        public void NewCounter_StartsAtTheLimit()
        {
            MoveCounter moves = new MoveCounter(5);

            Assert.That(moves.MoveLimit, Is.EqualTo(5));
            Assert.That(moves.MovesLeft, Is.EqualTo(5));
            Assert.That(moves.HasMovesLeft, Is.True);
        }

        [Test]
        public void UseMove_DecreasesMovesLeft_AndRaisesTheEventWithTheNewValue()
        {
            MoveCounter moves = new MoveCounter(3);
            int reported = -1;
            moves.MovesChanged += left => reported = left;

            moves.UseMove();

            Assert.That(moves.MovesLeft, Is.EqualTo(2));
            Assert.That(reported, Is.EqualTo(2));
        }

        [Test]
        public void HasMovesLeft_IsFalseAfterTheLastMove()
        {
            MoveCounter moves = new MoveCounter(1);

            moves.UseMove();

            Assert.That(moves.HasMovesLeft, Is.False);
        }

        [Test]
        public void UseMove_WithNoMovesLeft_Throws_AndRaisesNoEvent()
        {
            MoveCounter moves = new MoveCounter(1);
            moves.UseMove();
            int eventCount = 0;
            moves.MovesChanged += _ => eventCount++;

            Assert.Throws<InvalidOperationException>(() => moves.UseMove());
            Assert.That(eventCount, Is.EqualTo(0));
            Assert.That(moves.MovesLeft, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_RejectsALimitBelowOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoveCounter(0));
        }
    }
}

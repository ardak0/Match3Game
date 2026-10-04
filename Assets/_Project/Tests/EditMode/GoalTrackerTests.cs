using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class GoalTrackerTests
    {
        // A result that only says how many tiles of each color were cleared. The steps do not matter here.
        private static ResolveResult Cleared(int red = 0, int green = 0, int blue = 0)
        {
            int[] clearedByColor = new int[Enum.GetValues(typeof(TileColor)).Length];
            clearedByColor[(int)TileColor.Red] = red;
            clearedByColor[(int)TileColor.Green] = green;
            clearedByColor[(int)TileColor.Blue] = blue;
            return new ResolveResult(new ResolveStep[0], clearedByColor, 1);
        }

        private static GoalTracker RedAndBlue(int red, int blue)
        {
            return new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, red),
                new GoalDefinition(TileColor.Blue, blue)
            });
        }

        [Test]
        public void NewTracker_HasEveryGoalStillToDo()
        {
            GoalTracker goals = RedAndBlue(5, 7);

            Assert.That(goals.GoalCount, Is.EqualTo(2));
            Assert.That(goals.GetColor(0), Is.EqualTo(TileColor.Red));
            Assert.That(goals.GetRemaining(0), Is.EqualTo(5));
            Assert.That(goals.GetRemaining(1), Is.EqualTo(7));
            Assert.That(goals.AllGoalsMet, Is.False);
        }

        [Test]
        public void Register_CountsOnlyTheGoalColors()
        {
            GoalTracker goals = RedAndBlue(5, 7);

            goals.Register(Cleared(red: 3, green: 4, blue: 2));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(2));
            Assert.That(goals.GetRemaining(1), Is.EqualTo(5));
        }

        [Test]
        public void Register_AddsUpOverSeveralMoves()
        {
            GoalTracker goals = RedAndBlue(5, 7);

            goals.Register(Cleared(red: 2));
            goals.Register(Cleared(red: 2));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(1));
        }

        [Test]
        public void Register_NeverGoesBelowZero()
        {
            GoalTracker goals = RedAndBlue(2, 7);

            goals.Register(Cleared(red: 10));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(0));
        }

        [Test]
        public void AllGoalsMet_IsTrueOnlyWhenEveryGoalIsDone()
        {
            GoalTracker goals = RedAndBlue(3, 3);

            goals.Register(Cleared(red: 3));
            Assert.That(goals.AllGoalsMet, Is.False);

            goals.Register(Cleared(blue: 3));
            Assert.That(goals.AllGoalsMet, Is.True);
        }

        [Test]
        public void Register_RaisesGoalChanged_WithIndexAndRemaining_OnlyForGoalsThatMoved()
        {
            GoalTracker goals = RedAndBlue(5, 7);
            List<(int index, int remaining)> events = new List<(int, int)>();
            goals.GoalChanged += (index, remaining) => events.Add((index, remaining));

            goals.Register(Cleared(blue: 4, green: 9));

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.EqualTo((1, 3)));
        }

        [Test]
        public void Register_DoesNotReportAGoalThatWasAlreadyDone()
        {
            GoalTracker goals = RedAndBlue(2, 7);
            goals.Register(Cleared(red: 2));
            int eventCount = 0;
            goals.GoalChanged += (_, __) => eventCount++;

            goals.Register(Cleared(red: 3));

            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void TwoGoalsWithTheSameColor_AreCountedSeparately()
        {
            GoalTracker goals = new GoalTracker(new[]
            {
                new GoalDefinition(TileColor.Red, 2),
                new GoalDefinition(TileColor.Red, 5)
            });

            goals.Register(Cleared(red: 3));

            Assert.That(goals.GetRemaining(0), Is.EqualTo(0));
            Assert.That(goals.GetRemaining(1), Is.EqualTo(2));
        }

        [Test]
        public void Constructor_RejectsNoGoals_AndGoalsAskingForNothing()
        {
            Assert.Throws<ArgumentException>(() => new GoalTracker(new GoalDefinition[0]));
            Assert.Throws<ArgumentException>(() => new GoalTracker(new[] { new GoalDefinition(TileColor.Red, 0) }));
        }
    }
}

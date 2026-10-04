using System;
using System.Collections.Generic;
using Match3.Game;
using NUnit.Framework;

namespace Match3.Tests
{
    public class GameStateMachineTests
    {
        // Records Enter/Exit calls into a shared log so the tests can check the order.
        private class RecordingState : IGameState
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingState(string name, List<string> log)
            {
                _name = name;
                _log = log;
            }

            public virtual void Enter() => _log.Add(_name + ".Enter");
            public virtual void Exit() => _log.Add(_name + ".Exit");
        }

        private sealed class StateA : RecordingState { public StateA(List<string> log) : base("A", log) { } }
        private sealed class StateB : RecordingState { public StateB(List<string> log) : base("B", log) { } }
        private sealed class NeverRegistered : RecordingState { public NeverRegistered(List<string> log) : base("N", log) { } }

        // Passes straight through to B from inside Enter, like ResolvingState does.
        private sealed class PassThroughState : RecordingState
        {
            private readonly GameStateMachine _machine;

            public PassThroughState(GameStateMachine machine, List<string> log) : base("P", log)
            {
                _machine = machine;
            }

            public override void Enter()
            {
                base.Enter();
                _machine.ChangeTo<StateB>();
            }
        }

        private List<string> _log;
        private GameStateMachine _machine;

        [SetUp]
        public void SetUp()
        {
            _log = new List<string>();
            _machine = new GameStateMachine();
        }

        [Test]
        public void NewMachine_HasNoCurrentState()
        {
            Assert.That(_machine.Current, Is.Null);
        }

        [Test]
        public void ChangeTo_EntersTheState_AndMakesItCurrent()
        {
            StateA a = new StateA(_log);
            _machine.Register(a);

            _machine.ChangeTo<StateA>();

            Assert.That(_machine.Current, Is.SameAs(a));
            Assert.That(_machine.IsIn<StateA>(), Is.True);
            Assert.That(_machine.IsIn<StateB>(), Is.False);
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter" }));
        }

        [Test]
        public void ChangeTo_ExitsTheOldStateBeforeEnteringTheNewOne()
        {
            _machine.Register(new StateA(_log));
            _machine.Register(new StateB(_log));
            _machine.ChangeTo<StateA>();

            _machine.ChangeTo<StateB>();

            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.Exit", "B.Enter" }));
        }

        [Test]
        public void StateChanged_ReportsPreviousAndNext_BeforeTheNewStateIsEntered()
        {
            StateA a = new StateA(_log);
            StateB b = new StateB(_log);
            _machine.Register(a);
            _machine.Register(b);
            IGameState reportedPrevious = null;
            IGameState reportedNext = null;
            _machine.StateChanged += (previous, next) =>
            {
                reportedPrevious = previous;
                reportedNext = next;
                _log.Add("event");
            };
            _machine.ChangeTo<StateA>();
            _log.Clear();

            _machine.ChangeTo<StateB>();

            Assert.That(reportedPrevious, Is.SameAs(a));
            Assert.That(reportedNext, Is.SameAs(b));
            Assert.That(_log, Is.EqualTo(new[] { "A.Exit", "event", "B.Enter" }));
        }

        [Test]
        public void ChangeTo_InsideEnter_PassesThroughToTheLastState()
        {
            _machine.Register(new PassThroughState(_machine, _log));
            _machine.Register(new StateB(_log));

            _machine.ChangeTo<PassThroughState>();

            Assert.That(_machine.IsIn<StateB>(), Is.True);
            Assert.That(_log, Is.EqualTo(new[] { "P.Enter", "P.Exit", "B.Enter" }));
        }

        [Test]
        public void ChangeTo_AnUnregisteredState_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _machine.ChangeTo<NeverRegistered>());
        }

        [Test]
        public void Register_TheSameTypeTwice_Throws()
        {
            _machine.Register(new StateA(_log));

            Assert.Throws<ArgumentException>(() => _machine.Register(new StateA(_log)));
        }

        [Test]
        public void Stop_ExitsTheCurrentState_AndClearsIt()
        {
            _machine.Register(new StateA(_log));
            _machine.ChangeTo<StateA>();

            _machine.Stop();

            Assert.That(_machine.Current, Is.Null);
            Assert.That(_log, Is.EqualTo(new[] { "A.Enter", "A.Exit" }));
        }

        [Test]
        public void Stop_WithoutACurrentState_DoesNothing()
        {
            Assert.DoesNotThrow(() => _machine.Stop());
        }
    }
}

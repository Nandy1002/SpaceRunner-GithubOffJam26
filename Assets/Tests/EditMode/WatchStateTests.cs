using System;
using NUnit.Framework;
using SpaceRunner.Core;

namespace SpaceRunner.Tests
{
    public sealed class WatchStateTests
    {
        [Test]
        public void ClosingWatchDoesNotReleaseAnotherFeaturesPause()
        {
            var pause = new PauseState();
            using var otherFeature = pause.Acquire();
            using var watch = new WatchState(pause);
            watch.Toggle();
            watch.CompletePresentation(true);
            watch.Toggle();
            watch.CompletePresentation(false);
            Assert.That(pause.IsPaused, Is.True);
            otherFeature.Dispose();
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void WorldRemainsPausedUntilTheArmHasLowered()
        {
            var pause = new PauseState();
            using var watch = new WatchState(pause);
            watch.Toggle();
            Assert.That(pause.IsPaused, Is.True);
            watch.CompletePresentation(true);
            watch.Toggle();
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Closing));
            Assert.That(pause.IsPaused, Is.True);
            watch.CompletePresentation(false);
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void RapidTogglesIgnoreStaleAnimationEvents()
        {
            var pause = new PauseState();
            using var watch = new WatchState(pause);
            watch.Toggle(); watch.Toggle();
            watch.CompletePresentation(true);
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Closing));
            watch.Toggle();
            watch.CompletePresentation(false);
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Opening));
            Assert.That(pause.IsPaused, Is.True);
            watch.CompletePresentation(true);
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Open));
        }

        [Test]
        public void DisabledPauseOptionAllowsTimeToAdvanceWhileWatchIsOpen()
        {
            var pause = new PauseState();
            var timer = new RunTimer(pause);
            using var watch = new WatchState(pause, false);
            timer.StartRun();
            watch.Toggle(); watch.CompletePresentation(true);
            timer.Tick(2);
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(2));
            watch.Toggle(); watch.CompletePresentation(false);
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void PauseOptionCanBeChangedDuringAnOpenWatch()
        {
            var pause = new PauseState();
            using var watch = new WatchState(pause);
            watch.Toggle(); watch.CompletePresentation(true);
            watch.SetPausesGameplay(false);
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Open));
            watch.SetPausesGameplay(true);
            Assert.That(pause.IsPaused, Is.True);
            watch.Reset();
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void DisablingWatchPauseStillPreservesAnExternalPause()
        {
            var pause = new PauseState();
            using var other = pause.Acquire();
            using var watch = new WatchState(pause);
            watch.Toggle();
            watch.SetPausesGameplay(false);
            Assert.That(pause.IsPaused, Is.True);
        }

        [Test]
        public void EndingAndRestartingRunResetsTimer()
        {
            var timer = new RunTimer(new PauseState());
            timer.StartRun(); timer.Tick(12.5); timer.EndRun(); timer.Tick(100);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(12.5));
            timer.StartRun();
            Assert.That(timer.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void PausedTimeNeverGetsAddedOnResume()
        {
            var pause = new PauseState();
            var timer = new RunTimer(pause);
            timer.StartRun(); timer.Tick(1);
            var lease = pause.Acquire(); timer.Tick(1000);
            lease.Dispose(); timer.Tick(2);
            Assert.That(timer.ElapsedSeconds, Is.EqualTo(3));
        }

        [Test]
        public void ResetDuringOpeningReleasesPauseAndRejectsLateCompletion()
        {
            var pause = new PauseState();
            using var watch = new WatchState(pause);
            watch.Toggle(); watch.Reset(); watch.CompletePresentation(true);
            Assert.That(watch.Phase, Is.EqualTo(WatchPhase.Closed));
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void PauseLeaseReleaseIsIdempotent()
        {
            var pause = new PauseState();
            int changes = 0;
            pause.Changed += _ => changes++;
            var first = pause.Acquire();
            var second = pause.Acquire();
            first.Dispose(); first.Dispose(); second.Dispose(); second.Dispose();
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(pause.IsPaused, Is.False);
        }

        [Test]
        public void TimerFormattingDoesNotWrapAfterAnHour()
        {
            var timer = new RunTimer(new PauseState());
            timer.StartRun(); timer.Tick(3661.25);
            Assert.That(timer.DisplayTime, Is.EqualTo("61:01.25"));
        }

        [Test]
        public void InvalidDeltaCannotCorruptTheTimer()
        {
            var timer = new RunTimer(new PauseState());
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => timer.Tick(-1));
        }
    }
}

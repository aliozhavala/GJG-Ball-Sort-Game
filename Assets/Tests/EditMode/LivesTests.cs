using System;
using BallSort.Core.Player;
using NUnit.Framework;

namespace BallSort.Tests.EditMode
{
    /// <summary>Lives refill: one back per whole interval below the maximum, from a saved timestamp.</summary>
    [TestFixture]
    public sealed class LivesTests
    {
        private const int Max = 5;
        private static readonly TimeSpan Interval = TestPlayers.RefillInterval;
        private static readonly DateTime T0 = TestPlayers.Start;

        private static Lives Make(int count, DateTime? timerStart, DateTime now) =>
            new Lives(count, timerStart, Max, Interval, now);

        [Test]
        public void AtTheMaximum_NoLifeIsOnItsWay()
        {
            Lives lives = Make(Max, null, T0);

            Assert.That(lives.IsFull, Is.True);
            Assert.That(lives.TimerStartUtc, Is.Null);
            Assert.That(lives.GetTimeUntilNext(T0), Is.Null);
        }

        [Test]
        public void LosingALifeAtTheMaximum_StartsTheWaitThen()
        {
            Lives lives = Make(Max, null, T0);

            Assert.That(lives.LoseOne(T0), Is.True);

            Assert.That(lives.Count, Is.EqualTo(4));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0));
            Assert.That(lives.GetTimeUntilNext(T0), Is.EqualTo(Interval));
            Assert.That(lives.GetTimeUntilNext(T0.AddMinutes(10)), Is.EqualTo(TimeSpan.FromMinutes(20)));
        }

        [Test]
        public void LosingALifeBelowTheMaximum_KeepsTheCurrentWait()
        {
            Lives lives = Make(4, T0, T0);

            lives.LoseOne(T0.AddMinutes(10));

            Assert.That(lives.Count, Is.EqualTo(3));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0));
            Assert.That(lives.GetTimeUntilNext(T0.AddMinutes(10)), Is.EqualTo(TimeSpan.FromMinutes(20)));
        }

        [Test]
        public void ALifeComesBack_OnlyOnceAWholeIntervalHasPassed()
        {
            Lives lives = Make(4, T0, T0);

            Assert.That(lives.Refresh(T0 + Interval - TimeSpan.FromSeconds(1)), Is.False);
            Assert.That(lives.Count, Is.EqualTo(4));

            Assert.That(lives.Refresh(T0 + Interval), Is.True);
            Assert.That(lives.Count, Is.EqualTo(5));
            Assert.That(lives.IsFull, Is.True);
            Assert.That(lives.TimerStartUtc, Is.Null, "full: the timer stops");
        }

        [Test]
        public void PartOfAnInterval_CarriesOverToTheNextLife()
        {
            Lives lives = Make(2, T0, T0);

            lives.Refresh(T0.AddMinutes(45));

            Assert.That(lives.Count, Is.EqualTo(3));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0.AddMinutes(30)));
            Assert.That(lives.GetTimeUntilNext(T0.AddMinutes(45)), Is.EqualTo(TimeSpan.FromMinutes(15)));
        }

        [Test]
        public void ManyIntervals_FillUpToTheMaximumOnly()
        {
            Lives lives = Make(1, T0, T0);

            lives.Refresh(T0.AddHours(10));

            Assert.That(lives.Count, Is.EqualTo(Max));
            Assert.That(lives.TimerStartUtc, Is.Null);
        }

        [Test]
        public void TimeUntilNext_IsNullWhenTheMaximumIsDueByThen()
        {
            Lives lives = Make(4, T0, T0);

            Assert.That(lives.GetTimeUntilNext(T0.AddMinutes(31)), Is.Null);
            Assert.That(lives.Count, Is.EqualTo(4), "a query changes nothing");
        }

        [Test]
        public void TheConstructor_AddsTheLivesDueSinceTheSavedTimer()
        {
            Lives lives = Make(2, T0, T0.AddMinutes(61));

            Assert.That(lives.Count, Is.EqualTo(4));
            Assert.That(lives.GetTimeUntilNext(T0.AddMinutes(61)), Is.EqualTo(TimeSpan.FromMinutes(29)));
        }

        [Test]
        public void ASaveBelowTheMaximumWithoutATimer_StartsTheWaitAtLoad()
        {
            Lives lives = Make(3, null, T0);

            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0));
            Assert.That(lives.GetTimeUntilNext(T0), Is.EqualTo(Interval));
        }

        [Test]
        public void LivesNeverGoBelowZero()
        {
            Lives lives = Make(0, T0, T0);

            Assert.That(lives.LoseOne(T0.AddMinutes(1)), Is.False);

            Assert.That(lives.Count, Is.EqualTo(0));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0), "the wait goes on");
        }

        [Test]
        public void LoseOne_FirstAddsTheLivesThatCameBack()
        {
            Lives lives = Make(0, T0, T0);

            Assert.That(lives.LoseOne(T0 + Interval), Is.True, "the life that just came back is spent");

            Assert.That(lives.Count, Is.EqualTo(0));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(T0 + Interval));
        }

        [Test]
        public void TurningTheClockBack_RestartsTheWaitWithoutGivingALife()
        {
            Lives lives = Make(3, T0, T0);
            DateTime earlier = T0.AddHours(-2);

            Assert.That(lives.Refresh(earlier), Is.True);

            Assert.That(lives.Count, Is.EqualTo(3));
            Assert.That(lives.TimerStartUtc, Is.EqualTo(earlier));
            Assert.That(lives.GetTimeUntilNext(earlier), Is.EqualTo(Interval));
        }

        [Test]
        public void Changed_FiresOnlyWhenSomethingChanged()
        {
            Lives lives = Make(4, T0, T0);
            int changes = 0;
            lives.Changed += () => changes++;

            lives.Refresh(T0.AddMinutes(5));
            Assert.That(changes, Is.EqualTo(0));

            lives.LoseOne(T0.AddMinutes(5));
            lives.Refresh(T0.AddMinutes(30));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void Set_ForTestingTools_SetsTheCountAndTheWait()
        {
            Lives lives = Make(Max, null, T0);

            lives.Set(2, TimeSpan.FromMinutes(5), T0);

            Assert.That(lives.Count, Is.EqualTo(2));
            Assert.That(lives.GetTimeUntilNext(T0), Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [TestCase(-1)]
        [TestCase(Max + 1)]
        public void Constructor_RejectsACountOutOfRange(int count)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Make(count, null, T0));
        }

        // --- Through the save, as the game uses them -----------------------------------------------

        [Test]
        public void LivesComeBack_WhileTheAppIsClosed()
        {
            var clock = new FakeClock(T0);
            PlayerData player = TestPlayers.New(clock);
            for (int i = 0; i < 3; i++)
            {
                player.BeginAttempt(new Random(1));
                player.LeaveAttempt();
            }

            Assert.That(player.LifeCount, Is.EqualTo(2));
            string save = TestPlayers.Save(player);

            // The app is closed for 65 minutes: two whole intervals, and 5 minutes into the third.
            clock.Advance(TimeSpan.FromMinutes(65));
            PlayerData relaunched = TestPlayers.Launch(save, clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(4));
            Assert.That(relaunched.GetTimeUntilNextLife(), Is.EqualTo(TimeSpan.FromMinutes(25)));
        }

        [Test]
        public void LivesFillUp_AfterALongTimeClosed()
        {
            var clock = new FakeClock(T0);
            PlayerData player = TestPlayers.New(clock);
            player.SetLives(0);
            string save = TestPlayers.Save(player);

            clock.Advance(TimeSpan.FromDays(2));
            PlayerData relaunched = TestPlayers.Launch(save, clock);

            Assert.That(relaunched.LifeCount, Is.EqualTo(Max));
            Assert.That(relaunched.GetTimeUntilNextLife(), Is.Null);
        }

        [Test]
        public void ALifeComesBack_WhileTheAppIsOpen()
        {
            var clock = new FakeClock(T0);
            PlayerData player = TestPlayers.New(clock);
            player.SetLives(3, TimeSpan.FromMinutes(1));
            var counts = new System.Collections.Generic.List<int>();
            player.LivesChanged += counts.Add;
            int saves = 0;
            player.Changed += () => saves++;

            clock.Advance(TimeSpan.FromMinutes(1));
            player.RefreshLives();

            Assert.That(player.LifeCount, Is.EqualTo(4));
            Assert.That(counts, Is.EqualTo(new[] { 4 }));
            Assert.That(saves, Is.EqualTo(1));
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using KeePass.Infrastructure.Security;

namespace KeePass.Tests.Infrastructure
{
    /// <summary>
    /// AutoLockManager tests.
    /// NOTE: Start() and NotifyActivity() read Program.Config which is null in the
    /// test host, so those code paths are guarded by null-checks in the implementation
    /// and safe to call. Timer tick is never triggered because we don't pump the
    /// WinForms message loop, so Program.Config is never reached inside OnTimerTick.
    /// </summary>
    [TestClass]
    public class AutoLockManagerTests
    {
        // ── Constructor ───────────────────────────────────────────────────────

        [TestMethod]
        public void Constructor_DoesNotThrow()
        {
            using(var mgr = new AutoLockManager()) { }
        }

        [TestMethod]
        public void IsRunning_Initially_IsFalse()
        {
            using(var mgr = new AutoLockManager())
            {
                Assert.IsFalse(mgr.IsRunning);
            }
        }

        // ── Start ─────────────────────────────────────────────────────────────

        [TestMethod]
        public void Start_WhenProgramConfigNull_DoesNotThrow()
        {
            // Program.Config is null in test host; Start() guards against it
            using(var mgr = new AutoLockManager())
            {
                mgr.Start();
                // IsRunning is set to true inside Start regardless of config
                // (its guard only skips the timer interval setup)
            }
        }

        // ── Stop ──────────────────────────────────────────────────────────────

        [TestMethod]
        public void Stop_WithoutStart_DoesNotThrow()
        {
            using(var mgr = new AutoLockManager())
            {
                mgr.Stop();
            }
        }

        [TestMethod]
        public void Stop_SetsIsRunningFalse()
        {
            using(var mgr = new AutoLockManager())
            {
                mgr.Start();
                mgr.Stop();

                Assert.IsFalse(mgr.IsRunning);
            }
        }

        // ── NotifyActivity ────────────────────────────────────────────────────

        [TestMethod]
        public void NotifyActivity_WhenNotRunning_DoesNotThrow()
        {
            using(var mgr = new AutoLockManager())
            {
                mgr.NotifyActivity(); // IsRunning=false → early return
            }
        }

        [TestMethod]
        public void NotifyActivity_WhenRunning_DoesNotThrow()
        {
            using(var mgr = new AutoLockManager())
            {
                mgr.Start();
                mgr.NotifyActivity(); // Program.Config is null → timer not restarted, but no exception
            }
        }

        // ── LockRequested event ───────────────────────────────────────────────

        [TestMethod]
        public void LockRequested_CanAttachAndDetachHandler()
        {
            using(var mgr = new AutoLockManager())
            {
                System.EventHandler h = (s, e) => { };
                mgr.LockRequested += h;
                mgr.LockRequested -= h;
            }
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [TestMethod]
        public void Dispose_DoesNotThrow()
        {
            var mgr = new AutoLockManager();
            mgr.Dispose();
        }

        [TestMethod]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var mgr = new AutoLockManager();
            mgr.Dispose();
            mgr.Dispose(); // idempotent — m_disposed guard
        }

        [TestMethod]
        public void Start_AfterDispose_DoesNotThrow()
        {
            var mgr = new AutoLockManager();
            mgr.Dispose();
            mgr.Start(); // m_disposed guard → early return
        }
    }
}

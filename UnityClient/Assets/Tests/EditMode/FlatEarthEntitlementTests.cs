using NUnit.Framework;
using TransparentEarth.Store;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class FlatEarthEntitlementTests
    {
        [SetUp]
        public void Reset()
        {
            PlayerPrefs.DeleteKey("OverHorizon.FlatEarth.Simulated.v1");
            FlatEarthEntitlement.ResetForTesting();
        }

        [TearDown]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey("OverHorizon.FlatEarth.Simulated.v1");
            FlatEarthEntitlement.ResetForTesting();
        }

        [Test]
        public void ModeIsLockedByDefault()
        {
            Assert.That(FlatEarthEntitlement.IsUnlocked, Is.False);
        }

        [Test]
        public void PurchaseUnlocksTheModeThroughBrokerOwnership()
        {
            FlatEarthEntitlement.Purchase();
            Assert.That(FlatEarthEntitlement.IsUnlocked, Is.True);
            Assert.That(FlatEarthEntitlement.State.Phase, Is.EqualTo(PurchasePhase.Owned));
            Assert.That(PlayerPrefs.GetInt("OverHorizon.FlatEarth.Simulated.v1", 0), Is.EqualTo(1));
        }

        [Test]
        public void RestoreFailsWhenNothingWasBought()
        {
            FlatEarthEntitlement.Restore();
            Assert.That(FlatEarthEntitlement.IsUnlocked, Is.False);
            Assert.That(FlatEarthEntitlement.State.Phase, Is.EqualTo(PurchasePhase.Failed));
        }

        [Test]
        public void RestoreRecoversAPreviousSimulatedPurchase()
        {
            PlayerPrefs.SetInt("OverHorizon.FlatEarth.Simulated.v1", 1);
            FlatEarthEntitlement.Restore();
            Assert.That(FlatEarthEntitlement.IsUnlocked, Is.True);
        }

        [Test]
        public void CustomBrokerCanReportOwnershipDirectly()
        {
            FlatEarthEntitlement.Broker = new AlwaysOwnedBroker();
            Assert.That(FlatEarthEntitlement.IsUnlocked, Is.True);
        }

        private sealed class AlwaysOwnedBroker : IFlatEarthPurchaseBroker
        {
            public string ProductId => FlatEarthEntitlement.ProductId;
            public string LocalizedPrice => "$0.00";
            public bool IsReady => true;
            public bool OwnsProduct => true;
            public void Purchase(System.Action<bool, string> onComplete) => onComplete(true, "ok");
            public void Restore(System.Action<bool, string> onComplete) => onComplete(true, "ok");
            public void ManageSubscription() { }
        }
    }
}

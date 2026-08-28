using System;
using TransparentEarth.App;
using UnityEngine;

namespace TransparentEarth.Store
{
    public enum PurchasePhase
    {
        Idle,
        Pending,
        Owned,
        Failed
    }

    public readonly struct PurchaseState
    {
        public readonly PurchasePhase Phase;
        public readonly string Message;

        public PurchaseState(PurchasePhase phase, string message = "")
        {
            Phase = phase;
            Message = message ?? string.Empty;
        }
    }

    public interface IFlatEarthPurchaseBroker
    {
        string ProductId { get; }
        string LocalizedPrice { get; }
        bool IsReady { get; }
        bool OwnsProduct { get; }
        void Purchase(Action<bool, string> onComplete);
        void Restore(Action<bool, string> onComplete);
        void ManageSubscription();
    }

    /// <summary>
    /// Session entitlement for the renewable Flat Earth subscription. Store ownership is the
    /// source of truth: a local PlayerPrefs flag must never keep an expired subscription open.
    /// </summary>
    public static class FlatEarthEntitlement
    {
        public const string ProductId = AppIdentity.FlatEarthSubscriptionId;

        private static IFlatEarthPurchaseBroker _broker;

        public static event Action<PurchaseState> StateChanged;
        public static PurchaseState State { get; private set; } = new(PurchasePhase.Idle);

        public static IFlatEarthPurchaseBroker Broker
        {
            get => _broker ??= CreateDefaultBroker();
            set
            {
                _broker = value ?? CreateDefaultBroker();
                NotifyBrokerChanged();
            }
        }

        public static string LocalizedPrice => Broker.LocalizedPrice;
        public static bool IsReady => Broker.IsReady;
        public static bool IsUnlocked => Broker.OwnsProduct;

        public static void Purchase()
        {
            if (IsUnlocked)
            {
                SetState(new PurchaseState(PurchasePhase.Owned));
                return;
            }

            if (!Broker.IsReady)
            {
                SetState(new PurchaseState(PurchasePhase.Failed, "store unavailable"));
                return;
            }

            SetState(new PurchaseState(PurchasePhase.Pending));
            Broker.Purchase(CompleteStoreOperation);
        }

        public static void Restore()
        {
            if (!Broker.IsReady)
            {
                SetState(new PurchaseState(PurchasePhase.Failed, "store unavailable"));
                return;
            }

            SetState(new PurchaseState(PurchasePhase.Pending));
            Broker.Restore(CompleteStoreOperation);
        }

        public static void ManageSubscription() => Broker.ManageSubscription();

        /// <summary>Called by the store service when price or entitlement changes.</summary>
        public static void NotifyBrokerChanged()
        {
            if (_broker == null) return;
            SetState(new PurchaseState(_broker.OwnsProduct ? PurchasePhase.Owned : PurchasePhase.Idle));
        }

        private static void CompleteStoreOperation(bool success, string message)
        {
            SetState(success && Broker.OwnsProduct
                ? new PurchaseState(PurchasePhase.Owned, message)
                : new PurchaseState(PurchasePhase.Failed, message));
        }

        public static void ResetForTesting()
        {
            _broker = null;
            SetState(new PurchaseState(PurchasePhase.Idle));
        }

        private static IFlatEarthPurchaseBroker CreateDefaultBroker()
        {
#if UNITY_EDITOR
            return new SimulatedPurchaseBroker();
#else
            return new UnavailablePurchaseBroker();
#endif
        }

        private static void SetState(PurchaseState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }

    /// <summary>Editor-only stand-in used by EditMode tests and Play Mode previews.</summary>
    public sealed class SimulatedPurchaseBroker : IFlatEarthPurchaseBroker
    {
        private const string OwnedKey = "OverHorizon.FlatEarth.Simulated.v1";

        public string ProductId => FlatEarthEntitlement.ProductId;
        public string LocalizedPrice => "€2.99 / month";
        public bool IsReady => true;
        public bool OwnsProduct => PlayerPrefs.GetInt(OwnedKey, 0) == 1;

        public void Purchase(Action<bool, string> onComplete)
        {
            PlayerPrefs.SetInt(OwnedKey, 1);
            PlayerPrefs.Save();
            onComplete?.Invoke(true, "purchased");
        }

        public void Restore(Action<bool, string> onComplete)
        {
            onComplete?.Invoke(OwnsProduct, OwnsProduct ? "restored" : "nothing to restore");
        }

        public void ManageSubscription() { }
    }

    public sealed class UnavailablePurchaseBroker : IFlatEarthPurchaseBroker
    {
        public string ProductId => FlatEarthEntitlement.ProductId;
        public string LocalizedPrice => "—";
        public bool IsReady => false;
        public bool OwnsProduct => false;
        public void Purchase(Action<bool, string> onComplete) => onComplete?.Invoke(false, "store unavailable");
        public void Restore(Action<bool, string> onComplete) => onComplete?.Invoke(false, "store unavailable");
        public void ManageSubscription() { }
    }
}

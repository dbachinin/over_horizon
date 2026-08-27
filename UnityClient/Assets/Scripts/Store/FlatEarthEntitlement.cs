using System;
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

    /// <summary>
    /// A store broker the "Flat Earther" upgrade can be wired to. The default implementation is a
    /// local simulation so the feature is usable offline; drop in a Unity IAP / Google Play Billing
    /// backed implementation and assign <see cref="FlatEarthEntitlement.Broker"/> at startup to
    /// charge real money for the same product id.
    /// </summary>
    public interface IFlatEarthPurchaseBroker
    {
        string ProductId { get; }
        string LocalizedPrice { get; }
        bool OwnsProduct { get; }
        void Purchase(Action<bool, string> onComplete);
        void Restore(Action<bool, string> onComplete);
    }

    public static class FlatEarthEntitlement
    {
        public const string ProductId = "com.transparentearth.unity.flatearth";
        private const string OwnedKey = "OverHorizon.FlatEarth.Owned.v1";

        private static IFlatEarthPurchaseBroker _broker;

        public static event Action<PurchaseState> StateChanged;
        public static PurchaseState State { get; private set; } = new(PurchasePhase.Idle);

        public static IFlatEarthPurchaseBroker Broker
        {
            get => _broker ??= new SimulatedPurchaseBroker();
            set
            {
                _broker = value;
                if (IsUnlocked) SetState(new PurchaseState(PurchasePhase.Owned));
            }
        }

        public static string LocalizedPrice => Broker.LocalizedPrice;

        public static bool IsUnlocked =>
            PlayerPrefs.GetInt(OwnedKey, 0) == 1 || Broker.OwnsProduct;

        public static void Purchase()
        {
            if (IsUnlocked)
            {
                MarkOwned();
                return;
            }

            SetState(new PurchaseState(PurchasePhase.Pending));
            Broker.Purchase((success, message) =>
            {
                if (success) MarkOwned();
                else SetState(new PurchaseState(PurchasePhase.Failed, message));
            });
        }

        public static void Restore()
        {
            SetState(new PurchaseState(PurchasePhase.Pending));
            Broker.Restore((success, message) =>
            {
                if (success) MarkOwned();
                else SetState(new PurchaseState(PurchasePhase.Failed, message));
            });
        }

        private static void MarkOwned()
        {
            PlayerPrefs.SetInt(OwnedKey, 1);
            PlayerPrefs.Save();
            SetState(new PurchaseState(PurchasePhase.Owned));
        }

        /// Clears the local entitlement. Intended for tests and support tooling, not the UI.
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(OwnedKey);
            _broker = null;
            SetState(new PurchaseState(PurchasePhase.Idle));
        }

        private static void SetState(PurchaseState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }

    /// <summary>
    /// Offline stand-in for a real storefront. Confirms the purchase after a short "processing"
    /// delay and remembers it in <see cref="PlayerPrefs"/>. Replace with a billing-backed broker
    /// for production.
    /// </summary>
    public sealed class SimulatedPurchaseBroker : IFlatEarthPurchaseBroker
    {
        private const string OwnedKey = "OverHorizon.FlatEarth.Simulated.v1";

        public string ProductId => FlatEarthEntitlement.ProductId;
        public string LocalizedPrice => "€2.99";
        public bool OwnsProduct => PlayerPrefs.GetInt(OwnedKey, 0) == 1;

        public void Purchase(Action<bool, string> onComplete) => Settle(onComplete);

        public void Restore(Action<bool, string> onComplete)
        {
            if (OwnsProduct) onComplete?.Invoke(true, "restored");
            else onComplete?.Invoke(false, "nothing to restore");
        }

        private void Settle(Action<bool, string> onComplete)
        {
            var runner = PurchaseCoroutineRunner.Instance;
            if (runner == null)
            {
                Confirm(onComplete);
                return;
            }
            runner.RunAfter(1.4f, () => Confirm(onComplete));
        }

        private void Confirm(Action<bool, string> onComplete)
        {
            PlayerPrefs.SetInt(OwnedKey, 1);
            PlayerPrefs.Save();
            onComplete?.Invoke(true, "purchased");
        }
    }

    /// Minimal MonoBehaviour so the simulated broker can fake asynchronous storefront latency.
    public sealed class PurchaseCoroutineRunner : MonoBehaviour
    {
        private static PurchaseCoroutineRunner _instance;

        public static PurchaseCoroutineRunner Instance
        {
            get
            {
                if (_instance != null) return _instance;
                if (!Application.isPlaying) return null;
                var host = new GameObject("Flat Earth Purchase Runner");
                UnityEngine.Object.DontDestroyOnLoad(host);
                _instance = host.AddComponent<PurchaseCoroutineRunner>();
                return _instance;
            }
        }

        public void RunAfter(float seconds, Action action) => StartCoroutine(Delayed(seconds, action));

        private static System.Collections.IEnumerator Delayed(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TransparentEarth.App;
using UnityEngine;
using UnityEngine.Purchasing;

namespace TransparentEarth.Store
{
    /// <summary>
    /// Google Play renewable-subscription broker backed by Unity IAP 5. The store is queried on
    /// every launch and remains authoritative, so cancelled or expired subscriptions are revoked.
    /// </summary>
    public sealed class GooglePlaySubscriptionService : MonoBehaviour, IFlatEarthPurchaseBroker
    {
        private StoreController _store;
        private Product _product;
        private Action<bool, string> _purchaseCompletion;
        private Action<bool, string> _restoreCompletion;
        private bool _ownsProduct;
        private bool _initialized;
        private string _localizedPrice = "—";

        public string ProductId => FlatEarthEntitlement.ProductId;
        public string LocalizedPrice => _localizedPrice;
        public bool IsReady => _initialized && _product != null;
        public bool OwnsProduct => _ownsProduct;

        public async void Initialize()
        {
            if (_store != null) return;

            FlatEarthEntitlement.Broker = this;
            _store = UnityIAPServices.StoreController();
            _store.OnStoreConnected += OnStoreConnected;
            _store.OnStoreDisconnected += OnStoreDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            _store.OnCheckEntitlement += OnCheckEntitlement;
            _store.ProcessPendingOrdersOnPurchasesFetched(true);

            try
            {
                await _store.Connect();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Google Play Billing connection failed: {exception.Message}");
                FailOutstanding("store connection failed");
            }
        }

        public void Purchase(Action<bool, string> onComplete)
        {
            if (!IsReady)
            {
                onComplete?.Invoke(false, "subscription unavailable");
                return;
            }

            _purchaseCompletion = onComplete;
            _store.PurchaseProduct(_product);
        }

        public void Restore(Action<bool, string> onComplete)
        {
            if (!IsReady)
            {
                onComplete?.Invoke(false, "subscription unavailable");
                return;
            }

            _restoreCompletion = onComplete;
            _store.CheckEntitlement(_product);
        }

        public void ManageSubscription()
        {
            var url = $"https://play.google.com/store/account/subscriptions?sku={ProductId}" +
                      $"&package={AppIdentity.AndroidPackageName}";
            Application.OpenURL(url);
        }

        private void OnStoreConnected()
        {
            _initialized = true;
            _store.FetchProducts(new List<ProductDefinition>
            {
                new(ProductId, ProductType.Subscription)
            });
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogWarning($"Google Play Billing disconnected: {failure}");
            FailOutstanding("store disconnected");
        }

        private void OnProductsFetched(List<Product> products)
        {
            _product = products.FirstOrDefault(product => product.definition.id == ProductId);
            if (_product == null)
            {
                FailOutstanding("subscription not configured in Google Play");
                return;
            }

            if (!string.IsNullOrWhiteSpace(_product.metadata?.localizedPriceString))
                _localizedPrice = _product.metadata.localizedPriceString;

            FlatEarthEntitlement.NotifyBrokerChanged();
            _store.CheckEntitlement(_product);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"Google Play subscription fetch failed: {failure.FailureReason}");
            FailOutstanding("subscription unavailable");
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (!ContainsProduct(order)) return;

            // Grant before confirming. Unity IAP then acknowledges the purchase to Google Play.
            _ownsProduct = true;
            FlatEarthEntitlement.NotifyBrokerChanged();
            _purchaseCompletion?.Invoke(true, "subscribed");
            _purchaseCompletion = null;
            _store.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (ContainsProduct(order) && _product != null)
                _store.CheckEntitlement(_product);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            if (!ContainsProduct(order)) return;
            _purchaseCompletion?.Invoke(false, order.Details ?? order.FailureReason.ToString());
            _purchaseCompletion = null;
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            if (!ContainsProduct(order)) return;
            _purchaseCompletion?.Invoke(false, "purchase pending approval");
            _purchaseCompletion = null;
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement.Product?.definition.id != ProductId) return;

            _ownsProduct = entitlement.Status is EntitlementStatus.FullyEntitled
                or EntitlementStatus.EntitledButNotFinished;

            if (entitlement.Status == EntitlementStatus.EntitledButNotFinished &&
                entitlement.Order is PendingOrder pending)
                _store.ConfirmPurchase(pending);

            FlatEarthEntitlement.NotifyBrokerChanged();
            if (_restoreCompletion == null) return;
            _restoreCompletion(_ownsProduct, _ownsProduct ? "restored" : "no active subscription");
            _restoreCompletion = null;
        }

        private static bool ContainsProduct(Order order) => order?.CartOrdered?.Items()
            .Any(item => item.Product?.definition.id == FlatEarthEntitlement.ProductId) == true;

        private void FailOutstanding(string message)
        {
            _purchaseCompletion?.Invoke(false, message);
            _restoreCompletion?.Invoke(false, message);
            _purchaseCompletion = null;
            _restoreCompletion = null;
        }

        private void OnDestroy()
        {
            if (_store == null) return;
            _store.OnStoreConnected -= OnStoreConnected;
            _store.OnStoreDisconnected -= OnStoreDisconnected;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
            _store.OnCheckEntitlement -= OnCheckEntitlement;
        }
    }
}

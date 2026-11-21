using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;

public sealed class PremiumOffer
{
    internal PremiumOffer(string id, string name, int gold, int bonusTokens)
    {
        Id = id;
        Name = name;
        Gold = gold;
        BonusTokens = bonusTokens;
    }

    public string Id { get; }
    public string Name { get; }
    public int Gold { get; }
    public int BonusTokens { get; }
}

public static class PremiumStore
{
    private const string CatalogLoading = "Chargement de la boutique…";
    private const string CatalogUnavailable = "Boutique indisponible. Réessaie en rouvrant cet onglet.";
    private const string RecoveryLoading = "Vérification des achats…";
    private const string FinalizationPending = "Achat en attente. Rouvre cet onglet pour reprendre la livraison.";
    private const string PurchaseComplete = "Achat crédité.";

    public static IReadOnlyList<PremiumOffer> Offers { get; } = Array.AsReadOnly(new[]
    {
        new PremiumOffer("canonpow_gold_500", "Bourse de pièces", 500, 0),
        new PremiumOffer("canonpow_gold_2000", "Coffre de pièces", 2000, 0),
        new PremiumOffer("canonpow_boost_pack", "Pack bonus", 1000, 3)
    });

    // Le Fake Store de Unity ne crédite jamais la progression hors des builds mobiles.
    private static readonly string storeName = Application.platform switch
    {
        RuntimePlatform.Android => GooglePlay.Name,
        RuntimePlatform.IPhonePlayer => AppleAppStore.Name,
        _ => null
    };

    private static readonly Dictionary<string, Product> products = new Dictionary<string, Product>();
    private static readonly HashSet<string> confirmations = new HashSet<string>();
    private static readonly HashSet<string> retryTransactions = new HashSet<string>();
    private static PurchasingController controller;
    private static string checkoutProductId;
    private static bool connecting;
    private static bool fetchingProducts;
    private static bool recovering;
    private static bool recovered;

    public static string Status { get; private set; } = storeName == null
        ? "Achats disponibles sur iOS et Android."
        : CatalogLoading;

    public static event Action Changed;

    public static string Price(string id)
    {
        return id != null && products.TryGetValue(id, out Product product)
            ? product.metadata.localizedPriceString
            : null;
    }

    public static bool CanBuy(string id)
    {
        return recovered && !recovering && checkoutProductId == null && confirmations.Count == 0
            && retryTransactions.Count == 0
            && !string.IsNullOrEmpty(Price(id));
    }

    // Appelé quand l'onglet est ouvert. L'écran reste utilisable pendant les requêtes du store.
    public static async void Prepare()
    {
        if (storeName == null || connecting || fetchingProducts || recovering || checkoutProductId != null) return;

        try
        {
            if (controller == null) controller = CreateController();
            if (controller.GetConnectionState(storeName) == ConnectionState.Connected)
            {
                if (products.Count == 0) FetchProducts(storeName);
                else if (!recovered || retryTransactions.Count != 0) FetchPurchases();
                return;
            }

            connecting = true;
            SetStatus(CatalogLoading);
            await controller.Connect(storeName);
        }
        catch (Exception exception)
        {
            fetchingProducts = false;
            recovering = false;
            recovered = false;
            SetStatus(CatalogUnavailable);
            Debug.LogWarning($"CanonPOW Achats : connexion impossible ({exception.GetType().Name}).");
        }
        finally
        {
            connecting = false;
        }
    }

    public static void Buy(string id)
    {
        if (!CanBuy(id)) return;

        checkoutProductId = id;
        SetStatus("Confirmation du paiement…");
        try
        {
            controller.PurchaseProduct(products[id]);
        }
        catch (Exception exception)
        {
            checkoutProductId = null;
            SetStatus("Impossible d’ouvrir le paiement. Réessaie.");
            Debug.LogWarning($"CanonPOW Achats : paiement indisponible ({exception.GetType().Name}).");
        }
    }

    private static PurchasingController CreateController()
    {
        var purchasing = new PurchasingController(storeName);
        purchasing.OnStoreConnected += FetchProducts;
        purchasing.OnStoreDisconnected += HandleDisconnect;
        purchasing.OnProductsFetched += HandleProductsFetched;
        purchasing.OnProductsFetchFailed += HandleProductsFetchFailed;
        purchasing.OnPurchasesFetched += HandlePurchasesFetched;
        purchasing.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
        purchasing.OnPurchasePending += Deliver;
        purchasing.OnPurchaseConfirmed += HandleConfirmed;
        purchasing.OnPurchaseFailed += HandlePurchaseFailed;
        purchasing.OnPurchaseDeferred += HandleDeferred;
        return purchasing;
    }

    private static void FetchProducts(string store)
    {
        fetchingProducts = true;
        recovered = false;
        var definitions = new List<ProductDefinition>(Offers.Count);
        foreach (PremiumOffer offer in Offers)
            definitions.Add(new ProductDefinition(offer.Id, ProductType.Consumable));
        SetStatus(CatalogLoading);
        controller.FetchProductsWithNoRetries(definitions, store);
    }

    private static void HandleDisconnect(string store, StoreConnectionFailureDescription failure)
    {
        products.Clear();
        foreach (string transaction in confirmations)
            retryTransactions.Add(transaction);
        confirmations.Clear();
        checkoutProductId = null;
        fetchingProducts = false;
        recovering = false;
        recovered = false;
        SetStatus(CatalogUnavailable);
    }

    private static void HandleProductsFetched(string store, List<Product> fetched)
    {
        fetchingProducts = false;
        products.Clear();
        foreach (Product product in fetched)
        {
            if (product.availableToPurchase && FindOffer(product.definition.id) != null)
                products[product.definition.id] = product;
        }
        FetchPurchases();
    }

    private static void HandleProductsFetchFailed(string store, ProductFetchFailed failure)
    {
        fetchingProducts = false;
        if (products.Count == 0) SetStatus(CatalogUnavailable);
    }

    private static void FetchPurchases()
    {
        recovering = true;
        SetStatus(RecoveryLoading);
        controller.FetchPurchases(storeName);
    }

    private static void HandlePurchasesFetched(string store, Orders orders)
    {
        recovering = false;
        recovered = true;
        // Unity livre les achats en attente via OnPurchasePending. Un nouvel appel
        // pendant la même session peut nécessiter de reprendre les livraisons échouées.
        var retry = new HashSet<string>(retryTransactions);
        retryTransactions.Clear();
        foreach (PendingOrder order in orders.PendingOrders)
        {
            if (retry.Contains(order.Info.TransactionID)) Deliver(store, order);
        }

        if (Status == RecoveryLoading) SetStatus(products.Count > 0 ? "" : CatalogUnavailable);
        else Changed?.Invoke();
    }

    private static void HandlePurchasesFetchFailed(string store, PurchasesFetchFailureDescription failure)
    {
        recovering = false;
        SetStatus(CatalogUnavailable);
    }

    private static void Deliver(string store, PendingOrder order)
    {
        if (store != storeName) return;

        string transaction = order.Info?.TransactionID;
        if (string.IsNullOrEmpty(transaction)
            || (string.IsNullOrEmpty(order.Info.Receipt) && string.IsNullOrEmpty(order.Info.Apple?.jwsRepresentation)))
        {
            checkoutProductId = null;
            SetStatus(FinalizationPending);
            return;
        }
        if (confirmations.Contains(transaction)) return;

        int balance;
        bool granted;
        try
        {
            using var hash = SHA256.Create();
            string key = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(store + ":" + transaction)));
            granted = RecordDelivery(key, order, out balance);
        }
        catch (Exception exception)
        {
            retryTransactions.Add(transaction);
            checkoutProductId = null;
            SetStatus(FinalizationPending);
            Debug.LogWarning($"CanonPOW Achats : livraison mise en attente ({exception.GetType().Name}).");
            return;
        }

        retryTransactions.Remove(transaction);
        checkoutProductId = null;
        confirmations.Add(transaction);
        SetStatus("Finalisation de l’achat…");
        try
        {
            controller.ConfirmPurchase(order);
        }
        catch (Exception exception)
        {
            confirmations.Remove(transaction);
            retryTransactions.Add(transaction);
            SetStatus(FinalizationPending);
            Debug.LogWarning($"CanonPOW Achats : confirmation en attente ({exception.GetType().Name}).");
        }
        if (granted) GameEvents.RaiseCoinsChanged(balance);
    }

    private static bool RecordDelivery(string key, PendingOrder order, out int balance)
    {
        UpgradeSaveData save = UpgradeSaveStore.Load();
        balance = save.coins;
        save.fulfilledPurchases ??= new List<string>();
        if (save.fulfilledPurchases.Contains(key)) return false;

        var items = order.CartOrdered.Items();
        if (items.Count == 0) throw new InvalidOperationException("Commande sans produit.");

        // Solde, jetons et identifiant sont écrits ensemble avant confirmation au store.
        checked
        {
            foreach (CartItem item in items)
            {
                var definition = item.Product.definition;
                PremiumOffer offer = FindOffer(definition.id);
                if (offer == null || item.Quantity <= 0 || definition.type != ProductType.Consumable)
                    throw new InvalidOperationException("Produit inconnu dans la commande.");

                save.coins += offer.Gold * item.Quantity;
                save.premiumDoubleRewards += offer.BonusTokens * item.Quantity;
            }
        }
        save.fulfilledPurchases.Add(key);
        UpgradeSaveStore.Save(save);
        balance = save.coins;
        return true;
    }

    private static void HandleConfirmed(string store, Order order)
    {
        string transaction = order.Info.TransactionID;
        confirmations.Remove(transaction);
        if (order is FailedOrder)
        {
            if (!string.IsNullOrEmpty(transaction)) retryTransactions.Add(transaction);
            SetStatus(FinalizationPending);
            return;
        }
        SetStatus(PurchaseComplete);
    }

    private static void HandlePurchaseFailed(string store, FailedOrder order)
    {
        checkoutProductId = null;
        SetStatus(order.FailureReason == PurchaseFailureReason.UserCancelled
            ? "Achat annulé."
            : "Le paiement n’a pas abouti. Tu peux réessayer.");
    }

    private static void HandleDeferred(string store, DeferredOrder order)
    {
        checkoutProductId = null;
        SetStatus("Paiement en attente de validation.");
    }

    private static PremiumOffer FindOffer(string id)
    {
        foreach (PremiumOffer offer in Offers)
        {
            if (offer.Id == id) return offer;
        }
        return null;
    }

    private static void SetStatus(string message)
    {
        Status = message;
        Changed?.Invoke();
    }
}

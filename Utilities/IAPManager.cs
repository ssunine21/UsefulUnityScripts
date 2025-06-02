using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
	public UnityAction OnBindInitialized;
	public List<IAPProduct> products = new();
	public static IAPManager Instance { get; private set; }

	private IStoreController _storeController;
	private IExtensionProvider _extensionProvider;
	private UnityAction<bool> purchasedCallback;

	private bool IsInit => _storeController != null && _extensionProvider != null;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
		InitUnityIAP();
	}

	private void InitUnityIAP()
	{
		if (_storeController != null) return;

		var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
		foreach (var product in products)
		{
			builder.AddProduct(product.proudctId, product.productType);
		}

		UnityPurchasing.Initialize(this, builder);
	}

	public void OnInitialized(IStoreController controller, IExtensionProvider extension)
	{
		_storeController = controller;
		_extensionProvider = extension;

		OnBindInitialized?.Invoke();
	}


	public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
	{
#if UNITY_EDITOR
		Debug.Log($"OnPurchaseFailed : {product.definition.id}-{failureDescription.message}");
#endif
	}

	public void OnInitializeFailed(InitializationFailureReason error)
	{
#if UNITY_EDITOR
		Debug.Log($"OnInitializeFailed : {error}");
#endif
	}

	public void OnInitializeFailed(InitializationFailureReason error, string message)
	{
#if UNITY_EDITOR
		Debug.Log($"OnInitializeFailed : {error}-{message}");
#endif
	}

	public string GetLocalizedPriceString(string productId)
	{
		var product = _storeController.products.WithID(productId);
		return product.metadata.localizedPriceString;
	}

	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
	{
#if UNITY_EDITOR
		Debug.Log($"PurchaseResult : {purchaseEvent.purchasedProduct.definition.id}");
#endif
		purchasedCallback?.Invoke(true);

		return PurchaseProcessingResult.Complete;
	}

	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
#if UNITY_EDITOR
		Debug.LogError($"PurchaseFailed : {product.definition.id}, {failureReason}");
#endif
	}

	public void Purchase(string productId, UnityAction<bool> callback)
	{
		if (!IsInit) return;
		purchasedCallback = callback;

		var product = _storeController.products.WithID(productId);
		if (product is { availableToPurchase: true })
		{
#if UNITY_EDITOR
			Debug.Log($"productID : {product.definition.id}");
#endif
			_storeController.InitiatePurchase(product);
		}
		else
		{
#if UNITY_EDITOR
			Debug.Log($"not productId {productId}");
#endif
			purchasedCallback?.Invoke(false);
		}
	}

	public bool HadPurchased(string productId)
	{
		if (!IsInit) return false;
		var product = _storeController.products.WithID(productId);
		return product is { hasReceipt: true };
	}


	[Serializable]
	public class IAPProduct
	{
		public string proudctId;
		public ProductType productType;
	}
}
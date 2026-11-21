using UnityEngine;
using System;

/*
 * Scene object that owns the single WebImageManager and runs its coroutines
 * Billboards ask this service for images
*/

public class BillboardImageService : MonoBehaviour
{
    [Tooltip("On: download each URL once and share it. Off: every billboard downloads separately.")]
    [SerializeField] private bool _useStoredImages = true;
    [Tooltip("Some image hosts (e.g. Wikimedia) reject requests that don't identify themselves.")]
    [SerializeField] private string _userAgent = "DIG4778-Lab13/1.0 (Unity student project)";

    private WebImageManager _webImageManager;

    private void Awake()
    {
        _webImageManager = new WebImageManager(_userAgent);
    }

    // Gets the image at webImage and sends it back through the callback
    public void RequestImage(string webImage, Action<Texture2D> callback)
    {
        if (string.IsNullOrWhiteSpace(webImage))
        {
            Debug.LogWarning("BillboardImageService: empty image URL.", this);
            return;
        }

        if (_webImageManager == null)
        {
            _webImageManager = new WebImageManager(_userAgent);
        }

        StartCoroutine(_useStoredImages
            ? _webImageManager.GetWebImage(webImage, callback)
            : _webImageManager.DownloadImage(webImage, callback));
    }
}
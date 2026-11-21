using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/*
 * Downloads images from the web and keeps each one after the first download, so every billboard that uses the same URL shares one texture instead of downloading it again
*/

public class WebImageManager
{
    private readonly Dictionary<string, Texture2D> _storedImages = new Dictionary<string, Texture2D>();
    private readonly Dictionary<string, List<Action<Texture2D>>> _waitingCallbacks = new Dictionary<string, List<Action<Texture2D>>>();
    private readonly string _userAgent;

    public WebImageManager(string userAgent)
    {
        _userAgent = userAgent;
    }

    // Always goes to the network (the lab's starter version)
	// Sends null back if the download fails
    public IEnumerator DownloadImage(string webImage, Action<Texture2D> callback)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(webImage))
        {
            if (!string.IsNullOrEmpty(_userAgent))
            {
                request.SetRequestHeader("User-Agent", _userAgent);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"image download failed ({webImage}): {request.error}");
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(DownloadHandlerTexture.GetContent(request));
        }
    }

    // If webImage is already stored, sends the stored texture back right away
    // If it is already downloading, waits for that download instead of starting another - otherwise downloads it once, stores it, and sends it to everyone who asked
    public IEnumerator GetWebImage(string webImage, Action<Texture2D> callback)
    {
        if (_storedImages.TryGetValue(webImage, out Texture2D storedImage))
        {
            callback?.Invoke(storedImage);
            yield break;
        }

        if (_waitingCallbacks.TryGetValue(webImage, out List<Action<Texture2D>> waiting))
        {
            waiting.Add(callback);
            yield break;
        }

        _waitingCallbacks[webImage] = new List<Action<Texture2D>> { callback };
        yield return DownloadImage(webImage, texture => OnImageDownloaded(webImage, texture));
    }

    // Stores the finished download and hands it to every waiting billboard
    private void OnImageDownloaded(string webImage, Texture2D texture)
    {
        if (texture != null)
        {
            _storedImages[webImage] = texture;
        }

        if (!_waitingCallbacks.TryGetValue(webImage, out List<Action<Texture2D>> callbacks))
        {
            return;
        }

        _waitingCallbacks.Remove(webImage);
        foreach (Action<Texture2D> callback in callbacks)
        {
            callback?.Invoke(texture);
        }
    }
}
using UnityEngine;

/*
 * A physical billboard that shows an image from the web
*/

[RequireComponent(typeof(Renderer))]
public class Billboard : MonoBehaviour
{
    [SerializeField] private BillboardImageService _imageService;
    [SerializeField] private string _webImage;
    [Tooltip("Stretch the billboard's width to match the picture's aspect ratio.")]
    [SerializeField] private bool _matchImageAspect = true;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (_imageService == null)
        {
            Debug.LogError("Billboard: assign the BillboardImageService.", this);
            return;
        }

        _imageService.RequestImage(_webImage, OnWebImageLoaded);
    }

    // Puts the downloaded texture on this billboard's material
    private void OnWebImageLoaded(Texture2D image)
    {
        if (image == null || _renderer == null)
        {
            return;
        }

        _renderer.material.mainTexture = image;

        if (_matchImageAspect && image.height > 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = scale.y * image.width / image.height;
            transform.localScale = scale;
        }
    }
}
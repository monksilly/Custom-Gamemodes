using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomGameModes.Controllers;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CustomGameModes;

[RequireComponent(typeof(Image))]
public class DynamicSpriteSwitcher : MonoBehaviour
{
    public enum DisplayMode
    {
        Random,
        Slideshow
    }

    public List<Sprite> sprites;
    public DisplayMode mode = DisplayMode.Random;
    public float displayDuration = 10f;
    public float fadeDuration = 1.5f;

    private Image _image;
    private Coroutine _cycleCoroutine;
    private string _key;

    private bool _coroRunning;

    public void Initialize()
    {
        _image = GetComponent<Image>();
        if (sprites == null || sprites.Count < 1)
        {
            LogManager.Debug("[DynamicSpriteSwitcher] No sprites assigned");
            enabled = false;
            return;
        }
        _key = $"{gameObject.transform.parent.name}:{gameObject.name}";
        Enable();
    }

    private void Enable()
    {
        if (sprites.Count < 2)
        {
            _image.sprite = sprites[0];
            return;
        }
        else
        {
            _image.sprite = sprites[1];
        }

        switch (mode)
        {
            case DisplayMode.Random:
                ShowRandomSprite();
                break;
            case DisplayMode.Slideshow:
                _cycleCoroutine = StartCoroutine(SlideshowLoop());
                break;
        }
    }

    private void OnDisable()
    {
        if (_cycleCoroutine == null) return;
        
        StopCoroutine(_cycleCoroutine);
        _cycleCoroutine = null;
    }

    private async void Update()
    {
        if (gameObject.activeSelf && _cycleCoroutine is null)
        {
            try
            {
                Initialize();
            }
            catch
            {
                /**/
            }
            await Task.Delay(5000);
        }
    }

    private void ShowRandomSprite()
    {
        var gmc = GameModeController.Instance;
        if (gmc is null)
        {
            LogManager.Error("[DynamicSpriteSwitcher] GameModeController.Instance is null");
            return;
        }

        if (gmc.LastChosenSpriteIndices.ContainsKey(_key))
            gmc.LastChosenSpriteIndices[_key] = -1;

        var lastIndex = gmc.LastChosenSpriteIndices[_key];
        var newIndex = Random.Range(0, sprites.Count);
        
        // Avoid repeat
        if (sprites.Count > 1)
        {
            var attempts = 0;
            while (newIndex == lastIndex && attempts < 5)
            {
                newIndex = Random.Range(0, sprites.Count);
                attempts++;
            }
        }
        
        _image.sprite = sprites[newIndex];
        gmc.LastChosenSpriteIndices[_key] = newIndex;
    }

    private IEnumerator SlideshowLoop()
    {
        if (!_coroRunning)
            _coroRunning = true;
        
        var index = 0;
        while (true)
        {
            // Fade out
            yield return StartCoroutine(FadeAlpha(1f, 0.2f));
            // Advance Index
            _image.sprite = sprites[index];
            // Fade in
            yield return StartCoroutine(FadeAlpha(0.2f, 1f));
            // Wait displayDuration
            yield return new WaitForSeconds(displayDuration);

            index = (index + 1) % sprites.Count;
        }
    }

    private IEnumerator FadeAlpha(float from, float to)
    {
        var elapsed = 0f;
        var c = _image.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / fadeDuration);
            c.r = Mathf.Lerp(from, to, t);
            c.g = Mathf.Lerp(from, to, t);
            c.b = Mathf.Lerp(from, to, t);
            _image.color = c;
            yield return null;
        }

        c.r = to;
        c.g = to;
        c.b = to;
        _image.color = c;
    }
}
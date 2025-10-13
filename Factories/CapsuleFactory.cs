using System;
using System.Collections.Generic;
using System.Linq;
using CustomGameModes.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CustomGameModes.Factories
{
    /// <summary>
    /// Responsible for creating a “capsule” button in the Play Menu 
    /// given an M_Gamemode instance, using a nested Builder for clarity.
    /// </summary>
    public class CapsuleFactory
    {
        public void CreateCapsuleForGameMode(
            M_Gamemode gameMode,
            string category = "default",
            List<Sprite> sprites = null,
            string author = null)
        {
            new Builder(gameMode)
                .WithCategory(category)
                .WithSprites(sprites)
                .WithAuthor(author)
                .Build();
        }

        /// <summary>
        /// Inner Builder class to split the creation steps.
        /// </summary>
        private class Builder
        {
            private readonly M_Gamemode _gameMode;
            private string _category = "default";
            private List<Sprite> _sprites;
            private string _author;

            // Transforms & GameObjects
            private Transform _contentTransform;
            private Transform _template;
            private Transform _categoryHolder;
            private Transform _newCapsule;

            public Builder(M_Gamemode gameMode)
            {
                _gameMode = gameMode;
            }

            public Builder WithCategory(string category)
            {
                _category = category;
                return this;
            }

            public Builder WithSprites(List<Sprite> sprites)
            {
                _sprites = sprites;
                return this;
            }

            public Builder WithAuthor(string author)
            {
                _author = author;
                return this;
            }

            public void Build()
            {
                if (!ValidateInputs()) return;
                LocateContentAndTemplate();
                PrepareCategoryHolder();
                InstantiateCapsule();
                ConfigureSprites();
                UpdateTextAndName();
                AttachUIComponents();
                ConfigureRoachCounter();
                ConfigureAuthor();
                RebuildLayouts();
            }

            private bool ValidateInputs()
            {
                if (GameModeController.Instance.currentScene != "Main-Menu") return false;
                
                if (_gameMode is not null) return true;
                
                LogManager.Error("[CapsuleFactory] gameMode is null; cannot create capsule.");
                return false;
            }

            private void LocateContentAndTemplate()
            {
                _contentTransform = GameObject.Find(
                    "Canvas - Screens/Screens/Canvas - Screen - Play/Play Menu/Play Pane/Tab Objects/Play Pane - Scroll View Tab - Custom/Viewport/Content"
                )?.transform;
                if (_contentTransform is null)
                {
                    LogManager.Error("[CapsuleFactory] Content transform not found.");
                    return;
                }

                var contentToCopyFrom =
                    GameObject.Find(
                        "Canvas - Screens/Screens/Canvas - Screen - Play/Play Menu/Play Pane/Tab Objects/Play Pane - Scroll View Tab - Endless Variant/Viewport/Content")
                        ?.transform;
                
                _template = contentToCopyFrom?.Find("Mode Selection Button - Endless");
                if (_template is null)
                {
                    LogManager.Error("[CapsuleFactory] Template not found.");
                }
            }

            private void PrepareCategoryHolder()
            {
                var customHolderParent = _contentTransform;
                if (_category != "default")
                {
                    _categoryHolder = customHolderParent.Find($"{_category} Holder");
                    if (_categoryHolder is null)
                    {
                        _categoryHolder = CreateCategoryHolder(_category).transform;
                        _categoryHolder.SetParent(customHolderParent);
                        _categoryHolder.localScale = Vector3.one;
                    }
                }
                else
                {
                    _categoryHolder = customHolderParent;
                }
            }

            private void InstantiateCapsule()
            {
                _newCapsule = Object.Instantiate(_template, _categoryHolder);
                Object.Destroy(_newCapsule.gameObject.GetComponent<UI_Gamemode_Button>());
                Object.Destroy(_newCapsule.gameObject.GetComponent<UI_CapsuleButton>());
                _newCapsule.gameObject.SetActive(true);
            }

            private void ConfigureSprites()
            {
                if (_sprites == null) return;
                
                var switcher = _newCapsule.gameObject.AddComponent<DynamicSpriteSwitcher>();
                switcher.sprites = _sprites;
                switcher.mode = DynamicSpriteSwitcher.DisplayMode.Slideshow;
                switcher.Initialize();
            }

            private void UpdateTextAndName()
            {
                _newCapsule.name = $"Mode Selection Button - {_gameMode.gamemodeName}";
                var titleText = _newCapsule.Find("Mode Name")?.GetComponent<TMP_Text>();
                if (titleText is not null)
                {
                    titleText.text = _gameMode.gamemodeName;
                }
            }

            private void AttachUIComponents()
            {
                var capsuleButton = _newCapsule.gameObject.AddComponent<UI_CapsuleButton>();
                capsuleButton.unlockIcon = _newCapsule.Find("Lock Image").GetComponent<Image>();
                capsuleButton.showDelayAnimation = 10;
                capsuleButton.unlockAchievement = "ACH_TUTORIAL";
                _newCapsule.gameObject.AddComponent<GiveMeMyUnlocksPls>();

                
                _newCapsule.gameObject.SetActive(false);
                var gmButton = _newCapsule.gameObject.AddComponent<UI_Gamemode_Button>();
                gmButton.gamemode = _gameMode;
                gmButton.title = _newCapsule.Find("Mode Name").GetComponent<TMP_Text>();
                gmButton.unlockText = _newCapsule.Find("Lock Image/Unlock Requirement").GetComponent<TextMeshProUGUI>();
                // var roachCounter = _newCapsule.Find("Roach Counter/Roaches").gameObject;
                
                var iLoveCopyingRandomThingsJustSoItWillWork = GameObject
                    .Find(
                        "Canvas - Screens/Screens/Canvas - Screen - Play/Play Menu/Play Pane/Tab Objects/Play Pane - Scroll View Tab - Endless/Viewport/Content/Mode Selection Button - Endless")
                    ?.transform;

                var origGamemodeButton = iLoveCopyingRandomThingsJustSoItWillWork?.GetComponent<UI_Gamemode_Button>();
                
                if (iLoveCopyingRandomThingsJustSoItWillWork is not null && origGamemodeButton is not null)
                {
                    gmButton.medalImage = origGamemodeButton.medalImage;
                    gmButton.medals = origGamemodeButton.medals;
                    gmButton.showMedals = false;
                }
                _newCapsule.gameObject.SetActive(true);
            }

            private class GiveMeMyUnlocksPls : MonoBehaviour
            {
                private UI_CapsuleButton capsuleButton;
                
                public string author;
                
                private void Awake()
                {
                    capsuleButton = GetComponent<UI_CapsuleButton>();
                    
                    ForceUnlock();
                }

                private void Update()
                {
                    ForceUnlock();
                }
                
                private void OnEnable()
                {
                    ForceUnlock();
                    if (string.IsNullOrEmpty(author)) return;
                    
                    var authorHolder = transform.Find("Author Name Holder");
                    var authorName = authorHolder?.Find("Author Name");
                    if (authorName is null) return;
                    
                    authorName.gameObject.GetComponent<TextMeshProUGUI>().text = author;
                }

                private void OnDisable()
                {
                    ForceUnlock();
                }

                private void ForceUnlock()
                {
                    if (capsuleButton is null) return;
                    if (string.IsNullOrEmpty(capsuleButton.unlockAchievement))
                        capsuleButton.unlockAchievement = "ACH_TUTORIAL";
                }
            }

            private void ConfigureRoachCounter()
            {
                var roachHolder = _newCapsule.Find("Roach Counter/Roaches");
                if (roachHolder is null) return;
                
                roachHolder.parent.gameObject.SetActive(true);
                roachHolder.gameObject.SetActive(true);
                var roachBank = roachHolder.gameObject.AddComponent<UI_RoachBankAmount>();
                roachBank.roachBankID = $"custom-{_gameMode.gamemodeName}";
            }

            private void ConfigureAuthor()
            {
                if (string.IsNullOrEmpty(_author)) return;
                var roachHolder = _newCapsule.Find("Roach Counter/Roaches");
                var authorNameHolderTemp = roachHolder?.parent;
                if (authorNameHolderTemp is null) return;

                var authorNameHolder = Object.Instantiate(authorNameHolderTemp, _newCapsule);
                authorNameHolder.name = "Author Name Holder";
                Object.Destroy(authorNameHolder.gameObject.GetComponent<Image>());
                var nameRect = authorNameHolder.GetComponent<RectTransform>();
                nameRect.anchoredPosition = new Vector2(2.5f, -55);

                var nameHolder = authorNameHolder.GetChild(0);
                nameHolder.name = "Author Name";
                Object.Destroy(nameHolder.gameObject.GetComponent<UI_RoachBankAmount>());

                var textComp = nameHolder.GetComponent<TextMeshProUGUI>();
                LogManager.Debug($"Text compotnent is: {textComp is not null}");
                if (textComp is null) return;
                
                nameHolder.gameObject.SetActive(true);
                
                textComp.fontSizeMin = 10;
                textComp.maxVisibleLines = 2;
                textComp.alignment = TextAlignmentOptions.MidlineLeft;
                textComp.color = new Color(1f, 1f, 1f, 1f);
                //textComp.outlineColor = new Color(0f, 0f, 0f, 1f);
                //textComp.outlineWidth = 0.35f;
                textComp.text = $"By: {_author}";
            }

            private void RebuildLayouts()
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _categoryHolder.GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _contentTransform.GetComponent<RectTransform>());
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    _contentTransform.parent.gameObject.GetComponent<RectTransform>());
            }

            private static GameObject CreateCategoryHolder(string category)
            {
                var categoryString = $"{category} Holder";
                var customHolder = new GameObject(categoryString);
                try
                {
                    var rect = customHolder.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.one;
                    rect.anchorMax = Vector2.one;
                    rect.anchoredPosition = Vector2.zero;
                    rect.pivot = Vector2.one * 0.5f;

                    var layout = customHolder.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 15f;
                    layout.childAlignment = TextAnchor.MiddleLeft;
                    layout.childForceExpandHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childControlHeight = false;
                    layout.childControlWidth = false;
                    layout.padding = new RectOffset(15, 20, 0, 0);

                    var fitter = customHolder.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                    fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

                    customHolder.transform.localScale = Vector3.one;
                    rect.localScale = Vector3.one;
                }
                catch (Exception e)
                {
                    LogManager.Error($"[CapsuleFactory] Error creating category holder: {e.Message}");
                }

                var content = GameObject.Find(
                    "Canvas - Screens/Screens/Canvas - Screen - Play/Play Menu/Play Pane/Tab Objects/Play Pane - Scroll View Tab - Endless/Viewport/Content"
                )?.transform;
                if (content is null)
                {
                    LogManager.Error("[CapsuleFactory] Cannot find Content for category.");
                    return customHolder;
                }

                var sectionTemplate = content.Find("Mode - Major Section Break - Endless");
                if (sectionTemplate is null)
                {
                    LogManager.Error("[CapsuleFactory] Section break template missing.");
                    return customHolder;
                }

                var clone = Object.Instantiate(sectionTemplate, customHolder.transform);
                clone.name = "Major Section Break.Custom-GameModes";
                clone.localScale = Vector3.one;
                clone.gameObject.SetActive(true);

                var titleTransform = clone.Find("Section Title/Mode Name");
                var textComp = titleTransform.GetComponent<TextMeshProUGUI>();
                if (textComp is not null)
                {
                    textComp.text = category;
                    textComp.faceColor = new Color(100f/255f, 100f/255f, 100f/255f, 1f);
                    textComp.fontSize = 28f;
                }

                var regular = clone.Find("Section Title/Roach Counter - Regular");
                var hard = clone.Find("Section Title/Roach Counter - Hard");
                Object.Destroy(regular?.gameObject);
                Object.Destroy(hard?.gameObject);

                return customHolder;
            }
        }
    }
}

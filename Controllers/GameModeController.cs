using CustomGameModes.Config;
using CustomGameModes.Factories;
using MonoMod.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WKLib;
using WKLib.API.Assets;
using static M_Gamemode;
using Random = UnityEngine.Random;

namespace CustomGameModes.Controllers;

public class GameModeController : MonoBehaviour
{
    public static GameModeController Instance;
    public Dictionary<string,int> LastChosenSpriteIndices { get; } = [];
    public string currentScene;
    
    private CapsuleFactory _capsuleFactory;
    private AssetService _assetService;

    private const string ConfigFileName = "config.json";
    private readonly Dictionary<ConfigKind, Func<string, JObject, Task>> _configHandlers;
    private const string GameModesRoot = "Gamemodes";

    // FISH
    const string PlayPane = "Canvas - Screens/Screens/Canvas - Screen - Play/Play Menu/Play Pane";
    
    // Loading
    private readonly Dictionary<string, float> _progressPhases = [];
    private int _expectedPhaseCount;
    private Transform _customLoadingGamemodes;

    private string _customRoot;
    
    // Categories
    private readonly Dictionary<string, GameObject> _customCategories = [];
    
    private enum ConfigKind
    {
        Standard,
        Premade,
        Unknown
    }

    public GameModeController()
    {
        // mapping each config.
        _configHandlers = new Dictionary<ConfigKind, Func<string, JObject, Task>>
        {
            { ConfigKind.Premade, HandlePremadeConfigAsync },
            { ConfigKind.Standard, HandleStandardConfigAsync }
        };
    }
    
    private void Awake()
    {
        if (Instance is null || Instance != this)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        
        LogManager.Info("[GameModeController] Awake()");
            
        // Instantiate all helpers / services:
        _assetService = new AssetService(Plugin.Instance.Context);
        _capsuleFactory = new CapsuleFactory();

        var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var customRoot = Path.Combine(assemblyFolder, GameModesRoot);
        if (!Directory.Exists(customRoot))
            Directory.CreateDirectory(customRoot);
        _customRoot = customRoot;
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;
        if (scene.name != "Main-Menu") return;
        SetupStuff();
        gameObject.SetActive(true);
        _expectedPhaseCount = 0;
    }

    private async void SetupStuff()
    {
        LogManager.Info("[GameModeController] Setting up menu!");
        
        // Prepare Loading Text!
        PrepareLoadingText();
        
        // Prepare the "Custom Game Modes" section in the Play Menu
        PreparePlaySection();
        
        // Show the Custom Button in menu
        PrepareMenuTab();
        
        // Enumerate each subfolder under "<AssemblyFolder>/Gamemodes"
        var baseFolder = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            GameModesRoot
        );

        var folders = Directory.GetDirectories(baseFolder);
        foreach (var folder in folders)
        {
            await ProcessGamemodeFolderAsync(Path.Combine(Assembly.GetExecutingAssembly().Location, folder));
        }
        
        // Scan Plugins folder
        // To make ThunderStore Uploaded gamemodes work
        var foldersPlugins = Directory.GetDirectories(BepInEx.Paths.PluginPath);
        foreach (var folder in foldersPlugins)
        {
            await ProcessGamemodeFolderAsync(Path.Combine(BepInEx.Paths.PluginPath, folder));
        }
    }

    private void PrepareLoadingText()
    {
        var container = GameObject.Find("Canvas - Main Menu").transform;
        var toClone = container.Find("Loading");
        var customLoading = Instantiate(toClone, container);
        customLoading.name = "Loading Gamemodes";
        var customRect = customLoading.GetComponent<RectTransform>();
        if (customRect)
        {
            customRect.offsetMin = new Vector2(-250, -360);
            customRect.offsetMax = new Vector2(250, -255);
            customRect.transform.localPosition = new Vector3(0, -306, 0);
        }
        var allLoaded = _progressPhases.All(x => x.Value >= 1f) 
                        && _progressPhases.Count == _expectedPhaseCount;
        customLoading.gameObject.SetActive(!allLoaded);
        _customLoadingGamemodes = customLoading;
    }
    
    private void UpdateLoadingText(Dictionary<string, float> progressPhases)
    {
        if (_customLoadingGamemodes is null || currentScene != "Main-Menu") return;

        var text = _customLoadingGamemodes.Find("Settings Title.01").GetComponent<TextMeshProUGUI>();
        var sb = new StringBuilder();
        var keys = progressPhases.Keys.ToList();
        for (var i = 0; i < keys.Count; i++)
        {
            var phase = keys[i];
            var p = progressPhases[phase];
            if (p >= 1f) continue;
            
            sb.AppendLine($"({i+1}/{_expectedPhaseCount}) {phase}: {(p * 100f):0}%");
        }
        text.fontSize = 25;
        text.text = sb.ToString();

        var allLoaded = progressPhases.All(x => x.Value >= 1f) 
                        && progressPhases.Count == _expectedPhaseCount;
        _customLoadingGamemodes.gameObject.SetActive(!allLoaded);
    }

    private async Task CheckIfAllLoaded()
    {
        var allLoaded = _progressPhases.All(x => x.Value >= 1f) 
                        && _progressPhases.Count == _expectedPhaseCount;
        if (allLoaded)
            _customLoadingGamemodes.gameObject.SetActive(false);
        else
        {
            await Task.Delay(1000);
            await Task.Run(CheckIfAllLoaded);
        }
        
        
    }

    private async Task ProcessGamemodeFolderAsync(string folderPath)
    {
        var configPath = Path.Combine(folderPath, ConfigFileName);
        if (!File.Exists(configPath))
        {
            //LogManager.Error($"[GameModeLoader] No {ConfigFileName} in {folderPath}");
            return;
        }

        string jsonText;
        try
        {
            jsonText = await File.ReadAllTextAsync(configPath);
        }
        catch (Exception e)
        {
            LogManager.Error($"[GameModeLoader] Failed reading {configPath}: {e.Message}");
            return;
        }

        JObject root;
        try
        {
            root = JObject.Parse(jsonText);
        }
        catch (Exception e)
        {
            LogManager.Error($"[GameModeLoader] Invalid JSON in {configPath}: {e.Message}");
            return;
        }
        
        // Detect config type/kind
        var kind = DetectConfigKind(root);
        if (!_configHandlers.TryGetValue(kind, out var handler))
        {
            LogManager.Error($"[GameModeLoader] No handler for config kind {kind}");
            return;
        }
        
        // dispatch handler
        await handler(folderPath, root);

    }
    
    #region Standard Handler

    private async Task HandleStandardConfigAsync(string folderPath, JObject root)
    {
        _expectedPhaseCount += 2;
        try
        {
            GamemodeConfig cfg;
            try
            {
                cfg = root.ToObject<GamemodeConfig>()!;
            }
            catch (Exception e)
            {
                LogManager.Error($"[GameModeLoader] Invalid Standard Config Structure: {e}");
                return;
            }

            if (cfg.regions == null)
            {
                LogManager.Error($"[GameModeLoader] Standard Config is missing regions");
                return;
            }
            
            var assetsFolder = Path.Combine(folderPath, "Assets");

            if (cfg.assetBundleFileName != null && !File.Exists(Path.Combine(assetsFolder, cfg.assetBundleFileName)))
            {
                LogManager.Error("[GameModeLoader] Standard Config is missing asset bundle file");
                return;
            }


            var gmName = cfg.gamemodeName;

            var levelPhaseKey = $"Loading Levels for \"{gmName}\"";
            _progressPhases[levelPhaseKey] = 0f;
            IProgress<float> levelProgress = new Progress<float>(p =>
            {
                _progressPhases[levelPhaseKey] = p;
                UpdateLoadingText(_progressPhases);
            });

            // Load Icons
            // TODO: Support more than one, and modes(slideshow, random)
            
            var capsuleSprite = _assetService.LoadPngAsSprite(
                Path.Combine(assetsFolder, cfg.capsuleIcon)
            );
            var screenSprite = _assetService.LoadPngAsSprite(
                Path.Combine(assetsFolder, cfg.screenIcon)
            );

            // Load bundle && levels if specified
            Dictionary<string, M_Level> allLevels = [];

            foreach (var inGameLevel in CL_AssetManager.GetFullCombinedAssetDatabase().levelAssets)
            {
                allLevels.TryAdd(inGameLevel.level.name, inGameLevel.level);
                // allLevels[inGameLevel.name] = inGameLevel.GetComponent<M_Level>();
            }

            if (!string.IsNullOrEmpty(cfg.assetBundleFileName))
            {
                var bundle = await _assetService.LoadBundleRelativeAsync(
                    Path.Combine(assetsFolder, cfg.assetBundleFileName),
                    levelProgress
                );
                var allLevelsFromBundle = await _assetService.LoadAllLevelsFromBundle(bundle, levelProgress);

                var levelsToAdd = allLevelsFromBundle.Where(x => !allLevels.Contains(x))
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value);
                allLevels.AddRange(levelsToAdd);
            }

            // build regions/subregions
            var regions = cfg.regions.Select(rc => BuildRegion(rc, allLevels)).ToList();

            var gmPhaseKey = $"Loading Gamemode for \"{gmName}\"";
            _progressPhases[gmPhaseKey] = 0f;
            IProgress<float> gmProgress = new Progress<float>(p =>
            {
                _progressPhases[gmPhaseKey] = p;
                UpdateLoadingText(_progressPhases);
            });

            var gm = ScriptableObject.CreateInstance<M_Gamemode>();
            gm.allowAchievements = false;
            gm.allowCheatedScores = false;
            gm.allowCheats = true;
            gm.allowLeaderboardScoring = true;
            gm.steamLeaderboardName = "";
            gm.allowHeightAchievements = false;
            gm.baseGamemode = true;
            gm.modeType = cfg.isEndless
                ? M_Gamemode.GameType.endlessPlaylist
                : M_Gamemode.GameType.playlist;
            gm.capsuleName = cfg.gamemodeName;
            gm.gamemodeName = cfg.gamemodeName;
            gm.introText = cfg.introText;
            gm.isEndless = cfg.isEndless;
            gm.hasPerks = cfg.hasPerks;
            gm.hasRevives = cfg.hasRevives;
            gm.gamemodeScene = "Game-Main";
            gm.roachBankID = $"custom-{cfg.gamemodeName}";
            gm.gamemodePanel = Resources.FindObjectsOfTypeAll<UI_GamemodeScreen_Panel>().FirstOrDefault(x => x.name == "Gamemode_Panel_Base");
            gm.loseScreen = Resources.FindObjectsOfTypeAll<UI_ScoreScreen>().FirstOrDefault(x => x.name == "ScorePanel_Standard_Death");
            gm.winScreen = Resources.FindObjectsOfTypeAll<UI_ScoreScreen>().FirstOrDefault(x => x.name == "ScorePanel_Standard_Win");
            gm.modeTags = [""];
            gm.unlockAchievement = "";
            //gm.playlistLevels = [];
            gm.gamemodeModule = new GamemodeModule_Standard
            {
                winScoreMultiplier = 1f
            };
            gm.startItems = [new M_Gamemode.SpawnItem { itemid = "Item_Hammer" }];
            gm.name = cfg.gamemodeName;
            gm.capsuleArt = capsuleSprite;
            gm.screenArt = screenSprite;
            gm.regions = regions;

            M_Gamemode.GameType GameType = M_Gamemode.GameType.single;
            switch(cfg.gameType.ToLower() ?? "single")
            {
                case "endless":
                    GameType = M_Gamemode.GameType.endlessPlaylist;
                    break;
                case "standard":
                    GameType = M_Gamemode.GameType.standard;
                    break;
                case "playlist":
                    GameType = M_Gamemode.GameType.playlist;
                    break;
                case "playlist-shuffle":
                    GameType = M_Gamemode.GameType.shuffledPlaylist;
                    break;
                case "single":
                    GameType = M_Gamemode.GameType.single;
                    break;
            }

            var numLevelsToLoad = 0;

            regions.ForEach(reg => reg.subregionGroups.ForEach(subRegGroup =>
                subRegGroup.subregions.ForEach(subReg => numLevelsToLoad += subReg.levelReferences.Count)));

            LogManager.Debug($"[Gamemode Builder] Will load {numLevelsToLoad} levels for {cfg.gamemodeName}");

            switch (numLevelsToLoad)
            {
                case 1:
                    gm.modeType = M_Gamemode.GameType.single;
                    gm.playlistLevelAssets = [regions[0].subregionGroups[0].subregions[0].levelReferences[0]];
                    LogManager.Debug($"[Gamemode Builder] Loading one singular level");
                    break;
                case > 1 when GameType == M_Gamemode.GameType.single:
                    gm.modeType = M_Gamemode.GameType.playlist;
                    regions.ForEach(reg => reg.subregionGroups.ForEach(subRegGroup =>
                        subRegGroup.subregions.ForEach(subReg => gm.playlistLevelAssets.AddRange(subReg.levelReferences))));
                    break;
                case > 1:
                    gm.modeType = GameType;
                    List<M_Level.LevelAssetHolder> levels = [];
                    var loadedLevels = 0;

                    foreach (var level in from region in regions
                                          from subRegionGroup in region.subregionGroups
                                          from subRegion in subRegionGroup.subregions
                                          from level in subRegion.levelReferences
                                          select level)
                    {
                        try
                        {
                            levels.Add(level);
                            loadedLevels++;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                            LogManager.Error($"[Gamemode Builder] Failed to load: {e.Message}");
                        }
                    }
                    LogManager.Debug($"[Gamemode Builder] Loaded {loadedLevels}/{numLevelsToLoad} levels");

                    //_regions.ForEach(reg => reg.subregionGroups.ForEach(subRegGroup =>
                    //    subRegGroup.subregions.ForEach(subReg => gm.playlistLevels.AddRange(subReg.levels))));
                    gm.playlistLevelAssets = levels;
                    break;
            }

            gm.levelsToGenerate = numLevelsToLoad;

            // GameObjects: find “World_Root” in the currently loaded objects
            var worldRoot = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => go.name == "World_Root");
            gm.gamemodeObjects = worldRoot is not null
                ? [worldRoot]
                : [];

            levelProgress.Report(1f);
            gmProgress.Report(1f);
            _capsuleFactory.CreateCapsuleForGameMode(gm, cfg.category ?? "default", author: cfg.author);
        }
        catch
        {
            LogManager.Error($"Couldn't process {folderPath}");
        }
    }
    
    #endregion
    
    #region Premade Gamemode Handler

    private async Task HandlePremadeConfigAsync(string folderPath, JObject root)
    {
        _expectedPhaseCount += 3;
        
        try
        {
            PremadeGamemodeConfig cfg;
            try
            {
                cfg = root.ToObject<PremadeGamemodeConfig>()!;
            }
            catch (Exception e)
            {
                LogManager.Error($"[GameModeLoader] Invalid Premade Config Structure: {e}");
                return;
            }

            // validate
            if (string.IsNullOrEmpty(cfg.assetBundle) ||
                string.IsNullOrEmpty(cfg.gamemodeName))
            {
                LogManager.Error("[GamemodeLoader] Premade config is missing required fields");
                return;
            }

            var assetsFolder = Path.Combine(folderPath, "Assets");
            if (!Directory.Exists(assetsFolder))
            {
                LogManager.Error("[GamemodeLoader] Premade Assets folder is missing");
                return;
            }

            

            // Loading Levels

            var gmName = cfg.gamemodeName;

            // Gamemode Loading Progress
            var assetPhaseProgress = $"Loading Assets for \"{gmName}\"";
            _progressPhases[assetPhaseProgress] = 0f;

            IProgress<float> assetProgress = new Progress<float>(p =>
            {
                _progressPhases[assetPhaseProgress] = p;
                UpdateLoadingText(_progressPhases);
            });

            var bundle =
                await _assetService.LoadBundleRelativeAsync(Path.Combine(assetsFolder, cfg.assetBundle), assetProgress);


            // Level Loading Progress
            var levelPhaseKey = $"Loading Levels for \"{gmName}\"";
            _progressPhases[levelPhaseKey] = 0f;

            IProgress<float> levelProgress = new Progress<float>(p =>
            {
                _progressPhases[levelPhaseKey] = p;
                UpdateLoadingText(_progressPhases);
            });

            await _assetService.LoadAllLevelsFromBundle(bundle, levelProgress);

            // Loading Gamemode Progress
            var gmPhaseKey = $"Loading Gamemode \"{gmName}\"";
            _progressPhases[gmPhaseKey] = 0f;

            IProgress<float> gmProgress = new Progress<float>(p =>
            {
                _progressPhases[gmPhaseKey] = p;
                UpdateLoadingText(_progressPhases);
            });

            var gm = await _assetService.LoadGameModeFromBundle(bundle, cfg.gamemodeName, gmProgress);
            gm.playlistLevelAssets = [.. gm.playlistLevels.Select(M_Level.LevelAssetHolder.GetNewHolderFromLevel)]; // Migrate old to new

            List<Sprite> capsuleArtsFinal = null;

            // optional/additional arts
            if (cfg.capsuleArts != null)
            {
                if (cfg.capsuleArts.Count == 1)
                {
                    var loadedArt = _assetService.LoadPngAsSprite(cfg.capsuleArts[0]);
                    if (loadedArt is null)
                        LogManager.Error(
                            $"Failed to load {cfg.capsuleArts[0]} for the capsule art! (gamemode: {cfg.gamemodeName})");
                    else
                        gm.capsuleArt = _assetService.LoadPngAsSprite(cfg.capsuleArts[0]);
                }
                else
                {
                    List<Sprite> capsuleArts = [];
                    capsuleArts.AddRange(
                        cfg.capsuleArts
                            .Select(capsuleArt => _assetService.LoadPngAsSprite(Path.Combine(assetsFolder, capsuleArt)))
                            .Where(x => x is not null));
                    capsuleArtsFinal = capsuleArts;
                }
            }

            //ApplyRandomArt(cfg.capsuleArts, assetsFolder, gm, a => gm.capsuleArt = _assetService.LoadPngAsSprite(a));
            ApplyRandomArt(cfg.screenArts, assetsFolder, a => gm.screenArt = _assetService.LoadPngAsSprite(a));

            levelProgress.Report(1f);
            gmProgress.Report(1f);
            _capsuleFactory.CreateCapsuleForGameMode(gm, cfg.category ?? "default", capsuleArtsFinal, cfg.author);
        }
        catch
        {
            LogManager.Error($"Couldn't process {folderPath}");
        }
    }
    
    #endregion
    
    #region Helper Functions
    
    private static ConfigKind DetectConfigKind(JObject j)
    {
        // Premade if it has both "assetBundle" and "gamemodeName"
        if (j["assetBundle"] != null && j["gamemodeName"] != null)
            return ConfigKind.Premade;
        
        // Standard if it has regions
        if (j["regions"] != null)
            return ConfigKind.Standard;
        
        // Unregistered config
        return ConfigKind.Unknown;
    }

    private static void ApplyRandomArt(List<string> paths, string baseFolder, Action<string> apply)
    {
        if (paths == null || paths.Count == 0) return;
        
        var choice = paths.Count == 1
            ? paths[0]
            : paths[Random.Range(0, paths.Count-1)];
        var fullPath = Path.Combine(baseFolder, choice);
        apply(fullPath);
    }

    private M_Region BuildRegion(RegionConfig rc, Dictionary<string, M_Level> levels)
    {
        var subregions = (rc.subregions ?? [])
            .Select(src =>
            {
                var matches = FindMatchingLevels(src, levels);
                var subregion = ScriptableObject.CreateInstance<M_Subregion>();
                subregion.subregionName = src.subregionName;
                subregion.name = src.subregionName;
                subregion.levelReferences = [.. matches.Select((l) => M_Level.LevelAssetHolder.GetNewHolderFromLevel(l))];
                subregion.subregionHeight = subregion.levelReferences.Sum(lv => lv.level.GetHeight());
                subregion.sessionEventLists = [.. subregion.levelReferences.SelectMany(lv => lv.level.sessionEventLists)];
                return subregion;
            })
            .ToList();

        var region = ScriptableObject.CreateInstance<M_Region>();
        region.regionName = rc.regionName;
        region.name = rc.regionName;

        List<M_Region.SubregionGroup> subRegGroups = [];
        foreach (var subregion in subregions)
        {
            subRegGroups.Add(new M_Region.SubregionGroup() { subregions = [subregion] });
        }

        // Wrap the subregions in SubregionGroups
        region.subregionGroups = subRegGroups;

        if (subregions.Count > 1 && subregions[0].levelReferences.Count > 1)
        {
            region.transitionLevels =
            [
                new M_Region.TransitionLevels() {
                    fromRegion = rc.regionName,
                    levels = [subregions[0].levelReferences[^1].level]
                }
            ];

            subregions[0].levelReferences.RemoveAt(subregions[0].levelReferences.Count - 1);
        }

        region.regionHeight = subregions.Sum(sr => sr.subregionHeight);


        // pick a default start level - M1_Intro_01 (if exists)
        var defaultPrefab = CL_AssetManager.GetFullCombinedAssetDatabase().levelAssets
            .FirstOrDefault(pref => pref.level.levelName == "M1_Intro_01");
        var defaultLevelComp = defaultPrefab?.level;
        if (defaultLevelComp is not null)
        {
            region.startLevels = [defaultLevelComp];
        }

        region.regionOrder = M_Region.RegionOrder.playlist;
        region.introText = rc.regionName;

        // Flatten all sessionEventLists from subregions
        region.sessionEventLists = [.. subregions.SelectMany(sr => sr.sessionEventLists)];
        return region;
    }

    private List<M_Level> FindMatchingLevels(SubregionConfig src, Dictionary<string, M_Level> levels)
    {
        if (levels != null && src.levelNameContains != null)
        {
            return [.. levels
                .Where(kv =>  kv.Key.IndexOf(src.levelNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(kv => kv.Value)];
        }
        if (src.levels != null)
        {
            return [.. src.levels.SelectMany(_assetService.FindLevelsByName)];
        }
        
        LogManager.Error($"[GamemodeLoader] No matching rule for subregion {src.subregionName}");
        return [];
    }
    
    #endregion

    private void PrepareMenuTab()
    {
        var menuTabs = GameObject.Find($"{PlayPane}/Tabs/Tab Buttons");
        if (menuTabs is null) return;
        
        var customButton = menuTabs.transform.Find("ModeButton_Custom")?.gameObject;
        if (customButton is null) return;
        
        customButton.gameObject.SetActive(true);
        
    }
    
    private void PreparePlaySection()
    {
        var content = GameObject.Find(
            $"{PlayPane}/Tab Objects/Play Pane - Scroll View Tab - Custom/Viewport/Content"
        )?.transform;
        if (content is null)
        {
            LogManager.Error("[GameModeController] Cannot find Play Menu content.");
            return;
        }

        for (var childIndex = 0; childIndex < content.childCount; childIndex++)
        {
            content.GetChild(childIndex).gameObject.SetActive(false);
        }
        
        var contentToCopyFrom =
            GameObject.Find(
                $"{PlayPane}/Tab Objects/Play Pane - Scroll View Tab - Endless Variant/Viewport/Content")?.transform;
        
        if (contentToCopyFrom is null)
        {
            LogManager.Error("[GameModeController] Cannot find content To Copy From.");
            return;
        }
        
        // Find an existing “Mode - Major Section Break - Endless” to clone:
        var template = contentToCopyFrom.Find("Mode - Major Section Break - Endless");
        if (template is null)
        {
           LogManager.Error("[GameModeController] Cannot find section‐break template.");
           return;
        }


        var clone = Instantiate(template, content);
        clone.name = "Major Section Break.Custom-GameModes";
        clone.localScale = Vector3.one;
        clone.gameObject.SetActive(false);
        
        clone.Find("Break.02")?.gameObject.SetActive(true);
        
        var customContentHolder = new GameObject("Custom Gamemodes Holder");
        var customRectTransform = customContentHolder.AddComponent<RectTransform>();
        customRectTransform.anchorMin = Vector2.one;
        customRectTransform.anchorMax = Vector2.one;
        customRectTransform.anchoredPosition = Vector2.zero;
        customRectTransform.pivot = Vector2.one * 0.5f;
        var customHorizontalLayout = customContentHolder.AddComponent<HorizontalLayoutGroup>();
        customHorizontalLayout.spacing = 15f/2f;
        customHorizontalLayout.childAlignment = TextAnchor.MiddleLeft;
        customHorizontalLayout.childForceExpandHeight = true;
        customHorizontalLayout.childForceExpandWidth = false;
        customHorizontalLayout.childControlWidth = false;
        customHorizontalLayout.childControlHeight = false;
        customHorizontalLayout.padding = new RectOffset(5, 10, 0, 0);
        customContentHolder.transform.SetParent(content.transform);
        var customContentSizeFitter = customContentHolder.AddComponent<ContentSizeFitter>();
        customContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        customContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
        customContentHolder.transform.localScale = Vector3.one;
        customRectTransform.localScale = Vector3.one;
        

        // Update its title text to “Custom Game Modes”:
        var titleTransform = clone.Find("Section Title/Mode Name");
        var textComp = titleTransform?.GetComponent<TextMeshProUGUI>();
        if (textComp is not null)
        {
            textComp.text = "Custom GameModes".ToUpper();
        }

        var roachCounterRegularTransform = clone.Find("Section Title/Roach Counter - Regular");
        var roachCounterHardTransform = clone.Find("Section Title/Roach Counter - Hard");

        Destroy(roachCounterRegularTransform?.gameObject?.GetComponent<UT_HardModeEnable>());
        Destroy(roachCounterRegularTransform?.GetChild(0).GetComponent<UI_RoachBankAmount>());
        Destroy(roachCounterRegularTransform?.gameObject);
        Destroy(roachCounterHardTransform?.gameObject);
    }
}

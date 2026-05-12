using BepInEx;
using CustomGameModes.Controllers;
using HarmonyLib;
using CustomGameModes.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using WKLib.API;
using UnityEngine.UI;
using WKLib.Core;
using WKLib;

namespace CustomGameModes;

[BepInIncompatibility("com.validaq.loadintolevel")]
[BepInDependency(WKLibPlugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string GUID = "com.monksilly.CustomGamemodes";
    public const string NAME = "CustomGamemodes";
    public const string VERSION = MyPluginInfo.PLUGIN_VERSION;
    
    public static Plugin Instance;
    public static WKLibAPI LibAPI = WKLibAPI.Create(NAME, GUID, "CustomGamemodes");
    
    public GameObject stuffHolder;

    
    private bool _isObjectInitialized;
    private bool _isGameModeManagerInitialized;
    private bool _isCustomItemsInitialized;
    
    private void Awake()
    {
        Instance = this;
        
        // Register to WKLib
        LibAPI.AddToModList(new ModListTab());
        LibAPI.AddWindow(WindowDeclarations.MainWin);
        
        // Plugin startup logic
        LogManager.Init(Logger);
        LogManager.Info($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        Harmony.CreateAndPatchAll(typeof(Patches.ModPatches));
    }




    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupMainObject();

        switch (scene.name)
        {
            case "Game-Main":
                SetupCustomItemHolder();
                break;
            case "Intro":
                SetupGameModeController();
                break;
        }
    }

    private void SetupMainObject()
    {
        if (_isObjectInitialized) return;
        _isObjectInitialized = true;
        
        stuffHolder = new GameObject("CustomGamemodesManager");
        DontDestroyOnLoad(stuffHolder);
    }
    
    private void SetupCustomItemHolder()
    {
        if (_isCustomItemsInitialized) return;
        _isCustomItemsInitialized = true;
        
        stuffHolder.AddComponent<SpawnController>();
    }

    private void SetupGameModeController()
    {
        if (_isGameModeManagerInitialized) return;
        _isGameModeManagerInitialized = true;
        
        stuffHolder.AddComponent<GameModeController>();
        stuffHolder.AddComponent<TextLoading>();
    }
    
    private void OnDestroy()
    {
        LibAPI.Destroy();
    }
}

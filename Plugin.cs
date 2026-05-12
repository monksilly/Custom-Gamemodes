using BepInEx;
using CustomGameModes.Controllers;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using WKLib.API;
using WKLib.Core;

namespace CustomGameModes;

[BepInIncompatibility("com.validaq.loadintolevel")]
[BepInDependency("com.monksilly.WKLib")]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance;
    public WKLibAPI Context;
    
    public GameObject stuffHolder;

    
    private bool _isObjectInitialized;
    private bool _isGameModeManagerInitialized;
    private bool _isCustomItemsInitialized;
    
    private void Awake()
    {
        Instance = this;
        
        // Register to WKLib
        Context = WKLibAPI.Create(MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_GUID);
        
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
    }
}
using CustomGameModes;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using WKLib.API.Input;
using WKLib.API.UI;

public class MainWindow : WKLibWindow
{
    public bool TestBool = false;
    public float TestValue = 20f;
    public KeyBind TestKeybind = new(KeyCode.P);
    
    public MainWindow()
    {
        isOpen = false;
    }
    
    public override void Draw(ImGui gui, bool isRootPanelOpen)
    {
        if (!isRootPanelOpen)
            return;
        
        if (!gui.BeginWindow("Custom Gamemodes Information", ref isOpen, new ImSize(400, 400), ImWindowFlag.None))
            return;

        gui.Separator("Basic Info");
        gui.Text("Hewwwo :3");
        if (gui.Checkbox(ref TestBool, "mrrow? "))
        {
            // On change do something
            Debug.Log("Changed bool!!");
        }

        if (TestBool)
        {
            gui.NumericEdit(ref TestValue, min: 0f, max: float.MaxValue, format: "0.0", flags: ImNumericEditFlag.Slider);
        }
        
        gui.SimpleKeybind("Reload Gamemodes", ref TestKeybind);
        
        gui.EndWindow();
    }
    
    // Gets called before Draw, useful for extra input
    public override void HandleInput(ImGui gui)
    {
        var pressedKey = InputUtility.GetKeyDown(KeyCode.LeftControl); // Also works on draw
        if (pressedKey)
            LogManager.Info("Left control is pressed!");
    }
}
using Imui.Controls;
using Imui.Core;

namespace CustomGameModes.UI;

public class ModListTab : WKLib.API.UI.ModTab
{
    public override string DisplayName => "Custom Gamemodes";
    
    public override void DrawSubMenu(ImGui gui)
    {
        if (gui.Button("Main Info"))
        {
            WindowDeclarations.MainWin.isOpen = !WindowDeclarations.MainWin.isOpen;
        } 
        
    }
}
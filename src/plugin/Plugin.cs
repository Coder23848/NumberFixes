using BepInEx;
using MoreSlugcats;
using UnityEngine;

namespace NumberFixes
{
    [BepInPlugin("com.coder23848.numberfixes", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable IDE0051 // Visual Studio is whiny
        private void OnEnable()
#pragma warning restore IDE0051
        {
            On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;

            On.HUD.Map.ClearSprites += Map_ClearSprites;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.Menu.ModdingMenu.ShutDownProcess += ModdingMenu_ShutDownProcess;

            Application.focusChanged += Application_focusChanged;
        }

        // screen resolution bug
        private void Application_focusChanged(bool obj)
        {
            if (obj)
            {
                FixScreenBug(RWCustom.Custom.rainWorld);
            }
        }
        private void FixScreenBug(RainWorld rainWorld)
        {
            if (rainWorld != null && rainWorld.options != null)
            {
                Debug.Log("[Number Fixes] Attempting to fix screen bug...");
                Screen.SetResolution((int)rainWorld.options.ScreenSize.x, (int)rainWorld.options.ScreenSize.y, false);
                Screen.fullScreen = rainWorld.options.fullScreen;
                Futile.instance.UpdateScreenWidth((int)rainWorld.options.ScreenSize.x);
            }
            else
            {
                Debug.Log("[Number Fixes] Unable to fix screen bug, cannot find screen resolution settings.");
            }
        }

        // map reveal texture memory leak
        private void Map_ClearSprites(On.HUD.Map.orig_ClearSprites orig, HUD.Map self)
        {
            orig(self);
            Debug.Log("[Number Fixes] Clearing map reveal texture...");
            self.DestroyTextures();
        }
        // RoomCamera textures memory leak
        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            Debug.Log("[Number Fixes] Clearing leakable camera textures...");
            foreach (RoomCamera i in self.cameras)
            {
                if (i.paletteTexture != null)
                {
                    Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.paletteTexture));
                    UnityEngine.Object.Destroy(i.paletteTexture);
                    i.paletteTexture = null;
                }
                if (i.snowLightTex != null)
                {
                    Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.snowLightTex));
                    UnityEngine.Object.Destroy(i.snowLightTex);
                    i.snowLightTex = null;
                }
                if (i.ghostFadeTex != null)
                {
                    Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.ghostFadeTex));
                    UnityEngine.Object.Destroy(i.ghostFadeTex);
                    i.ghostFadeTex = null;
                }
                if (i.fadeTexA != null)
                {
                    Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.fadeTexA));
                    UnityEngine.Object.Destroy(i.fadeTexA);
                    i.fadeTexA = null;
                }
                if (i.fadeTexB != null)
                {
                    Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.fadeTexB));
                    UnityEngine.Object.Destroy(i.fadeTexB);
                    i.fadeTexB = null;
                }
            }
        }
        // Remix thumbnails memory leak
        private void ModdingMenu_ShutDownProcess(On.Menu.ModdingMenu.orig_ShutDownProcess orig, Menu.ModdingMenu self)
        {
            Debug.Log("[Number Fixes] Clearing leakable mod thumbnails...");
            foreach (Menu.Remix.MenuModList.ModButton i in Menu.Remix.ConfigContainer.menuTab.modList.modButtons)
            {
                i._thumbnail?.Destroy();
            }
            orig(self);
        }

        // shortcut glow effect not displaying with "Show Underwater Shortcuts" enabled
        private void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
        {
            orig(self);
            for (int l = 0; l < self.room.shortcuts.Length; l++)
            {
                if (self.entranceSprites[l, 0] != null)
                {
                    if (ModManager.MMF && MMF.cfgShowUnderwaterShortcuts.Value)
                    {
                        self.camera.ReturnFContainer("GrabShaders").AddChild(self.entranceSprites[l, 1]);
                    }
                }
            }
        }
    }
}
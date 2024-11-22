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

            On.RainWorld.Update += RainWorld_Update;
        }

        // TODO: Texture leak in MoreSlugcats.BlizzardGraphics::TileTexUpdate
        // The leak happens for the room you end the cycle in. Would expect it to leak more often from the code; perhaps another mod partially fixes it? Modless testing is in order.

        // TODO: OOM from comically bad stowaway placement?

        // screen resolution bug
        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            //Debug.Log($"Test 1! Unity screen resolution: ({Screen.currentResolution.width}x{Screen.currentResolution.height}), Unity screen size: ({Screen.width}x{Screen.height}), Game screen resolution: ({self.options?.ScreenSize.x}x{self.options?.ScreenSize.y})");
            orig(self);
            if (self.options != null && (
                    Screen.width != (int)self.options.ScreenSize.x ||
                    Screen.height != (int)self.options.ScreenSize.y
                ))
            {
                Debug.Log("[Number Fixes] Fixing screen resolution bug...");
                Screen.SetResolution((int)self.options.ScreenSize.x, (int)self.options.ScreenSize.y, false);
                Screen.fullScreen = self.options.fullScreen;
                Futile.instance.UpdateScreenWidth((int)self.options.ScreenSize.x);
                Cursor.visible = !self.options.fullScreen;
            }
            //Debug.Log($"Test 2! Unity screen resolution: ({Screen.currentResolution.width}x{Screen.currentResolution.height}), Unity screen size: ({Screen.width}x{Screen.height}), Game screen resolution: ({self.options?.ScreenSize.x}x{self.options?.ScreenSize.y})");
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
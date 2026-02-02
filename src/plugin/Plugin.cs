using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System.Collections.Generic;
using UnityEngine;

namespace NumberFixes
{
    [BepInPlugin("com.coder23848.numberfixes", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource PluginLogger;

#pragma warning disable IDE0051 // Visual Studio is whiny
        private void OnEnable()
#pragma warning restore IDE0051
        {
            PluginLogger = Logger;

            On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;

            //On.HUD.Map.ClearSprites += Map_ClearSprites;
            //On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            //On.Menu.ModdingMenu.ShutDownProcess += ModdingMenu_ShutDownProcess;

            On.RainWorld.Update += RainWorld_Update;

            //IL.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;

            //IL.Lizard.SwimBehavior += Lizard_SwimBehavior;

            On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
            IL.GhostCreatureSedater.Update += GhostCreatureSedater_Update;
        }

        // creatures being removed improperly when an echo is nearby
        // This function also skips some creatures arbitrarily, since it modifies the list of creatures in the room while iterating over it. The bugs produced by this should be negligible, however, as it will run out of creatures to remove in (hopefully) just a few game ticks.
        private void GhostCreatureSedater_Update(ILContext il)
        {
            ILCursor cursor = new(il);
            if (cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<AbstractRoomNode.Type>(nameof(AbstractRoomNode.Type.Exit))
                ) &&
                cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<GhostCreatureSedater>(nameof(GhostCreatureSedater.creaturesMovedToDens)),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.room)),
                x => x.MatchCallvirt<Room>("get_abstractRoom"),
                x => x.MatchLdfld<AbstractRoom>(nameof(AbstractRoom.creatures)),
                x => x.MatchLdloc(2),
                x => x.Match(OpCodes.Callvirt), // I have no idea how to match an indexer and I don't think I really need to anyways
                x => x.MatchCallvirt(typeof(List<AbstractCreature>).GetMethod(nameof(List<AbstractCreature>.Contains)))
                ))
            {
                cursor.MoveAfterLabels();
                
                static void Delegate(GhostCreatureSedater self, int j)
                {
                    if (self.room.abstractRoom.creatures[j].realizedCreature != null)
                    {
                        Debug.Log("[Number Fixes] Destroying realized creature due to echo presence: " + self.room.abstractRoom.creatures[j].realizedCreature);
                        self.room.abstractRoom.creatures[j].realizedCreature.Destroy();
                    }
                }

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_2);
                cursor.EmitDelegate(Delegate);
            }
            else
            {
                PluginLogger.LogError("Failed to hook " + il.Method.Name + ": no match found.");
            }
        }

        // batflies being echo-proof
        private static void StaticWorld_InitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();
            StaticWorld.creatureTemplates[CreatureTemplate.Type.Fly.index].ghostSedationImmune = false;
            Debug.Log("[Number Fixes] Made batflies vulnerable to echo sedation.");
        }

        // empty region bugfix bug
        // appears to be fixed as of 1.11.6
        //private static List<int> tempRespawnCreatures = null;
        //private static void WorldLoader_GeneratePopulation(ILContext il)
        //{
        //    ILCursor startCursor = new(il);
        //    bool startCursorValid = startCursor.TryGotoNext(MoveType.Before,
        //        x => x.MatchLdcI4(0),
        //        x => x.MatchStloc(0));
        //    startCursor.MoveAfterLabels();
            
        //    ILCursor endCursor = startCursor.Clone();
        //    bool endCursorValid = endCursor.TryGotoNext(MoveType.Before,
        //        x => x.MatchLdloc(2),
        //        x => x.MatchLdcI4(0),
        //        x => x.MatchCeq(),
        //        x => x.MatchLdloc(3),
        //        x => x.MatchAnd(),
        //        x => x.MatchLdloc(1),
        //        x => x.MatchOr());
        //    endCursor.MoveAfterLabels();

        //    if (!startCursorValid || !endCursorValid)
        //    {
        //        PluginLogger.LogError("Failed to hook " + il.Method.Name + ": no match found.");
        //        return;
        //    }

        //    static void StartDelegate(WorldLoader self)
        //    {
        //        if (self.game.session is StoryGameSession storyGameSession)
        //        {
        //            if (tempRespawnCreatures != null)
        //            {
        //                Debug.Log("[Number Fixes] Respawn temp storage is already in use when it should be initialized. This should never happen!");
        //                return;
        //            }
        //            tempRespawnCreatures = [.. storyGameSession.saveState.respawnCreatures];
        //            //Debug.Log("[Number Fixes] Creatures to respawn: [" + string.Join(", ", storyGameSession.saveState.respawnCreatures) + "]");
        //            Debug.Log("[Number Fixes] Preparing for empty region check...");
        //        }
        //        else
        //        {
        //            Debug.Log("[Number Fixes] Not a story session, no creatures to respawn!");
        //        }
        //    }
        //    static void EndDelegate(WorldLoader self)
        //    {
        //        if (self.game.session is StoryGameSession storyGameSession)
        //        {
        //            if (tempRespawnCreatures == null)
        //            {
        //                Debug.Log("[Number Fixes] Respawn temp storage is uninitialized when it should be consumed. This should never happen!");
        //                return;
        //            }
        //            storyGameSession.saveState.respawnCreatures = [.. tempRespawnCreatures];
        //            tempRespawnCreatures = null;
        //            //Debug.Log("[Number Fixes] Creatures to respawn: [" + string.Join(", ", storyGameSession.saveState.respawnCreatures) + "]");
        //            Debug.Log("[Number Fixes] Successfully preserved creature respawns through empty region check.");
        //        }
        //        else
        //        {
        //            Debug.Log("[Number Fixes] Not a story session, no creatures to respawn!");
        //        }
        //    }
        //    startCursor.Emit(OpCodes.Ldarg_0);
        //    startCursor.EmitDelegate(StartDelegate);
        //    endCursor.Emit(OpCodes.Ldarg_0);
        //    endCursor.EmitDelegate(EndDelegate);
        //}

        // TODO: Texture leak in MoreSlugcats.BlizzardGraphics::TileTexUpdate
        // The leak happens for the room you end the cycle in. Would expect it to leak more often from the code; perhaps another mod partially fixes it? Modless testing is in order.

        // TODO: OOM from comically bad stowaway placement?

        // aquatic lizards not pathfinding out of water correctly (also fixed in the M4rblelous Entity Pack, but the two fixes shouldn't interfere with each other)
        // appears to be fixed as of 1.11.5
        //private static void Lizard_SwimBehavior(ILContext il)
        //{
        //    ILCursor cursor = new(il);
        //    if (cursor.TryGotoNext(MoveType.After,
        //        x => x.MatchLdarg(0),
        //        x => x.MatchCall(nameof(Creature), "get_mainBodyChunk"),
        //        x => x.MatchLdfld(nameof(BodyChunk), nameof(BodyChunk.pos)),
        //        x => x.MatchLdarg(0),
        //        x => x.MatchLdfld(nameof(UpdatableAndDeletable), nameof(UpdatableAndDeletable.room)),
        //        x => x.MatchLdarg(0),
        //        x => x.MatchCall(nameof(Creature), "get_mainBodyChunk"), 
        //        x => x.MatchLdfld(nameof(BodyChunk), nameof(BodyChunk.pos)),
        //        x => x.MatchCallvirt(typeof(Room).GetMethod(nameof(Room.MiddleOfTile), new System.Type[] { typeof(Vector2) }))))
        //    {
        //        static Vector2 Lizard_SwimBehaviorDelegate(Vector2 orig, Lizard self)
        //        {
        //            return self.room.MiddleOfTile(self.followingConnection.destinationCoord);
        //        }
        //        cursor.Emit(OpCodes.Ldarg_0);
        //        cursor.EmitDelegate(Lizard_SwimBehaviorDelegate);
        //    }
        //    else
        //    {
        //        Logger.LogError("Failed to hook Lizard.SwimBehavior: no match found.");
        //    }
        //}

        // screen resolution bug
        private static void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
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
        // appears to be fixed as of 1.11.5
        //private static void Map_ClearSprites(On.HUD.Map.orig_ClearSprites orig, HUD.Map self)
        //{
        //    orig(self);
        //    Debug.Log("[Number Fixes] Clearing map reveal texture...");
        //    self.DestroyTextures();
        //}

        // RoomCamera textures memory leak
        // appears to be fixed as of 1.11.5
        //private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        //{
        //    orig(self);
        //    Debug.Log("[Number Fixes] Clearing leakable camera textures...");
        //    foreach (RoomCamera i in self.cameras)
        //    {
        //        if (i.paletteTexture != null)
        //        {
        //            Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.paletteTexture));
        //            UnityEngine.Object.Destroy(i.paletteTexture);
        //            i.paletteTexture = null;
        //        }
        //        if (i.snowLightTex != null)
        //        {
        //            Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.snowLightTex));
        //            UnityEngine.Object.Destroy(i.snowLightTex);
        //            i.snowLightTex = null;
        //        }
        //        if (i.ghostFadeTex != null)
        //        {
        //            Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.ghostFadeTex));
        //            UnityEngine.Object.Destroy(i.ghostFadeTex);
        //            i.ghostFadeTex = null;
        //        }
        //        if (i.fadeTexA != null)
        //        {
        //            Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.fadeTexA));
        //            UnityEngine.Object.Destroy(i.fadeTexA);
        //            i.fadeTexA = null;
        //        }
        //        if (i.fadeTexB != null)
        //        {
        //            Debug.Log("[Number Fixes] Destroying " + nameof(RoomCamera.fadeTexB));
        //            UnityEngine.Object.Destroy(i.fadeTexB);
        //            i.fadeTexB = null;
        //        }
        //    }
        //}
        // Remix thumbnails memory leak
        // Appears to be fixed as of 1.11.5, probably?
        //private static void ModdingMenu_ShutDownProcess(On.Menu.ModdingMenu.orig_ShutDownProcess orig, Menu.ModdingMenu self)
        //{
        //    Debug.Log("[Number Fixes] Clearing leakable mod thumbnails...");
        //    foreach (Menu.Remix.MenuModList.ModButton i in Menu.Remix.ConfigContainer.menuTab.modList.modButtons)
        //    {
        //        i._thumbnail?.Destroy();
        //    }
        //    orig(self);
        //}

        // shortcut glow effect not displaying with "Show Underwater Shortcuts" enabled (also fixed in MergeFix, but the two fixes shouldn't interfere with each other)
        private static void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
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
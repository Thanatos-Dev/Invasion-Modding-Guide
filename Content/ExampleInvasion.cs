using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace InvasionModdingGuide.Content
{
    public class ExampleInvasion : ModSystem
    {
        public static bool isActive = false;
        public static int killCount = 0; // How many enemies have been killed so far
        public static int killsNeeded = 120 - 40; // How many total kills are needed to end the invasion. We subtract 40 here so we can add 40 per player for multiplayer scaling

        // The 2 methods below are needed to make sure the invasion will continue & remember the current kill count when exiting the world
        public override void SaveWorldData(TagCompound tag)
        {
            tag.Add("InvasionActive", isActive);

            tag.Add("CurrentKillCount", killCount);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            if (tag.ContainsKey("InvasionActive"))
            {
                isActive = tag.GetBool("InvasionActive");
            }

            if (tag.ContainsKey("CurrentKillCount"))
            {
                killCount = tag.GetInt("CurrentKillCount");
            }
        }

        // This method is where we can check the kill count to see if the invasion has been completed & set up extra stuff like custom visuals, apply buffs, etc.
        public override void PreUpdateWorld()
        {
            if (killCount >= killsNeeded)
            {
                isActive = false;
                killCount = 0;
                
                // This sends the chat message for multiplayer & singleplayer respectively
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
                string key = "Example Invasion has been defeated!";
                Color messageColor = new Color(255, 255, 255);

                if (Main.netMode == NetmodeID.Server)
                {
                    Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), messageColor);
                }
                else if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.NewText(Language.GetTextValue(key), messageColor);
                }
            }

            // This is an example of custom visuals. Here we set the sun texture to the alternate sunglasses version while the invasion is active
            if (isActive)
            {
                TextureAssets.Sun = TextureAssets.Sun2;
            }
            else if (!isActive)
            {
                TextureAssets.Sun = Main.Assets.Request<Texture2D>("Images/Sun");
            }
        }

        // This message resets the sun texture if the mod is disabled
        public override void Unload()
        {
            TextureAssets.Sun = Main.Assets.Request<Texture2D>("Images/Sun");
        }

        // The 2 methods below sync our variables between clients and server
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(killCount);
            writer.Write(isActive);
        }

        public override void NetReceive(BinaryReader reader)
        {
            killCount = reader.ReadInt32();
            isActive = reader.ReadBoolean();
        }
    }
    
    // This class handles enemy spawning
    public class ExampleInvasionSpawnRates : GlobalNPC
    {
        public static List<int> invasionEnemies = new List<int>()
        {
            ModContent.NPCType<InvasionZombie>(),
            ModContent.NPCType<InvasionBat>(),
        };

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (ExampleInvasion.isActive == true)
            {
                // Here we can check for special requirements to be met for the enemies to spawn. For example, a biome or time of day
                if (player.ZoneForest && Main.IsItDay())
                {
                    spawnRate = 100; // Make this value lower for faster spawns & higher for slower spawns
                    maxSpawns = 25;
                }
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (ExampleInvasion.isActive == true)
            {
                // Again, we'll check for special requirements
                if (spawnInfo.Player.ZoneForest && Main.IsItDay())
                {
                    // All spawn chances added together should equal 100

                    pool.Add(ModContent.NPCType<InvasionZombie>(), 50);

                    pool.Add(ModContent.NPCType<InvasionBat>(), 50);

                    // This removes vanilla enemies from the spawn pool
                    for (int type = 0; type < NPCLoader.NPCCount; type++)
                    {
                        if (!invasionEnemies.Contains(type))
                        {
                            pool.Remove(type);
                        }
                    }
                }
            }
        }
    }

    // Here we can set custom music to play during the invasion. Audio files must be in a folder named "Music"
    public class ExampleInvasionMusic : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.Event;

        public override bool IsSceneEffectActive(Player player)
        {
            if (ExampleInvasion.isActive == true)
            {
                // We'll check for special requirements once again
                if (player.ZoneForest && Main.IsItDay())
                {
                    return true;
                }
            }

            return false;
        }

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Content/Music/ExampleInvasionMusic"); // If you want to play vanilla music, use "MusicID" instead of "MusicLoader"
    }

    // We need a ModBiome to be able to have a filter for the invasion in the bestiary
    public class ExampleInvasionBiome : ModBiome
    {
        public override string BestiaryIcon => base.BestiaryIcon; // Icon must be 30x30 and be named ending with "_Icon". For example, "ExampleInvasionBiome_Icon.png"
        public override SceneEffectPriority Priority => SceneEffectPriority.Event;
    }

    // Here we actually draw the progress bar UI that we set up in the mod's main script
    internal class ExampleInvasionProgressUI : ModSystem
    {
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));

            if (ExampleInvasion.isActive)
            {
                int index = layers.FindIndex(layer => layer is not null && layer.Name.Equals("Vanilla: Inventory"));
                LegacyGameInterfaceLayer NewLayer = new LegacyGameInterfaceLayer("Eventful: Buried Barrage UI",
                    delegate
                    {
                        InvasionModdingGuide.Instance.DrawExampleInvasionUI(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI);
                layers.Insert(index, NewLayer);
            }
        }
    }
}
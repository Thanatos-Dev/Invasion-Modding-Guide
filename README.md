# ModSystem Setup
The first thing we need to do is set up a ModSystem, this will handle almost everything for our invasion.

We need 3 variables:

```csharp
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
    }
}
```

# World Data
Now we need to make sure the invasion will continue and remember the current kill count when exiting the world.

```csharp
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
```

The next method is where we can check the kill count to see if the invasion has been completed and set up extra stuff like custom visuals, apply buffs, etc.

```csharp
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
```

We should also reset the sun texture if the mod is disabled.

```csharp
public override void Unload()
{
    TextureAssets.Sun = Main.Assets.Request<Texture2D>("Images/Sun");
}
```

# Networking and Multiplayer Scaling
It's important to sync our variables between clients and server to ensure our invasion works correctly in multiplayer.

```csharp
public override void NetSend(BinaryWriter writer)
{
    writer.Write(killCount);
    writer.Write(isActive);

    NetMessage.SendData(MessageID.WorldData);
}

public override void NetReceive(BinaryReader reader)
{
    killCount = reader.ReadInt32();
    isActive = reader.ReadBoolean();
}
```

# Enemies
Now that the invasion itself is set up, we need to have our custom enemies spawn while the invasion is active. This class can be put in the same .cs file that ExampleInvasion is in.

```csharp
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
```

Here we have a basic example enemy for the invasion:

```csharp
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InvasionModdingGuide.Content
{
    public class InvasionZombie : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
        }

        public override void SetDefaults()
        {
            NPC.width = 34;
            NPC.height = 46;
            NPC.damage = 10;
            NPC.defense = 5;
            NPC.lifeMax = 25;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = NPCAIStyleID.Fighter;

            AIType = NPCID.GoblinScout; // This makes the enemy not try to despawn during the day
            AnimationType = NPCID.Zombie;

            // This correlates this enemy with Example Invasion for the bestiary
            SpawnModBiomes = [ModContent.GetInstance<ExampleInvasionBiome>().Type];
        }

        public override void OnKill()
        {
            if (ExampleInvasion.isActive == true)
            {
                ExampleInvasion.killCount++; // Counts up the invasion's kill count

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }
    }
}
```

# Summoning and Disabling
We can now make an item to summon the invasion.

```csharp
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InvasionModdingGuide.Content
{
    public class InvasionSummon : ModItem
    {
        // The 2 methods below are for journey mode
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossSpawners;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.noMelee = true;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = Item.useAnimation = 45;
            Item.UseSound = SoundID.Roar;
        }

        public override bool CanUseItem(Player player)
        {
            if (ExampleInvasion.isActive)
            {
                return false;
            }

            // Check for the same special requirements as in the invasion script
            if (!player.ZoneForest || !Main.IsItDay())
            {
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            ExampleInvasion.isActive = true;
            ExampleInvasion.killsNeeded += 40 * (Main.player.Where(p => p.active).Count() - 1); // Adds 40 enemies for each player

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
            string key = "Example Invasion has started!";
            Color messageColor = new Color(255, 75, 75);

            if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), messageColor);
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(Language.GetTextValue(key), messageColor);
            }

            return true;
        }
    }
}
```

We can also make an item to disable the invasion. The main differences are in the `CanUseItem` and `UseItem` methods.

```csharp
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InvasionModdingGuide.Content
{
    public class InvasionDisable : ModItem
    {
        // The 2 methods below are for journey mode
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
        }

        public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
        {
            itemGroup = ContentSamples.CreativeHelper.ItemGroup.BossSpawners;
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.noMelee = true;
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.autoReuse = false;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = Item.useAnimation = 45;
            Item.UseSound = SoundID.Item92;
        }

        public override bool CanUseItem(Player player)
        {
            if (ExampleInvasion.isActive)
            {
                return true;
            }

            return false;
        }

        public override bool? UseItem(Player player)
        {
            ExampleInvasion.isActive = false;
            ExampleInvasion.killCount = 0;

            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.WorldData);
            string key = "Example Invasion has been disabled!";
            Color messageColor = new Color(100, 255, 100);

            if (Main.netMode == NetmodeID.Server)
            {
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), messageColor);
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(Language.GetTextValue(key), messageColor);
            }

            return true;
        }
    }
}
```

# Music
If we want our invasion to play custom music, we need to make a class that inherits from `ModSceneEffect`. Make sure to put your audio file in a folder named "Music".

```csharp
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
```

# Bestiary Filter
For our invasion to have a filter in the bestiary, we can make a class that inherits from `ModBiome`. This won't be an actual biome, this is just so the bestiary can filter our invasion's enemies. Don't forget to include a sprite for the bestiary filter icon. the image file must be 30x30 and be named ending with "_Icon". For example, "ExampleInvasionBiome_Icon.png"

```csharp
public class ExampleInvasionBiome : ModBiome
{
    public override string BestiaryIcon => base.BestiaryIcon;
    public override SceneEffectPriority Priority => SceneEffectPriority.Event;
}
```

# Progress Bar UI
Lastly, we need the progress bar UI. This part will be done in your mod's main class that inherits from `Mod`.

```csharp
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria;
using Terraria.ModLoader;
using InvasionModdingGuide.Content;

namespace InvasionModdingGuide
{
	public class InvasionModdingGuide : Mod
	{
        public static InvasionModdingGuide Instance;

        public InvasionModdingGuide()
        {
            Instance = this;
        }

        // This method is for drawing the actual UI for the progress bar. Doing it this way only requires 1 sprite for the icon
        public void DrawExampleInvasionUI(SpriteBatch spriteBatch)
        {
            if (ExampleInvasion.isActive)
            {
                // Check for the same special requirements as in the invasion script
                if (Main.LocalPlayer.ZoneForest && Main.IsItDay())
                {
                    const string invasionName = "Example Invasion";
                    const int descWidth = 250; // Change this to adjust the description width depending on the length of your text

                    const float Scale = 1;
                    const float Alpha = 0.5f;
                    const int InternalOffset = 6;
                    const int OffsetX = 20;
                    const int OffsetY = 20;
                    const int InfoOffsetY = 2;

                    int progress = (int)(100 * ExampleInvasion.killCount / ((float)ExampleInvasion.killsNeeded));

                    Texture2D EventIcon = Assets.Request<Texture2D>("Content/ExampleInvasionBiome_Icon", AssetRequestMode.ImmediateLoad).Value;
                    Color descColor = new Color(32, 56, 128); // Change this to adjust the color of the description box
                    Color progressBarColor = new Color(255, 241, 51); // Change this to adjust the color of the progress bar

                    int width = (int)(200f * Scale);
                    int height = (int)(50f * Scale);

                    Rectangle progressBarBackground = Utils.CenteredRectangle(new Vector2(Main.screenWidth - OffsetX - 100f, Main.screenHeight - OffsetY - 23f), new Vector2(width, height));
                    Utils.DrawInvBG(spriteBatch, progressBarBackground, new Color(63, 65, 151, 255) * 0.785f);

                    string waveText = "Cleared " + progress + "%";
                    Utils.DrawBorderString(spriteBatch, waveText, new Vector2(progressBarBackground.Center.X, progressBarBackground.Y + 5), Color.White, Scale * 0.85f, 0.5f, -0.1f);
                    Rectangle waveProgressBar = Utils.CenteredRectangle(new Vector2(progressBarBackground.Center.X, progressBarBackground.Y + progressBarBackground.Height * 0.75f), TextureAssets.ColorBar.Size());

                    var waveProgressAmount = new Rectangle(0, 0, (int)(TextureAssets.ColorBar.Width() * 0.01f * MathHelper.Clamp(progress, 0f, 100f)), TextureAssets.ColorBar.Height());
                    var offset = new Vector2((waveProgressBar.Width - (int)(waveProgressBar.Width * Scale)) * 0.5f, (waveProgressBar.Height - (int)(waveProgressBar.Height * Scale)) * 0.5f - InfoOffsetY);

                    spriteBatch.Draw(TextureAssets.ColorBar.Value, waveProgressBar.Location.ToVector2() + offset, null, Color.White * Alpha, 0f, new Vector2(0f), Scale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(TextureAssets.ColorBar.Value, waveProgressBar.Location.ToVector2() + offset, waveProgressAmount, progressBarColor, 0f, new Vector2(0f), Scale, SpriteEffects.None, 0f);

                    Vector2 descSize = new Vector2(descWidth, 50) * Scale * 0.75f;
                    Rectangle barrierBackground = Utils.CenteredRectangle(new Vector2(Main.screenWidth - OffsetX - 100f, Main.screenHeight - OffsetY - 19f), new Vector2(width, height));
                    Rectangle descBackground = Utils.CenteredRectangle(new Vector2(barrierBackground.Center.X, barrierBackground.Y - InternalOffset - descSize.Y * 0.5f), descSize * 0.9f);
                    Utils.DrawInvBG(spriteBatch, descBackground, descColor * Alpha);

                    int descOffset = (descBackground.Height - (int)(32f * Scale)) / 2;
                    var icon = new Rectangle(descBackground.X + descOffset + 7, descBackground.Y + descOffset, (int)(32 * Scale), (int)(32 * Scale));
                    spriteBatch.Draw(EventIcon, icon, Color.White);
                    Utils.DrawBorderString(spriteBatch, invasionName, new Vector2(barrierBackground.Center.X - 5, barrierBackground.Y - InternalOffset - descSize.Y * 0.5f), Color.White, 0.8f, 0.3f, 0.4f);
                }
            }
        }
    }
}
```

Now that we have the drawing code, we need to make it actually appear on screen. This class can also be put in the same .cs file that ExampleInvasion is in.

```csharp
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
```
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
            ExampleInvasion.killsNeeded += 25 * (Main.player.Where(p => p.active).Count() - 1); // Adds 25 enemies for each player

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
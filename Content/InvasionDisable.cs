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
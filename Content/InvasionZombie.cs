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

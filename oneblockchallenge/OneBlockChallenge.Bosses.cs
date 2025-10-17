using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Terraria;
using Terraria.ID;
using Terraria.Utilities;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace OneBlockChallenge
{
    public partial class OneBlockChallenge
    {

        #region 击败生物检测 
        private void OnNPCKilled(NpcKilledEventArgs args)
        {
            int npcId = args.npc.type;
            if (IsBoss(npcId))
            {
                string bossName = (string)Lang.GetNPCName(npcId);
                if (!Config.BossDefeated[bossName])
                {
                    List<int> newTileTypes = Config.BossTile.ContainsKey(bossName) ? Config.BossTile[bossName] : new List<int>();
                    foreach (var tileType in newTileTypes)
                    {
                        RandomTilePool.Add(tileType);
                    }

                    Config.BossDefeated[bossName] = true;

                    if (!Config.BossDefeated["邪恶BOSS"] && (bossName == "世界吞噬怪" || bossName == "克苏鲁之脑"))
                    {
                        newTileTypes = Config.BossTile["邪恶BOSS"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BossDefeated["邪恶BOSS"] = true;
                        if (Config.Broadcast["邪恶BOSS"])
                        {
                            TShock.Utils.Broadcast("邪恶BOSS 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }
                    if (!Config.BossDefeated["一王"] && (CheckBossDowned("双子魔眼") || CheckBossDowned("毁灭者") || CheckBossDowned("机械骷髅王")))
                    {
                        newTileTypes = Config.BossTile["一王"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BossDefeated["一王"] = true;
                        if (Config.Broadcast["一王"])
                        {
                            TShock.Utils.Broadcast("一王 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }

                    if (!Config.BossDefeated["三王"] && CheckBossDowned("双子魔眼") && CheckBossDowned("毁灭者") && CheckBossDowned("机械骷髅王"))
                    {
                        newTileTypes = Config.BossTile["三王"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BossDefeated["三王"] = true;
                        if (Config.Broadcast["三王"])
                        {
                            TShock.Utils.Broadcast("三王 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }
                    Config.Write();
                    if (Config.Broadcast["所有BOSS"])
                    {
                        TShock.Utils.Broadcast($"{bossName} 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                    }

                }

                if (!Config.BossDefeated["双子魔眼"] && CheckBossDowned("双子魔眼") && (bossName == "激光眼" || bossName == "魔焰眼"))
                {
                    List<int> newTileTypes = Config.BossTile.ContainsKey(bossName) ? Config.BossTile[bossName] : new List<int>();
                    newTileTypes = Config.BossTile["双子魔眼"];

                    foreach (var tileType in newTileTypes)
                    {
                        RandomTilePool.Add(tileType);
                    }
                    Config.BossDefeated["双子魔眼"] = true;
                    Config.Write();
                    TShock.Utils.Broadcast("双子魔眼 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                }
            }
        }
        #endregion

        #region 检测boss击败
        private bool CheckBossDowned(string bossName)
        {
            switch (bossName)
            {
                case "克苏鲁之眼":
                    return NPC.downedBoss1;
                case "史莱姆王":
                    return NPC.downedSlimeKing;
                case "世界吞噬者":
                    return NPC.downedBoss2;
                case "克苏鲁之脑":
                    return NPC.downedBoss2;
                case "骷髅王":
                    return NPC.downedBoss3;
                case "蜂王":
                    return NPC.downedQueenBee;
                case "鹿角怪":
                    return NPC.downedDeerclops;
                case "血肉墙":
                    return Main.hardMode;
                case "双子魔眼":
                    return NPC.downedMechBoss2;
                case "毁灭者":
                    return NPC.downedMechBoss1;
                case "机械骷髅王":
                    return NPC.downedMechBoss3;
                case "世纪之花":
                    return NPC.downedPlantBoss;
                case "石巨人":
                    return NPC.downedGolemBoss;
                case "猪鲨":
                    return NPC.downedFishron;
                case "拜月教邪教徒":
                    return NPC.downedAncientCultist;
                case "月球领主":
                    return NPC.downedMoonlord;
                case "光之女皇":
                    return NPC.downedEmpressOfLight;
                case "史莱姆皇后":
                    return NPC.downedQueenSlime;
                case "哀木":
                    return NPC.downedHalloweenTree;
                case "南瓜王":
                    return NPC.downedHalloweenKing;
                case "长绿尖叫怪":
                    return NPC.downedChristmasTree;
                case "冰雪女皇":
                    return NPC.downedChristmasIceQueen;
                case "圣诞坦克":
                    return NPC.downedChristmasSantank;
                case "火星飞碟":
                    return NPC.downedMartians;
                case "星云柱":
                    return NPC.downedTowerNebula;
                case "日耀柱":
                    return NPC.downedTowerSolar;
                case "星尘柱":
                    return NPC.downedTowerStardust;
                case "星旋柱":
                    return NPC.downedTowerVortex;
                default:
                    return false;
            }
        }
        #endregion

        #region BOSS列表
        private bool IsBoss(int npcId)
        {
            // 这里定义Boss的ID列表
            HashSet<int> bossIds = new HashSet<int>
            {
               NPCID.EyeofCthulhu,
               NPCID.KingSlime,
               NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody,NPCID.EaterofWorldsTail,
               NPCID.BrainofCthulhu,
               NPCID.SkeletronHead,
               NPCID.QueenBee,
               NPCID.Deerclops,
               NPCID.WallofFlesh,
               NPCID.Retinazer, NPCID.Spazmatism,
               NPCID.TheDestroyer,
               NPCID.SkeletronPrime,
               NPCID.Plantera,
               NPCID.GolemHead,
               NPCID.DukeFishron,
               NPCID.CultistBoss,
               NPCID.MoonLordCore,
               NPCID.HallowBoss,
               NPCID.QueenSlimeBoss,
            };

            // 检查传入的npcId是否在Boss ID列表中
            return bossIds.Contains(npcId);
        }
        #endregion
    }
}

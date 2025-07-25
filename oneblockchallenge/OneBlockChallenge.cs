using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;
using Terraria.Utilities;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace OneBlockChallenge
{
        #region 插件信息
    [ApiVersion(2, 1)]
    public class OneBlockChallenge : TerrariaPlugin
    {
        public override string Author => "35117";
        public override string Description => "单方块生存，群777541014";
        public override string Name => "OneBlockChallenge";
        public override Version Version => new Version(1, 4, 3, 0);
        const string whathehell = "不会真的有人反编译新手写的插件吧";
        static private Random random = new Random();
        private static List<int> RandomTilePool = new List<int>();
        List<Tuple<int, int>> generatedStructures = new List<Tuple<int, int>>();
        internal static Configuration Config = new();
        int MaxTileRangeX = Main.maxTilesX - 50;
        int MaxTileRangeY = Main.maxTilesY - 50;
        int MinTileRangeX = 50;
        int MinTileRangeY = 50;
        public OneBlockChallenge(Main game) : base(game)
        {
        }
        #endregion

        #region 初始化
        public override void Initialize()
        {
            LoadConfig();
            GeneralHooks.ReloadEvent += ReloadEvent;
            ServerApi.Hooks.GamePostInitialize.Register(this, OnWorldload);
            GetDataHandlers.TileEdit += OnTileEdit;
            Commands.ChatCommands.Add(new Command("OBC", OBC, "obc"){ AllowServer = true, DoLog = true, HelpDesc = new string[] { "帮助文档", "手动在出生点放置一次方块" }, HelpText = "手动在出生点放置一次方块" });
            ServerApi.Hooks.NpcKilled.Register(this, OnNPCKilled);
        }
        #endregion

        #region 卸载钩子
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TShockAPI.Hooks.GeneralHooks.ReloadEvent -= ReloadEvent;
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnWorldload);
                GetDataHandlers.TileEdit -= OnTileEdit;
            }
            base.Dispose(disposing);
        }
        #endregion

        #region 世界加载事件
        private void OnWorldload(EventArgs args)
        {
            if (Config.Enabled)
            {
                LoadConfig();
                UpdateRandomTilePool();
                PlaceRandomBlock();
            }
        }
        #endregion

        #region 重读
        private static void ReloadEvent(ReloadEventArgs e)
        {
            Config = Configuration.Read();
            UpdateRandomTilePool();
            e.Player?.SendSuccessMessage("[OneBlockChallenge] 重新加载配置完毕。");
            Config.Write();
        }
        #endregion

        #region 加载配置
        private static void LoadConfig()
        {
            Config = Configuration.Read();
            Config.Write();
        }
        #endregion

        #region 插件启用时，破坏图格放置方块
        void OnTileEdit(object o, GetDataHandlers.TileEditEventArgs args)
        {
            if(Config.Enabled)
            {
                if (args.X == Main.spawnTileX && args.Y == Main.spawnTileY)
                {
                    if (args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
                    {
                        PlaceRandomBlock();
                        args.Handled = true;
                    }
                }
            }
        }
        #endregion

        #region 更新随机池
        private static void UpdateRandomTilePool()
        {
            // 清空随机池
            RandomTilePool.Clear();

            // 添加“无限制”对应的图格id
            if (Config.BossTile.ContainsKey("无限制"))
            {
                RandomTilePool.AddRange(Config.BossTile["无限制"]);
            }

            // 添加已击败的Boss对应的图格id
            foreach (var bossKey in Config.BossDefeated.Keys)
            {
                if (Config.BossDefeated[bossKey])
                {
                    if (Config.BossTile.ContainsKey(bossKey))
                    {
                        RandomTilePool.AddRange(Config.BossTile[bossKey]);
                    }
                }
            }
        }
        #endregion

        #region 放置方块
        private void PlaceRandomBlock()
        {
            WorldGen.KillTile(Main.spawnTileX, Main.spawnTileY);
            ushort tileType = GetRandomTileType();
            WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY, tileType, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX, Main.spawnTileY, (TileChangeType)0);

        }
        #endregion

        #region 命令
        private void OBC(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("OBC 指令需要至少一个子指令。有效子指令如下：\n" +
                    "obc place [tileType] -- 手动放置方块\n" +
                    "obc create [yes] -- 创建基础空岛地图，需要确认");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();
            switch (subCommand)
            {
                case "place":
                    ManualPlace(args);
                    break;
                case "create":
                    if (args.Parameters.Count > 1 && args.Parameters[1].ToLower() == "yes")
                    {
                        CreateMap(args);
                    }
                    else
                    {
                        args.Player.SendErrorMessage("创建地图操作需要使用 'obc create yes' 来确认。");
                    }
                    break;
                default:
                    args.Player.SendErrorMessage("无效的 obc 子指令。输入/obc查看有效指令。");
                    break;
            }
        }
        #endregion

        #region 手动放置指令
        private void ManualPlace(CommandArgs args)
        {
            int tileType = -1;
            if (!Config.Enabled)
            {
                args.Player.SendErrorMessage("插件当前未启用。");
                return;
            }
            if (args.Parameters.Count > 0 && int.TryParse(args.Parameters[0], out tileType))
            {

                WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY, tileType, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, Main.spawnTileX, Main.spawnTileY, (TileChangeType)0);
            }
            else
            {
                PlaceRandomBlock();
                args.Player.SendSuccessMessage("你手动在出生点位置放置了一次方块！");
            }
        }
        #endregion

        #region 生成空岛地图方法1
        private void CreateMap(CommandArgs args)
        {
            long Time1 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Main.tile[x, y].ClearEverything();
                    NetMessage.SendTileSquare(-1, x, y, (TileChangeType)0);
                }
                if (Config.Broadcast["生成地图广播"])
                {
                    int xremainnum = Main.maxTilesX - x;
                    if (xremainnum % 100 == 0)
                    {
                        float xremainpercent = x * 100 / Main.maxTilesX;
                        TShock.Utils.Broadcast($"[OneBlockChallenge] 已清空100列图格，剩余{xremainnum}列（{xremainpercent}%）", new Color(0, 255, 0));
                    }
                }
            }
            if (Config.MapRandomPlace)
            {
                PlaceRandomBlock();
                WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY + 1, 226, false, true, -1, 0);

                int DemonAltarX, DemonAltarY;
                CheckStructure(out DemonAltarX, out DemonAltarY, 100);
                CreateAltar(DemonAltarX, DemonAltarY, 25, 26, 0);

                int CrimsonAltarX, CrimsonAltarY;
                CheckStructure(out CrimsonAltarX, out CrimsonAltarY, 100);
                CreateAltar(CrimsonAltarX, CrimsonAltarY, 203, 26, 1);

                int LihzahrdAltarX, LihzahrdAltarY;
                CheckStructure(out LihzahrdAltarX, out LihzahrdAltarY, 100);
                CreateAltar(LihzahrdAltarX, LihzahrdAltarY, 226, 237, 0);

                int HellforgeX, HellforgeY;
                CheckStructure(out HellforgeX, out HellforgeY, 100);
                CreateAltar(HellforgeX, HellforgeY, 75, 77, 0);

                int WaterX, WaterY;
                CheckStructure(out WaterX, out WaterY, 100, true);
                CreateLiquid(WaterX, WaterY, 0);

                int LavaX, LavaY;
                CheckStructure(out LavaX, out LavaY, 100);
                CreateLiquid(LavaX, LavaY, 1);

                int ShimmerX, ShimmerY;
                CheckStructure(out ShimmerX, out ShimmerY, 100);
                CreateLiquid(ShimmerX, ShimmerY, 3);

                if (Config.Broadcast["生成地图广播"])
                {
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 腐化祭坛已生成，位于{DemonAltarX},{DemonAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 猩红祭坛已生成，位于{CrimsonAltarX},{CrimsonAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 丛林蜥蜴祭坛已生成，位于{LihzahrdAltarX},{LihzahrdAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 地狱熔炉已生成，位于{HellforgeX},{HellforgeY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 初始方块已生成", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 水已生成，位于{WaterX},{WaterY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 熔岩已生成，位于{LavaX},{LavaY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 微光已生成，位于{ShimmerX},{ShimmerY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 地牢点位于{Main.dungeonX},{Main.dungeonY}", new Color(0, 255, 0));
                }

            }
            else
            {

                int DemonAltarX = Main.spawnTileX - 500;
                int DemonAltarY = Main.spawnTileY;
                int CrimsonAltarX = Main.spawnTileX + 500;
                int CrimsonAltarY = Main.spawnTileY;
                int LihzahrdAltarX = Main.spawnTileX - 500;
                int LihzahrdAltarY = Main.spawnTileY + 500;
                int HellforgeX = Main.spawnTileX;
                int HellforgeY = Main.maxTilesY - 200;
                int WaterX = Main.spawnTileX;
                int WaterY = Main.spawnTileY + 200;
                int LavaX = Main.spawnTileX;
                int LavaY = Main.maxTilesY - 300;
                int ShimmerX = Main.spawnTileX + 500;
                int ShimmerY = Main.spawnTileY + 500;
                PlaceRandomBlock();
                WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY + 1, 226, false, true, -1, 0);

                CreateAltar(DemonAltarX, DemonAltarY, 25, 26, 0);
                CreateAltar(CrimsonAltarX, CrimsonAltarY, 203, 26, 1);
                CreateAltar(LihzahrdAltarX, LihzahrdAltarY, 226, 237, 0);
                CreateAltar(HellforgeX, HellforgeY, 75, 77, 0);
                CreateLiquid(WaterX, WaterY, 0);
                CreateLiquid(LavaX, LavaY, 1);
                CreateLiquid(ShimmerX, ShimmerY, 3);
                if (Config.Broadcast["生成地图广播"])
                {
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 腐化祭坛已生成，位于{DemonAltarX},{DemonAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 猩红祭坛已生成，位于{CrimsonAltarX},{CrimsonAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 丛林蜥蜴祭坛已生成，位于{LihzahrdAltarX},{LihzahrdAltarY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 地狱熔炉已生成，位于{HellforgeX},{HellforgeY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 初始方块已生成", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 水已生成，位于{WaterX},{WaterY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 熔岩已生成，位于{LavaX},{LavaY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 微光已生成，位于{ShimmerX},{ShimmerY}", new Color(0, 255, 0));
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 地牢点位于{Main.dungeonX},{Main.dungeonY}", new Color(0, 255, 0));
                }

            }


            long Time2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long LastSecond = Time2 - Time1;
            TShock.Utils.Broadcast($"[OneBlockChallenge] 地图已生成，用时{LastSecond}秒", new Color(0, 255, 0));

        }
        #endregion

        #region 随机放置方块
        private ushort GetRandomTileType()
        {
            // 确保RandomTilePool列表不为空
            if (RandomTilePool.Count == 0)
            {
                return 0; // 如果列表为空，返回0或适当的默认值
            }

            // 使用System.Random生成随机索引
            var randomIndex = random.Next(RandomTilePool.Count-1); // _random是Random类的实例

            // 从列表中获取随机选择的图格类型
            int randomTileType = RandomTilePool[randomIndex];

            // 转换为ushort类型
            ushort tileType = Convert.ToUInt16(randomTileType);
            if (Config.Broadcast["调试广播"])
            {
                TShock.Utils.Broadcast($"{tileType}", new Color(255, 255, 255));
            }
            return tileType;
        }
        #endregion

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

                if (!Config.BossDefeated["双子魔眼"] && CheckBossDowned("双子魔眼")&&(bossName=="激光眼"|| bossName == "魔焰眼"))
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

        #region 生成祭坛方法
        private void CreateAltar(int AltarX, int AltarY, int AltarTile, ushort Altar, ushort AltarStyle)
        {
            for (int x = AltarX - 1; x <= AltarX + 1; x++)
            {
                WorldGen.PlaceTile(x, AltarY + 1, AltarTile, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, x, AltarY + 1, (TileChangeType)0);
            }
            WorldGen.Place3x2(AltarX, AltarY, Altar, AltarStyle);
            NetMessage.SendTileSquare(-1, AltarX, AltarY, 3, 3);
        }
        #endregion

        #region 生成流体方法
        private void CreateLiquid(int LiquidX, int LiquidY, byte Liquid)
        {
            WorldGen.PlaceLiquid(LiquidX, LiquidY, Liquid, byte.MaxValue);
            NetMessage.sendWater(LiquidX, LiquidY);
            WorldGen.PlaceTile(LiquidX, LiquidY + 1, Config.LiquidBlock, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, LiquidX, LiquidY + 1, (TileChangeType)0);
            WorldGen.PlaceTile(LiquidX + 1, LiquidY, Config.LiquidBlock, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, LiquidX + 1, LiquidY, (TileChangeType)0);
            WorldGen.PlaceTile(LiquidX - 1, LiquidY, Config.LiquidBlock, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, LiquidX - 1, LiquidY, (TileChangeType)0);
        }
        #endregion

        #region 检测结构不重叠方法
        private void CheckStructure(out int StructureX, out int StructureY, int avoidRadius, bool isWater = false)
        {
            bool IsWithinRangeOfExistingStructures(int checkX, int checkY)
            {
                foreach (var structure in generatedStructures)
                {
                    int structureX = structure.Item1;
                    int structureY = structure.Item2;
                    if (Math.Abs(checkX - structureX) <= 5 || Math.Abs(checkY - structureY) <= 5)
                    {
                        return true;
                    }
                }
                return false;
            }

            bool IsWithinSpawnRadius(int checkX, int checkY)
            {
                return Math.Abs(checkX - Main.spawnTileX) <= avoidRadius || Math.Abs(checkY - Main.spawnTileY) <= avoidRadius;
            }

            bool IsBelowSpawnY(int checkY)
            {
                return checkY > Main.spawnTileY;
            }

            do
            {
                StructureX = random.Next(MinTileRangeX, MaxTileRangeX);
                StructureY = random.Next(MinTileRangeY, MaxTileRangeY);
            } while (IsWithinRangeOfExistingStructures(StructureX, StructureY) ||
                    (isWater && IsBelowSpawnY(StructureY)) || // 如果是水，则检查是否在出生点Y坐标以上
                    IsWithinSpawnRadius(StructureX, StructureY)); // 检查是否在出生点的avoidRadius半径内

            generatedStructures.Add(new Tuple<int, int>(StructureX, StructureY));
        }
        #endregion

    }

}

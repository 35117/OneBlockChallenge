using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Microsoft.Xna.Framework;
using Terraria.Utilities;
using Google.Protobuf.WellKnownTypes;

namespace OneBlockChallenge
{
        #region 插件信息
    [ApiVersion(2, 1)]
    public class OneBlockChallenge : TerrariaPlugin
    {
        public override string Author => "35117";
        public override string Description => "单方块生存，群777541014";
        public override string Name => "OneBlockChallenge";
        public override Version Version => new Version(1, 1, 0, 0);
        string whathehell = "不会真的有人反编译新手写的插件吧";
        private Random _random = new Random();
        private static List<int> RandomTilePool = new List<int>();
        internal static Configuration Config = new();
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

        #region 插件启用放置方块
        void OnTileEdit(object o, GetDataHandlers.TileEditEventArgs args)
        {
            if(Config.Enabled)
            {
                if (args.X == Main.spawnTileX && args.Y == Main.spawnTileY)
                {
                    if (args.Action == GetDataHandlers.EditAction.KillTile && args.EditData == 0)
                    {
                        PlaceRandomBlock();
                    }
                }
            }
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

        #region 加载配置
        private static void LoadConfig()
        {
            Config = Configuration.Read();
            Config.Write();
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

        #region 更新随机池
        private static void UpdateRandomTilePool()
        {
            // 清空随机池
            RandomTilePool.Clear();

            // 添加“无限制”对应的图格id
            if (Config.BOSS对应图格.ContainsKey("无限制"))
            {
                RandomTilePool.AddRange(Config.BOSS对应图格["无限制"]);
            }

            // 添加已击败的Boss对应的图格id
            foreach (var bossKey in Config.BOSS击败检测.Keys)
            {
                if (Config.BOSS击败检测[bossKey])
                {
                    if (Config.BOSS对应图格.ContainsKey(bossKey))
                    {
                        RandomTilePool.AddRange(Config.BOSS对应图格[bossKey]);
                    }
                }
            }
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

        #region 放置方块
        private void PlaceRandomBlock()
        {
            ushort tileType = GetRandomTileType();
            // 正常放置
            WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY, tileType, false, true, -1, 0);
            // 发送数据包
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
                    "obc create -- 创建基础空岛地图");
                return;
            }

            string subCommand = args.Parameters[0].ToLower();
            switch (subCommand)
            {
                case "place":
                    ManualPlace(args);
                    break;
                case "create":
                    CreateMap(args);
                    break;
                default:
                    args.Player.SendErrorMessage("无效的 OBC 子指令。");
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

        #region 生成空岛地图
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
                int xremainnum = Main.maxTilesX - x;
                if (xremainnum % 100 == 0)
                {
                    float xremainpercent = x*100 / Main.maxTilesX;
                    TShock.Utils.Broadcast($"[OneBlockChallenge] 已清空100列图格，剩余{xremainnum}列（{xremainpercent}%）", new Color(0, 255, 0));
                }
            }

            for (int x = Main.spawnTileX - 500; x < Main.spawnTileX - 497; x++) 
            {
                WorldGen.PlaceTile(x, Main.spawnTileY, 25, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, x, Main.spawnTileY, (TileChangeType)0);
            }
            WorldGen.Place3x2(Main.spawnTileX - 499, Main.spawnTileY-1, 26, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX - 499, Main.spawnTileY-1, 3, 0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 腐化祭坛已生成，位于{Main.spawnTileX -499},{Main.spawnTileY - 1}", new Color(0, 255, 0));

            for (int x = Main.spawnTileX + 500; x < Main.spawnTileX + 503; x++)
            {
                WorldGen.PlaceTile(x, Main.spawnTileY , 203, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, x, Main.spawnTileY, (TileChangeType)0);
            }
            WorldGen.Place3x2(Main.spawnTileX + 501, Main.spawnTileY-1, 26, 1);
            NetMessage.SendTileSquare(-1, Main.spawnTileX + 501, Main.spawnTileY-1, 3, 0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 猩红祭坛已生成，位于{Main.spawnTileX + 501},{Main.spawnTileY - 1}", new Color(0, 255, 0));

            for (int x = Main.spawnTileX - 500; x < Main.spawnTileX - 497; x++)
            {
                WorldGen.PlaceTile(x, Main.spawnTileY + 500, 226, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, x, Main.spawnTileY + 500, (TileChangeType)0);
            }
            WorldGen.Place3x2(Main.spawnTileX - 499, Main.spawnTileY + 499, 237, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX - 499, Main.spawnTileY + 499, 3, 0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 丛林蜥蜴祭坛已生成，位于{Main.spawnTileX - 499},{Main.spawnTileY + 499}", new Color(0, 255, 0));

            PlaceRandomBlock();
            WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY + 1, 226, false, true, -1, 0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 初始方块已生成", new Color(0, 255, 0));


            WorldGen.PlaceLiquid(Main.spawnTileX, Main.spawnTileY + 200, 0, byte.MaxValue);
            NetMessage.sendWater(Main.spawnTileX, Main.spawnTileY + 200);
            WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY + 201, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX, Main.spawnTileY + 201, (TileChangeType)0);
            WorldGen.PlaceTile(Main.spawnTileX+1, Main.spawnTileY + 200, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX + 1, Main.spawnTileY + 200, (TileChangeType)0);
            WorldGen.PlaceTile(Main.spawnTileX-1, Main.spawnTileY + 200, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX-1, Main.spawnTileY + 200, (TileChangeType)0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 水已生成，位于{Main.spawnTileX},{Main.spawnTileY + 200}", new Color(0, 255, 0));

            WorldGen.PlaceLiquid(Main.spawnTileX, Main.maxTilesY - 300, 1, byte.MaxValue);
            NetMessage.sendWater(Main.spawnTileX, Main.maxTilesY - 300);
            WorldGen.PlaceTile(Main.spawnTileX, Main.maxTilesY - 299, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX, Main.maxTilesY - 299, (TileChangeType)0);
            WorldGen.PlaceTile(Main.spawnTileX + 1, Main.maxTilesY - 300, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX + 1, Main.maxTilesY - 300, (TileChangeType)0);
            WorldGen.PlaceTile(Main.spawnTileX - 1, Main.maxTilesY - 300, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX - 1, Main.maxTilesY - 300, (TileChangeType)0);
            TShock.Utils.Broadcast($"[OneBlockChallenge] 熔岩已生成，位于{Main.spawnTileX},{Main.maxTilesY - 300}", new Color(0, 255, 0));

            WorldGen.PlaceTile(Main.spawnTileX + 499, Main.spawnTileY + 500, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX + 499, Main.spawnTileY + 500, (TileChangeType)0);
            WorldGen.PlaceTile(Main.spawnTileX + 510, Main.spawnTileY + 500, 226, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, Main.spawnTileX + 510, Main.spawnTileY + 500, (TileChangeType)0);

            for (int x = Main.spawnTileX+500;x< Main.spawnTileX + 510; x++ )
            {
                WorldGen.PlaceTile(x, Main.spawnTileY + 501, 226, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, x, Main.spawnTileY + 501, (TileChangeType)0);
                WorldGen.PlaceLiquid(x, Main.spawnTileY + 500, 3, byte.MaxValue);
                NetMessage.sendWater(x, Main.spawnTileY + 500);
            }
            TShock.Utils.Broadcast($"[OneBlockChallenge] 微光已生成，位于{Main.spawnTileX + 500},{Main.spawnTileY + 500}", new Color(0, 255, 0));
            TShock.Utils.Broadcast($"[OneBlockChallenge] 地牢点位于{Main.dungeonX},{Main.dungeonY}", new Color(0, 255, 0));


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
            var randomIndex = _random.Next(RandomTilePool.Count); // _random是Random类的实例

            // 从列表中获取随机选择的图格类型
            int randomTileType = RandomTilePool[randomIndex];

            // 转换为ushort类型
            ushort tileType = Convert.ToUInt16(randomTileType);
            if (Config.广播设置["调试广播"])
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
                if (!Config.BOSS击败检测[bossName])
                {
                    List<int> newTileTypes = Config.BOSS对应图格.ContainsKey(bossName) ? Config.BOSS对应图格[bossName] : new List<int>();
                    foreach (var tileType in newTileTypes)
                    {
                        RandomTilePool.Add(tileType);
                    }

                    Config.BOSS击败检测[bossName] = true;

                    if (!Config.BOSS击败检测["邪恶BOSS"] && (bossName == "世界吞噬怪" || bossName == "克苏鲁之脑"))
                    {
                        newTileTypes = Config.BOSS对应图格["邪恶BOSS"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BOSS击败检测["邪恶BOSS"] = true;
                        if (Config.广播设置["邪恶BOSS"])
                        {
                            TShock.Utils.Broadcast("邪恶BOSS 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }
                    if (!Config.BOSS击败检测["一王"] && (CheckBossDowned("双子魔眼") || CheckBossDowned("毁灭者") || CheckBossDowned("机械骷髅王")))
                    {
                        newTileTypes = Config.BOSS对应图格["一王"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BOSS击败检测["一王"] = true;
                        if (Config.广播设置["一王"])
                        {
                            TShock.Utils.Broadcast("一王 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }

                    if (!Config.BOSS击败检测["三王"] && CheckBossDowned("双子魔眼") && CheckBossDowned("毁灭者") && CheckBossDowned("机械骷髅王"))
                    {
                        newTileTypes = Config.BOSS对应图格["三王"];
                        foreach (var tileType in newTileTypes)
                        {
                            RandomTilePool.Add(tileType);
                        }
                        Config.BOSS击败检测["三王"] = true;
                        if (Config.广播设置["三王"])
                        {
                            TShock.Utils.Broadcast("三王 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                        }
                    }
                    Config.Write();
                    if (Config.广播设置["所有BOSS"])
                    {
                        TShock.Utils.Broadcast($"{bossName} 已被击败，对应的图格已加入随机池.", new Color(0, 255, 0));
                    }

                }

                if (!Config.BOSS击败检测["双子魔眼"] && CheckBossDowned("双子魔眼")&&(bossName=="激光眼"|| bossName == "魔焰眼"))
                {
                    List<int> newTileTypes = Config.BOSS对应图格.ContainsKey(bossName) ? Config.BOSS对应图格[bossName] : new List<int>();
                    newTileTypes = Config.BOSS对应图格["双子魔眼"];

                    foreach (var tileType in newTileTypes)
                    {
                        RandomTilePool.Add(tileType);
                    }
                    Config.BOSS击败检测["双子魔眼"] = true;
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

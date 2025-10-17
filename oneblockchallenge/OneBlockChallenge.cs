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
        #region 插件信息
    [ApiVersion(2, 1)]
    public partial class OneBlockChallenge : TerrariaPlugin
    {
        public override string Author => "35117";
        public override string Description => "单方块生存，群777541014";
        public override string Name => "OneBlockChallenge";
        public override Version Version => new Version(1, 4, 5, 0);
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
            GetDataHandlers.TileEdit += OnTileEdit!;
            Commands.ChatCommands.Add(new Command("OBC", OBC, "obc"){ AllowServer = true, DoLog = true});
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
                GetDataHandlers.TileEdit -= OnTileEdit!;
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
            }
        }
        #endregion


        #region 命令
        private void OBC(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("OBC 指令需要至少一个子指令。有效子指令如下：\n" +
                    "/obc place [tileType] -- 手动放置方块\n" +
                    "/obc create [yes] -- 创建基础空岛地图，需要确认");
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
                        args.Player.SendErrorMessage("创建地图操作需要使用 'obc create yes' 来确认，以防止手误清空地图。");
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
                args.Player.SendSuccessMessage("你输入的方块类型不合法！已随机放置一个方块");
                PlaceRandomBlock();
            }
            else
            {
                WorldGen.PlaceTile(Main.spawnTileX, Main.spawnTileY, tileType, false, true, -1, 0);
                NetMessage.SendTileSquare(-1, Main.spawnTileX, Main.spawnTileY, (TileChangeType)0);
                args.Player.SendSuccessMessage("你手动在出生点位置放置了一次方块！");
            }
        }
        #endregion



    }

}

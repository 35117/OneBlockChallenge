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
            if (Config.Enabled)
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
            RandomTilePool = Config.BossTile
                .Where(kvp => kvp.Key == "无限制" || Config.BossDefeated.GetValueOrDefault(kvp.Key, false))
                .SelectMany(kvp => kvp.Value)
                .ToList();
        }
        #endregion


        #region 生成地图广播方法
        private void BroadcastGenerate(int DemonAltarX, int DemonAltarY, int CrimsonAltarX, int CrimsonAltarY, int LihzahrdAltarX, int LihzahrdAltarY, int HellforgeX, int HellforgeY, int WaterX, int WaterY, int LavaX, int LavaY, int ShimmerX, int ShimmerY)
        {
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
        #endregion
    }
}

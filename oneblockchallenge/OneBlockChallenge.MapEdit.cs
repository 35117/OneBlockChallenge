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

        #region 生成祭坛方法
        private void CreateAltar(int AltarX, int AltarY, int AltarTile, ushort Altar, ushort AltarStyle)
        {
            for (int x = AltarX - 1; x <= AltarX + 1; x++)
            {
                CreateAndSendTile(x, AltarY + 1, AltarTile);
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
            CreateAndSendTile(LiquidX, LiquidY + 1, Config.LiquidBlock);
            CreateAndSendTile(LiquidX + 1, LiquidY, Config.LiquidBlock);
            CreateAndSendTile(LiquidX - 1, LiquidY, Config.LiquidBlock);
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


        #region 生成图格并发送更新方法
        private void CreateAndSendTile(int x, int y, int type)
        {
            WorldGen.PlaceTile(x, y, type, false, true, -1, 0);
            NetMessage.SendTileSquare(-1, x, y, (TileChangeType)0);
        }
        #endregion


        #region 清空地图方法
        private void ClearMap()
        {
            for (int x = 0; x < Main.maxTilesX; x++)
            {
                for (int y = 0; y < Main.maxTilesY; y++)
                {
                    Main.tile[x, y].ClearEverything();
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
            NetMessage.SendData((int)PacketTypes.TileFrameSection, -1, -1, null, 0, 0, Main.maxTilesX, Main.maxTilesY);
        }
        #endregion


        #region 生成空岛地图方法
        private void CreateMap(CommandArgs args)
        {
            long Time1 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            ClearMap();
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
                BroadcastGenerate(DemonAltarX, DemonAltarY, CrimsonAltarX, CrimsonAltarY, LihzahrdAltarX, LihzahrdAltarY, HellforgeX, HellforgeY, WaterX, WaterY, LavaX, LavaY, ShimmerX, ShimmerY);

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
                BroadcastGenerate(DemonAltarX, DemonAltarY, CrimsonAltarX, CrimsonAltarY, LihzahrdAltarX, LihzahrdAltarY, HellforgeX, HellforgeY, WaterX, WaterY, LavaX, LavaY, ShimmerX, ShimmerY);
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
            var randomIndex = random.Next(RandomTilePool.Count - 1); // _random是Random类的实例

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
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 方块 Y 轴旋转 仅 0° 与 90° 两档
    /// </summary>
    public enum CubeRotation
    {
        Deg0 = 0,
        Deg90 = 1,
    }

    /// <summary>
    /// 一次放置的完整描述
    /// 值类型上下文 只存起始格与占格尺寸
    /// </summary>
    public readonly struct CubePlacementInfo
    {
        public Vector3Int OriginPoint { get; }
        public Vector3Int CubePrefabSize { get; }

        public CubePlacementInfo(Vector3Int origin, Vector3Int cellSize)
        {
            OriginPoint = origin;
            CubePrefabSize = cellSize;
        }

        /// <summary>
        /// 由目标格与方块数据构建放置描述
        /// </summary>
        public static bool TryCreate(Vector3Int targetCell, CubeData cubeData, out CubePlacementInfo placement)
        {
            placement = default;
            if (cubeData?.CubePrefab == null)
                return false;

            placement = new CubePlacementInfo(targetCell, cubeData.GetCubePrefabSizeInt());
            return true;
        }

        /// <summary>
        /// 写入当前放置占用的所有格子
        /// </summary>
        public void FillOccupiedCells(List<Vector3Int> output, CubeRotation rotation)
            => FillOccupiedCells(OriginPoint, CubePrefabSize, rotation, output);

        /// <summary>
        /// 写入指定原点与尺寸的占格 破坏方块等场景用
        /// </summary>
        public static void FillOccupiedCells(
            Vector3Int origin,
            Vector3Int cellSize,
            CubeRotation rotation,
            List<Vector3Int> output)
        {
            output.Clear();

            for (int x = 0; x < cellSize.x; x++)
            for (int y = 0; y < cellSize.y; y++)
            for (int z = 0; z < cellSize.z; z++)
            {
                var offset = new Vector3Int(x, y, z);
                if (rotation == CubeRotation.Deg90)
                    offset = new Vector3Int(offset.z, offset.y, -offset.x);
                output.Add(origin + offset);
            }
        }

        /// <summary>
        /// 由占格列表计算世界空间 AABB
        /// </summary>
        public static Bounds ComputeBoundsFromCells(List<Vector3Int> cells, float gridUnitSize)
        {
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.z < minZ) minZ = cell.z;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
                if (cell.z > maxZ) maxZ = cell.z;
            }

            var minWorld = new Vector3(minX, minY, minZ) * gridUnitSize;
            var maxWorld = new Vector3(maxX + 1, maxY + 1, maxZ + 1) * gridUnitSize;
            return new Bounds((minWorld + maxWorld) * 0.5f, maxWorld - minWorld);
        }
    }

    /// <summary>
    /// 已放置方块记录
    /// 只存起始格 origin 占格按尺寸与旋转现算
    /// </summary>
    public class PlacedCube
    {
        public CubeData data;
        public GameObject spawnedObj;
        public Vector3Int origin;
        public CubeRotation rotation;

        public PlacedCube(CubeData data, GameObject spawnedObj, Vector3Int origin, CubeRotation rotation = CubeRotation.Deg0)
        {
            this.data = data;
            this.spawnedObj = spawnedObj;
            this.origin = origin;
            this.rotation = rotation;
        }
    }
}

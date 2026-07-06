using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    [CreateAssetMenu(fileName = "PaintedBuildingPlan", menuName = "Mm_ProceduralBuilding/PaintedBuildingPlan")]
    public class PaintedBuildingPlan : SerializedScriptableObject
    {
        /// <summary>
        /// 楼层数据列表
        /// </summary>
        [LabelText("楼层数据列表")]
        public List<PaintedBuildingFloorData> paintFloorDataList = new();

        /// <summary>
        /// 获取或创建楼层
        /// </summary>
        public PaintedBuildingFloorData GetOrCreateFloor(int floorIndex)
        {
            int safeFloorIndex = Mathf.Max(0, floorIndex);
            foreach (var floorData in paintFloorDataList)
            {
                if (floorData != null && floorData.floorIndex == safeFloorIndex)
                    return floorData;
            }

            var newFloorData = new PaintedBuildingFloorData
            {
                floorIndex = safeFloorIndex,
            };
            paintFloorDataList.Add(newFloorData);
            return newFloorData;
        }

        /// <summary>
        /// 查找楼层
        /// </summary>
        public PaintedBuildingFloorData FindFloor(int floorIndex)
        {
            int safeFloorIndex = Mathf.Max(0, floorIndex);
            foreach (var floorData in paintFloorDataList)
            {
                if (floorData != null && floorData.floorIndex == safeFloorIndex)
                    return floorData;
            }

            return null;
        }

        /// <summary>
        /// 设置格子
        /// </summary>
        public void SetCell(
            int floorIndex,
            Vector2Int gridPos,
            EPaintedBuildingCellType cellType,
            int wallHeightGridCount,
            int cutoutStartHeightGridCount,
            int cutoutEndHeightGridCount)
        {
            var floorData = GetOrCreateFloor(floorIndex);
            if (cellType == EPaintedBuildingCellType.Erase)
            {
                floorData.RemoveTopCell(gridPos);
                return;
            }

            if (cellType == EPaintedBuildingCellType.Floor)
            {
                SetFloorCell(floorData, gridPos);
                return;
            }

            SetStructureCell(floorData, gridPos, cellType, wallHeightGridCount, cutoutStartHeightGridCount, cutoutEndHeightGridCount);
        }

        /// <summary>
        /// 移除顶层格子
        /// </summary>
        public void RemoveTopCell(int floorIndex, Vector2Int gridPos)
        {
            var floorData = FindFloor(floorIndex);
            if (floorData == null)
                return;

            floorData.RemoveTopCell(gridPos);
        }

        /// <summary>
        /// 清空楼层网格
        /// </summary>
        public void ClearFloor(int floorIndex)
        {
            var floorData = FindFloor(floorIndex);
            if (floorData == null)
                return;

            floorData.floorCellDataList.Clear();
            floorData.structureCellDataList.Clear();
        }

        /// <summary>
        /// 填充地面矩形
        /// </summary>
        public void FillFloorRect(int floorIndex, Vector2Int bottomLeftGridPos, Vector2Int topRightGridPos)
        {
            GetGridRectBounds(bottomLeftGridPos, topRightGridPos, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                    SetCell(floorIndex, new Vector2Int(x, z), EPaintedBuildingCellType.Floor, 1, 0, 1);
            }
        }

        /// <summary>
        /// 批量设置墙体
        /// </summary>
        public void SetWallCells(
            int floorIndex,
            IEnumerable<Vector2Int> gridPosList,
            int wallHeightGridCount)
        {
            foreach (var gridPos in gridPosList)
            {
                SetCell(
                    floorIndex,
                    gridPos,
                    EPaintedBuildingCellType.Wall,
                    wallHeightGridCount,
                    0,
                    wallHeightGridCount);
            }
        }

        /// <summary>
        /// 获取矩形边界
        /// </summary>
        private static void GetGridRectBounds(
            Vector2Int bottomLeftGridPos,
            Vector2Int topRightGridPos,
            out int minX,
            out int maxX,
            out int minZ,
            out int maxZ)
        {
            minX = Mathf.Min(bottomLeftGridPos.x, topRightGridPos.x);
            maxX = Mathf.Max(bottomLeftGridPos.x, topRightGridPos.x);
            minZ = Mathf.Min(bottomLeftGridPos.y, topRightGridPos.y);
            maxZ = Mathf.Max(bottomLeftGridPos.y, topRightGridPos.y);
        }

        /// <summary>
        /// 设置地面格子
        /// </summary>
        private void SetFloorCell(PaintedBuildingFloorData floorData, Vector2Int gridPos)
        {
            var cellData = floorData.FindFloorCell(gridPos);
            if (cellData == null)
            {
                cellData = new PaintedBuildingCellData
                {
                    gridPos = gridPos,
                };
                floorData.floorCellDataList.Add(cellData);
            }

            cellData.cellType = EPaintedBuildingCellType.Floor;
            cellData.heightGridCount = 1;
            cellData.cutoutStartHeightGridCount = 0;
            cellData.cutoutEndHeightGridCount = 1;
        }

        /// <summary>
        /// 设置结构格子
        /// </summary>
        private void SetStructureCell(
            PaintedBuildingFloorData floorData,
            Vector2Int gridPos,
            EPaintedBuildingCellType cellType,
            int wallHeightGridCount,
            int cutoutStartHeightGridCount,
            int cutoutEndHeightGridCount)
        {
            var cellData = floorData.FindStructureCell(gridPos);
            if (cellData == null)
            {
                cellData = new PaintedBuildingCellData
                {
                    gridPos = gridPos,
                };
                floorData.structureCellDataList.Add(cellData);
            }

            cellData.cellType = cellType;
            cellData.heightGridCount = Mathf.Max(1, wallHeightGridCount);
            cellData.cutoutStartHeightGridCount = Mathf.Max(0, cutoutStartHeightGridCount);
            cellData.cutoutEndHeightGridCount = Mathf.Clamp(cutoutEndHeightGridCount, cellData.cutoutStartHeightGridCount + 1, cellData.heightGridCount);
        }

        /// <summary>
        /// 校验参数
        /// </summary>
        private void OnValidate()
        {
            if (paintFloorDataList == null)
                paintFloorDataList = new List<PaintedBuildingFloorData>();

            foreach (var floorData in paintFloorDataList)
            {
                if (floorData == null)
                    continue;

                floorData.floorIndex = Mathf.Max(0, floorData.floorIndex);
                if (floorData.floorCellDataList == null)
                    floorData.floorCellDataList = new List<PaintedBuildingCellData>();

                if (floorData.structureCellDataList == null)
                    floorData.structureCellDataList = new List<PaintedBuildingCellData>();

                ValidateCellList(floorData.floorCellDataList, true);
                ValidateCellList(floorData.structureCellDataList, false);
            }
        }

        /// <summary>
        /// 校验格子列表
        /// </summary>
        private void ValidateCellList(List<PaintedBuildingCellData> cellDataList, bool isFloorLayer)
        {
            foreach (var cellData in cellDataList)
            {
                if (cellData == null)
                    continue;

                if (isFloorLayer)
                    cellData.cellType = EPaintedBuildingCellType.Floor;

                cellData.heightGridCount = Mathf.Max(1, cellData.heightGridCount);
                cellData.cutoutStartHeightGridCount = Mathf.Max(0, cellData.cutoutStartHeightGridCount);
                cellData.cutoutEndHeightGridCount = Mathf.Clamp(cellData.cutoutEndHeightGridCount, cellData.cutoutStartHeightGridCount + 1, cellData.heightGridCount);
            }
        }
    }
}

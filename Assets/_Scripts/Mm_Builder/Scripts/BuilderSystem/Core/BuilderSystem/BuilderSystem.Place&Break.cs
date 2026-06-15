using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 处理放置和破坏方块
    /// </summary>
    public partial class BuilderSystem
    {




        /// <summary>
        /// 处理放置方块
        /// </summary>
        public override void HandlePlace(BuilderPlacementReport placement, CubeData cubeData)
        {
            if (!HandlePlaceValid(placement, cubeData)) return;
            CreatAndPlaceCube(placement, cubeData);
        }

        /// <summary>
        /// 处理破坏方块
        /// </summary>
        public override void HandleBreak(Vector3Int gridPos, CubeData cubeData)
        {
            if (!HandleBreakValid(gridPos, cubeData)) return;
            BreakCube(gridPos);
        }


        /// <summary>
        /// 处理放置校验
        /// </summary>
        /// <param name="placement">放置报告</param>
        /// <param name="cubeData">方块数据</param>
        /// <returns></returns>
        public override bool HandlePlaceValid(BuilderPlacementReport placement, CubeData cubeData)
        {
            // 校验区域放置是否合法
            if (!ValidPlacement(placement))
                return false;

            // 校验外部开发者配置
            if (imBuilder != null)
            {
                if (!imBuilder.CustomPlaceValid(out placement, cubeData))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 处理破坏校验
        /// </summary>
        public override bool HandleBreakValid(Vector3Int gridPos, CubeData cubeData)
        {
            if (!runtimeCubeDataDict.TryGetValue(gridPos, out var instance))
                return false;

            if (imBuilder != null)
            {
                var targetData = instance.data;
                if (!imBuilder.CustomBreakValid(out gridPos, targetData))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 校验区域放置是否合法
        /// </summary>
        private bool ValidPlacement(BuilderPlacementReport placement)
        {
            // 将占格信息写入列表
            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            foreach (var gridPos in tempOccupiedGridList)
            {
                // 校验该格是否在可放置区域内
                if (!virtualGrid.ValidVirtualGroup(gridPos))
                    return false;

                // 校验该格是否被占用
                if (runtimeCubeDataDict.ContainsKey(gridPos))
                    return false;
            }

            return true;
        }


        /// <summary>
        /// 放置方块并写入运行时字典
        /// </summary>
        private void CreatAndPlaceCube(BuilderPlacementReport placement, CubeData cubeData)
        {
            // 将占格信息写入列表
            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            var prefab = cubeData.CubePrefab;
            var unitSize = virtualGrid.gridUnitSize;

            // 计算预览方块中心点
            var worldCenter = placement.GetWorldCenterFormGridList(tempOccupiedGridList, unitSize);

            // 实例化方块并设置
            var spawnedObj = Instantiate(prefab, worldCenter, placement.CubeWorldRotation, cubeRoot);
            spawnedObj.layer = builderSetting.cubeLayer;

            // 创建运行时数据
            var runtimeData = new CubeInstance(cubeData, spawnedObj, placement);

            // 调用方块行为
            spawnedObj.GetComponent<CubeBehaviour>()?.OnPlaced(runtimeData);

            // 写入运行时字典
            foreach (var gridPos in tempOccupiedGridList)
                runtimeCubeDataDict.Add(gridPos, runtimeData);

            // 调用外部开发者回调
            if (imBuilder != null)
                imBuilder.CustorOnPlaceSucceeded(runtimeData);
        }


        /// <summary>
        /// 破坏方块
        /// </summary>
        private void BreakCube(Vector3Int gridPos)
        {
            // 尝试获取运行时数据
            if (!runtimeCubeDataDict.TryGetValue(gridPos, out var instanceData)) return;

            // 调用方块行为
            instanceData.instantiateCube.GetComponent<CubeBehaviour>()?.OnRemoved();

            // 销毁方块物体
            if (instanceData.instantiateCube != null) Destroy(instanceData.instantiateCube);

            // 如果是单格方块 则直接移除运行时数据并返回
            if (instanceData.data.IsUnit)
            {
                runtimeCubeDataDict.Remove(gridPos);
                return;
            }

            // 如果是多格方块 则需要移除多个格子的占格信息
            // 创建放置描述
            var placement = new BuilderPlacementReport(
                instanceData.originGridPos,
                instanceData.data.GetCubePrefabSizeInt(),
                instanceData.rotation);

            // 将占格信息写入列表
            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            // 逐个移除占格信息
            foreach (var occupiedGridPos in tempOccupiedGridList)
                runtimeCubeDataDict.Remove(occupiedGridPos);

            // 调用外部开发者回调
            if (imBuilder != null)
                imBuilder.CustorOnBreakSucceeded(instanceData);
        }

    }
}
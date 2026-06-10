using System;
using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mm_Budier
{

    [RequireComponent(typeof(VirtualGrid))]
    public partial class BuilderSystem : MonoBehaviour
    {
        [Header("必要组件")]
        [LabelText("虚拟网格组件")] public VirtualGrid virtualGrid;
        [LabelText("区块加载系统")] public ChunkSystem chunkSystem;

        [LabelText("相机组件")] public Camera mainCamera;//做第一人称的话用这个
        [LabelText("玩家碰撞体组件")] public Collider playerCollider;

        [LabelText("配置文件")] public CubeBuiderSystemConfig config;

        [Header("Debug")]
        [LabelText("当前活跃方块类型")] public CubeData activeCubeData;
        public Dictionary<Vector3Int, CubeRuntimeData> cubeMgrDict = new();//总方块字典

        //占格校验临时列表
        private readonly List<Vector3Int> occupiedList = new(8);

        ///  方块根节点
        private Transform cubeRoot;
        /// 预览方块根节点 
        private Transform preViewRoot;
        /// 区块根节点 
        private Transform chunkRoot;
        public Transform ChunkRoot => chunkRoot;

        [NonSerialized] public static BuilderSystem Instance;

        // 放置偏移量 防止穿模
        public const float PLACEMENT_EPSILON = 0.01f;

        void Awake()
        {
            //单例初始化
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            virtualGrid ??= GetComponent<VirtualGrid>();
            mainCamera ??= Camera.main;

            config.InitCameraInfo(mainCamera);

        }

        void Start()
        {
            cubeRoot = this.transform.Find("CubeRoot");
            preViewRoot = this.transform.Find("PreViewRoot");
            chunkRoot = this.transform.Find("ChunkRoot");
            InitPreView();
        }


        void Update()
        {
            TryPlaceCube(activeCubeData);
            chunkSystem?.UpdateChunk(playerCollider.transform.position);
        }


        #region 公共函数
        public void TryPlaceCube(CubeData cubeData)
        {
            //拿到当前方块数据
            activeCubeData = cubeData;
            if (cubeData == null)
            {
                HidePreView();
                return;
            }

            Vector3Int targetCell = default;
            bool canPlace = false;
            bool fromRaycast = config.CanRaycast();

            // 从射线检测中获取目标格
            if (fromRaycast)
            {
                if (!TryGetPlacementHit(out var hit))
                {
                    HidePreView();
                    return;
                }
                targetCell = GetTargetCell(hit);
            }
            // 从缓存中获取目标格
            else if (!config.TryGetCachedResult(out _, out targetCell, out canPlace))
            {
                HidePreView();
                return;
            }

            // 计算放置信息
            if (!CubePlacementInfo.TryCreatePltInfo(targetCell, cubeData, virtualGrid, out var placement))
            {
                HidePreView();
                return;
            }

            // 从射线检测中获取目标格 需要验证放置信息
            if (fromRaycast)
            {
                // 验证放置信息
                canPlace = ValidatePlacement(placement);
                // 更新缓存
                config.UpdateCache(placement.CubeWorldCenter, targetCell, canPlace);
            }

            // 处理预览
            HandlePreview(placement, cubeData, canPlace);

            // 处理输入并放置
            if (HandleInput(canPlace))
                HandlePlaceCube(placement, cubeData);
        }

        /// <summary>
        /// 破坏方块
        /// </summary>
        /// <param name="worldPos"></param>
        public void TryBreakCube(Vector3 worldPos)
        {
            var gridPos = virtualGrid.WorldToGrid(worldPos);
            BreakCube(gridPos);
            activeCubeData = null;
        }

        #endregion


        #region 射线检测

        /// <summary>
        /// 从屏幕准心发射射线 忽略玩家自身碰撞体
        /// </summary>
        private bool TryGetPlacementHit(out RaycastHit hit)
        {
            hit = default;
            if (mainCamera == null)
                return false;

            // 从屏幕中心发射射线
            var ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
            var hits = config.raycastHits;
            var mask = config.groundLayer | config.cubeLayer;

            // 射线检测
            int hitCount = Physics.RaycastNonAlloc(ray,  // 射线
                                                   hits,  // 碰撞体数组
                                                   config.raycastMaxDistance,  // 最大距离
                                                   mask,  // 层掩码
                                                   QueryTriggerInteraction.Ignore);  // 触发器交互

            if (hitCount <= 0)
                return false;

            float closestDist = float.MaxValue;
            bool found = false;

            // 获取最近的碰撞体
            for (int i = 0; i < hitCount; i++)
            {
                var col = hits[i].collider;
                if (col == null || IsPlayerCollider(col))
                    continue;

                if (hits[i].distance < closestDist)
                {
                    closestDist = hits[i].distance;
                    hit = hits[i];
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// 判断是否是玩家碰撞体
        /// </summary>
        /// <param name="col">碰撞体</param>
        /// <returns>是否是玩家碰撞体</returns>
        private bool IsPlayerCollider(Collider col)
        {
            if (playerCollider == null)
                return false;

            var t = col.transform;
            return t == playerCollider.transform || t.IsChildOf(playerCollider.transform);
        }

        /// <summary>
        /// 命中面外侧空格 作为占格起始格
        /// </summary>
        private Vector3Int GetTargetCell(RaycastHit hit)
        {
            // 向外偏移 防止穿模 = 1* 0.01f
            var eps = virtualGrid.gridUnitSize * PLACEMENT_EPSILON;
            // 向命中点的法线反方向偏移一点点 防止抖动
            var hitPoint = hit.point - hit.normal * eps;
            // 世界转网格坐标
            var hitCell = virtualGrid.WorldToGrid(hitPoint, clampToBounds: false);
            // 网格坐标 + 法线方向(一般是单位向量) 得到目标格
            return hitCell + Vector3Int.RoundToInt(hit.normal);
        }

        #endregion

        #region 处理输入
        private bool HandleInput(bool canPlace)
        {
            if (!canPlace) return false;
            //放置
            if (Mouse.current.leftButton.wasPressedThisFrame)
                return true;
            //销毁
            if (Mouse.current.rightButton.wasPressedThisFrame)
                return true;
            return false;
        }
        #endregion

        #region 放置销毁

        /// <summary>
        /// 校验占格边界 占用 玩家碰撞
        /// </summary>
        private bool ValidatePlacement(CubePlacementInfo placement)
        {
            // 获取占格列表
            placement.FillOccupiedCells(occupiedList);

            // 遍历占格列表
            foreach (var cell in occupiedList)
            {
                //验证是否超出边界
                if (!virtualGrid.ValidBoundary(cell))
                    return false;

                //验证是否已有方块
                if (chunkSystem != null)
                {
                    if (chunkSystem.HasCube(cell))
                        return false;
                }
                else if (cubeMgrDict.ContainsKey(cell))
                {
                    return false;
                }
            }

            //验证是否与玩家碰撞体重叠
            if (playerCollider != null && playerCollider.bounds.Intersects(placement.CubeWorldBounds))
                return false;

            return true;
        }

        /// <summary>
        /// 放置方块
        /// </summary>
        private void HandlePlaceCube(CubePlacementInfo placement, CubeData cubeData)
        {
            // 获取占格列表
            placement.FillOccupiedCells(occupiedList);
            // 创建方块
            var spawnedObj = TryCreatCube(placement, cubeData);
            // 单位方块不单独存占格列表
            var occupiedGridList = cubeData.IsUnit ? null : new List<Vector3Int>(occupiedList);
            // 保存数据到缓存
            HandleDataToCache(placement, cubeData, spawnedObj, occupiedGridList);
        }

        /// <summary>
        /// 创建方块
        /// </summary>
        private GameObject TryCreatCube(CubePlacementInfo placement, CubeData cubeData)
        {
            var prefab = cubeData.CubePrefab;
            if (!prefab)
            {
                print("得不到预制体" + cubeData.CubePrefab);
                return null;
            }

            var spawnedObj = GameObject.Instantiate(
                prefab,
                placement.CubeWorldCenter,
                Quaternion.identity,
                cubeRoot);
            spawnedObj.layer = config.cubeLayer;
            return spawnedObj;
        }

        /// <summary>
        /// 保存数据到内存
        /// </summary>
        private void HandleDataToCache(CubePlacementInfo placement,
                                CubeData cubeData,
                                GameObject spawnedObj,
                                List<Vector3Int> occupiedGridList)
        {
            var runtimeData = new CubeRuntimeData(cubeData, spawnedObj, occupiedGridList);
            // 如果占格列表为空 则认为是单位方块 直接添加到字典中
            if (occupiedGridList == null)
            {
                cubeMgrDict.Add(placement.OriginPoint, runtimeData);
                chunkSystem?.AddCube(placement.OriginPoint, cubeData);
                return;
            }

            // 如果占格列表不为空 则认为是非单位方块 则遍历占格列表 逐个添加到字典中
            foreach (var pos in occupiedGridList)
            {
                cubeMgrDict.Add(pos, runtimeData);
                chunkSystem?.AddCube(pos, cubeData);
            }
        }

        /// <summary>
        /// 破坏方块
        /// </summary>
        private void BreakCube(Vector3Int gridPos)
        {
            if (!cubeMgrDict.TryGetValue(gridPos, out var data)) return;

            //销毁方块物体
            if (data.spawnedObj != null) Destroy(data.spawnedObj);

            if (data.occupiedGrids == null)
            {
                cubeMgrDict.Remove(gridPos);
                chunkSystem?.RemoveCube(gridPos);
                return;
            }

            //删除所有占用网格
            foreach (var pos in data.occupiedGrids)
            {
                if (cubeMgrDict.ContainsKey(pos)) cubeMgrDict.Remove(pos);
                chunkSystem?.RemoveCube(pos);
            }
        }
        #endregion


    }
}

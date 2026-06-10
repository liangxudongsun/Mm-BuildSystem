using System.Collections.Generic;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 方块数据实体
    /// </summary>
    public class CubeRuntimeData
    {
        public CubeData data;
        public GameObject spawnedObj;
        public List<Vector3Int> occupiedGrids;

        public CubeRuntimeData(CubeData data, GameObject spawnedObj, List<Vector3Int> occupiedGrids)
        {
            this.data = data;
            this.spawnedObj = spawnedObj;
            this.occupiedGrids = occupiedGrids;
        }
    }
}

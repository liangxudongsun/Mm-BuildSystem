using UnityEngine;
using Sirenix.OdinInspector;

namespace Mm_Budier
{


    [CreateAssetMenu(fileName = "NewCube", menuName = "Mm_Builder/CubeData")]
    public class CubeData : SerializedScriptableObject
    {
        [LabelText("类型")] 
        public ECubeType CubeType;
        [LabelText("预制体")] 
        public GameObject CubePrefab;

    }

}

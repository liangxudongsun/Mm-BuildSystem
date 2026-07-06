using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    [CreateAssetMenu(fileName = "PaintedBuildingBrushPreset", menuName = "Mm_ProceduralBuilding/PaintedBuildingBrushPreset")]
    public class PaintedBuildingBrushPreset : SerializedScriptableObject
    {
        /// <summary>
        /// 格子类型
        /// </summary>
        [LabelText("格子类型")]
        public EPaintedBuildingCellType cellType = EPaintedBuildingCellType.Wall;

        /// <summary>
        /// 预览颜色
        /// </summary>
        [LabelText("预览颜色")]
        public Color previewColor = Color.red;

        /// <summary>
        /// 默认墙体高度
        /// </summary>
        [LabelText("默认墙体高度")]
        [MinValue(1)]
        public int defaultHeightGridCount = 3;

        /// <summary>
        /// 生成材质
        /// </summary>
        [LabelText("生成材质")]
        public Material material;

        /// <summary>
        /// 校验参数
        /// </summary>
        private void OnValidate()
        {
            defaultHeightGridCount = Mathf.Max(1, defaultHeightGridCount);
        }
    }
}

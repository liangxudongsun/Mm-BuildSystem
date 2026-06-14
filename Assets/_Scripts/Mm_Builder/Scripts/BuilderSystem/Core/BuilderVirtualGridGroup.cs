using Sirenix.OdinInspector;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 可放置分区 原点格加尺寸格描述轴对齐长方体
/// 格坐标左闭右开 origin 到 origin加size
/// </summary>
[Serializable]
public class BuilderVirtualGridGroup
{
    [LabelText("编号")] public string id;
    [LabelText("允许放置")] public bool allowPlacement = true;
    [LabelText("原点(格)")] public Vector3Int originCell;
    [LabelText("尺寸(格)"), MinValue(1)] public Vector3Int sizeCells = Vector3Int.one;

    /// <summary>
    /// 算出分区格坐标边界 min含 max不含
    /// Contains 与 DrawGizmo 共用
    /// </summary>
    public void GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
    {
        minX = originCell.x;
        maxX = originCell.x + Mathf.Max(1, sizeCells.x);
        minY = originCell.y;
        maxY = originCell.y + Mathf.Max(1, sizeCells.y);
        minZ = originCell.z;
        maxZ = originCell.z + Mathf.Max(1, sizeCells.z);
    }

    /// <summary>
    /// 格坐标是否落在此分区内
    /// 运行时 ValidPlacementCell 用来判断能不能放
    /// </summary>
    public bool Contains(Vector3Int gridPos)
    {
        GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
        return gridPos.x >= minX && gridPos.x < maxX
            && gridPos.y >= minY && gridPos.y < maxY
            && gridPos.z >= minZ && gridPos.z < maxZ;
    }

    /// <summary>
    /// 深拷贝一份分区数据
    /// Editor 读写 grid-groups.json 时避免改到场景里那份引用
    /// </summary>
    public BuilderVirtualGridGroup Clone()
    {
        return new BuilderVirtualGridGroup
        {
            id = id,
            allowPlacement = allowPlacement,
            originCell = originCell,
            sizeCells = new Vector3Int(
                Mathf.Max(1, sizeCells.x),
                Mathf.Max(1, sizeCells.y),
                Mathf.Max(1, sizeCells.z)),
        };
    }

    /// <summary>
    /// Scene 视图绘制此分区线框
    /// 由 BuilderVirtualGrid.OnDrawGizmos 调用
    /// </summary>
    public void DrawGizmo(
        int gridUnitSize,
        bool showGridColor,
        bool showGridHeight,
        bool showYAxisColor,
        Color gridColor,
        Color yAxisColor,
        float planeYOffset,
        Color heightLabelColor,
        int heightLabelFontSize)
    {
        GetGridBounds(out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
        GridGizmoDraw.DrawRegionBounds(
            minX, maxX, minY, maxY, minZ, maxZ,
            gridUnitSize, planeYOffset,
            showGridColor, showGridHeight, showYAxisColor,
            gridColor, yAxisColor,
            showGridHeight ? sizeCells.y : 0,
            heightLabelColor, heightLabelFontSize,
            id);
    }
}

/// <summary>
/// 分区与全局网格的 Scene Gizmo 绘制
/// 仅 Editor 可视化 不参与运行时逻辑
/// </summary>
public static class GridGizmoDraw
{
    /// <summary>
    /// 按格边界画线框 底面或立体框 竖线 高度文字
    /// </summary>
    public static void DrawRegionBounds(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ,
        float unitSize, float planeYOffset,
        bool showGridColor, bool showGridHeight, bool showYAxisColor,
        Color gridColor, Color yAxisColor,
        int heightInCells,
        Color heightLabelColor,
        int heightLabelFontSize,
        string regionLabel = null)
    {
        if (unitSize <= 0 || maxX <= minX || maxZ <= minZ || maxY <= minY)
            return;

        if (showGridColor)
        {
            Gizmos.color = gridColor;
            if (showGridHeight)
            {
                var center = new Vector3(
                    (minX + maxX) * 0.5f * unitSize,
                    (minY + maxY) * 0.5f * unitSize,
                    (minZ + maxZ) * 0.5f * unitSize);
                var size = new Vector3(
                    (maxX - minX) * unitSize,
                    (maxY - minY) * unitSize,
                    (maxZ - minZ) * unitSize);
                Gizmos.DrawWireCube(center, size);
            }
            else
            {
                DrawFloorRect(minX, maxX, minZ, maxZ, minY * unitSize + planeYOffset, unitSize);
            }
        }

        if (showGridHeight && showYAxisColor)
        {
            Gizmos.color = yAxisColor;
            DrawCornerPillars(minX, maxX, minY, maxY, minZ, maxZ, unitSize);
        }

        if (showGridHeight && heightInCells > 0)
        {
            DrawHeightLabel(
                minX, maxX, minY, maxY, minZ, maxZ, unitSize,
                heightInCells, heightLabelColor, heightLabelFontSize, regionLabel);
        }
    }

#if UNITY_EDITOR
    static GUIStyle heightLabelStyle;

    /// <summary>
    /// 分区右上角显示层数或编号
    /// </summary>
    static void DrawHeightLabel(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ,
        float unitSize, int heightInCells,
        Color color, int fontSize, string regionLabel)
    {
        var labelPos = new Vector3(
            (minX + maxX) * 0.5f * unitSize,
            maxY * unitSize + unitSize * 0.2f,
            maxZ * unitSize + unitSize * 0.15f);

        heightLabelStyle ??= new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        heightLabelStyle.fontSize = fontSize;
        heightLabelStyle.normal.textColor = color;

        var text = string.IsNullOrWhiteSpace(regionLabel)
            ? heightInCells.ToString()
            : $"{regionLabel}\n{heightInCells}";

        Handles.Label(labelPos, text, heightLabelStyle);
    }
#endif

    /// <summary>
    /// 只画分区底面矩形轮廓
    /// showGridHeight 关闭时用 避免立体框挡视线
    /// </summary>
    static void DrawFloorRect(int minX, int maxX, int minZ, int maxZ, float worldY, float unitSize)
    {
        float x0 = minX * unitSize;
        float x1 = maxX * unitSize;
        float z0 = minZ * unitSize;
        float z1 = maxZ * unitSize;

        Gizmos.DrawLine(new Vector3(x0, worldY, z0), new Vector3(x1, worldY, z0));
        Gizmos.DrawLine(new Vector3(x1, worldY, z0), new Vector3(x1, worldY, z1));
        Gizmos.DrawLine(new Vector3(x1, worldY, z1), new Vector3(x0, worldY, z1));
        Gizmos.DrawLine(new Vector3(x0, worldY, z1), new Vector3(x0, worldY, z0));
    }

    /// <summary>
    /// 分区四角竖线 标出 Y 方向高度跨度
    /// </summary>
    static void DrawCornerPillars(
        int minX, int maxX, int minY, int maxY, int minZ, int maxZ, float unitSize)
    {
        float y0 = minY * unitSize;
        float y1 = maxY * unitSize;
        float x0 = minX * unitSize;
        float x1 = maxX * unitSize;
        float z0 = minZ * unitSize;
        float z1 = maxZ * unitSize;

        Gizmos.DrawLine(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0));
        Gizmos.DrawLine(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0));
        Gizmos.DrawLine(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1));
        Gizmos.DrawLine(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1));
    }
}

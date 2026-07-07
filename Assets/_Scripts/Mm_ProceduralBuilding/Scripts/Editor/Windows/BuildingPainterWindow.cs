#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mm_ProceduralBuilding.Editor
{
    public class BuildingPainterWindow : EditorWindow
    {
        private const string PlanAssetPath = "Assets/_Scripts/Mm_ProceduralBuilding/So/PaintedBuildingPlan.asset";
        private const string ConventionAssetPath = "Assets/_Scripts/Mm_ProceduralBuilding/So/BuildingGridConvention.asset";
        private const string BrushPresetFolderPath = "Assets/_Scripts/Mm_ProceduralBuilding/So/BrushPresets";
        private const string PlanPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.PlanPath";
        private const string GeneratorPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.GeneratorId";
        private const string LeftWidthPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.LeftWidth";
        private const string RightWidthPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.RightWidth";
        private const string ToolPanelTabPrefsKey = "Mm_ProceduralBuilding.BuildingPainter.ToolPanelTab";
        private const float MinCellPixelSize = 10f;
        private const float MaxCellPixelSize = 64f;
        private const float MinSideWidth = 130f;
        private const float MaxSideWidth = 360f;
        private const float SplitterWidth = 5f;

        /// <summary>
        /// 绘制蓝图
        /// </summary>
        [SerializeField]
        private PaintedBuildingPlan paintedPlan;

        /// <summary>
        /// 绘制生成器
        /// </summary>
        [SerializeField]
        private PaintedBuildingGenerator generator;

        /// <summary>
        /// 格子公约
        /// </summary>
        [SerializeField]
        private BuildingGridConvention convention;

        /// <summary>
        /// 笔刷预设列表
        /// </summary>
        [SerializeField]
        private List<PaintedBuildingBrushPreset> brushPresetList = new();

        /// <summary>
        /// 当前笔刷预设
        /// </summary>
        [SerializeField]
        private PaintedBuildingBrushPreset currentBrushPreset;

        /// <summary>
        /// 当前楼层
        /// </summary>
        [SerializeField]
        private int currentFloorIndex;

        /// <summary>
        /// 当前格子类型
        /// </summary>
        [SerializeField]
        private EPaintedBuildingCellType currentCellType = EPaintedBuildingCellType.Wall;

        /// <summary>
        /// 墙体高度格数
        /// </summary>
        [SerializeField]
        private int wallHeightGridCount = 3;

        /// <summary>
        /// 挖空起点高度
        /// </summary>
        [SerializeField]
        private int cutoutStartHeightGridCount;

        /// <summary>
        /// 挖空终点高度
        /// </summary>
        [SerializeField]
        private int cutoutEndHeightGridCount = 2;

        /// <summary>
        /// 地面填充左下角
        /// </summary>
        [SerializeField]
        private Vector2Int floorFillBottomLeftGridPos;

        /// <summary>
        /// 地面填充右上角
        /// </summary>
        [SerializeField]
        private Vector2Int floorFillTopRightGridPos = new Vector2Int(5, 5);

        /// <summary>
        /// 墙体厚度格数
        /// </summary>
        [SerializeField]
        private int wallThicknessGridCount = 1;

        /// <summary>
        /// 墙体延伸方向
        /// </summary>
        [SerializeField]
        private EWallExtendDirection wallExtendDirection = EWallExtendDirection.Outward;

        /// <summary>
        /// 生成前清空楼层
        /// </summary>
        [SerializeField]
        private bool roomClearBeforeGenerate;

        /// <summary>
        /// 房间锚点格坐标
        /// </summary>
        [SerializeField]
        private Vector2Int roomAnchorGridPos;

        /// <summary>
        /// 房间宽度格数
        /// </summary>
        [SerializeField]
        private int roomWidthGridCount = 6;

        /// <summary>
        /// 房间深度格数
        /// </summary>
        [SerializeField]
        private int roomDepthGridCount = 6;

        /// <summary>
        /// 房间是否带门
        /// </summary>
        [SerializeField]
        private bool roomEnableDoor = true;

        /// <summary>
        /// 房间门所在墙面
        /// </summary>
        [SerializeField]
        private ERoomDoorWallSide roomDoorWallSide = ERoomDoorWallSide.Down;

        /// <summary>
        /// 房间门偏移格数
        /// </summary>
        [SerializeField]
        private int roomDoorOffsetGridCount = 2;

        /// <summary>
        /// 房间门宽格数
        /// </summary>
        [SerializeField]
        private int roomDoorWidthGridCount = 1;

        /// <summary>
        /// 阵列行数
        /// </summary>
        [SerializeField]
        private int roomGridRowCount = 2;

        /// <summary>
        /// 阵列列数
        /// </summary>
        [SerializeField]
        private int roomGridColumnCount = 2;

        /// <summary>
        /// 走廊宽度格数
        /// </summary>
        [SerializeField]
        private int roomCorridorWidthGridCount = 1;

        /// <summary>
        /// 阵列门模式
        /// </summary>
        [SerializeField]
        private ERoomGridDoorMode roomGridDoorMode = ERoomGridDoorMode.Same;

        /// <summary>
        /// 阵列门随机种子
        /// </summary>
        [SerializeField]
        private int roomGridDoorRandomSeed = 12345;

        /// <summary>
        /// 单格像素大小
        /// </summary>
        [SerializeField]
        private float cellPixelSize = 24f;

        /// <summary>
        /// 左侧栏宽度
        /// </summary>
        [SerializeField]
        private float leftPanelWidth = 180f;

        /// <summary>
        /// 右侧栏宽度
        /// </summary>
        [SerializeField]
        private float rightPanelWidth = 260f;

        /// <summary>
        /// 网格平移
        /// </summary>
        [SerializeField]
        private Vector2 gridPanOffset;

        /// <summary>
        /// 鼠标悬停格子
        /// </summary>
        private Vector2Int hoverGridPos;

        /// <summary>
        /// 是否存在悬停格子
        /// </summary>
        private bool hasHoverGridPos;

        /// <summary>
        /// 是否正在拖拽平移
        /// </summary>
        private bool isPanning;

        /// <summary>
        /// 是否正在框选绘制
        /// </summary>
        private bool isSelectingCells;

        /// <summary>
        /// 框选起点格子
        /// </summary>
        private Vector2Int selectionStartGridPos;

        /// <summary>
        /// 框选终点格子
        /// </summary>
        private Vector2Int selectionEndGridPos;

        /// <summary>
        /// 是否正在拖拽左侧分隔条
        /// </summary>
        private bool isDraggingLeftSplitter;

        /// <summary>
        /// 是否正在拖拽右侧分隔条
        /// </summary>
        private bool isDraggingRightSplitter;

        /// <summary>
        /// 上次鼠标位置
        /// </summary>
        private Vector2 lastMousePos;

        /// <summary>
        /// 笔刷滚动位置
        /// </summary>
        private Vector2 brushScrollPos;

        /// <summary>
        /// 绘制页签滚动位置
        /// </summary>
        private Vector2 toolPaintScrollPos;

        /// <summary>
        /// 通用页签滚动位置
        /// </summary>
        private Vector2 toolGeneralScrollPos;

        /// <summary>
        /// 工具面板页签索引
        /// </summary>
        private int toolPanelTabIndex;

        /// <summary>
        /// 是否存在未保存绘制数据
        /// </summary>
        private bool hasDirtyPaintData;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/Mm_ProceduralBuilding/建筑画笔")]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildingPainterWindow>();
            window.titleContent = new GUIContent("建筑画笔");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        /// <summary>
        /// 启用窗口
        /// </summary>
        private void OnEnable()
        {
            leftPanelWidth = EditorPrefs.GetFloat(LeftWidthPrefsKey, leftPanelWidth);
            rightPanelWidth = EditorPrefs.GetFloat(RightWidthPrefsKey, rightPanelWidth);
            toolPanelTabIndex = EditorPrefs.GetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            EnsureDefaultAssets();
            LoadPersistedReferences();
            EnsureConventionReference();
            SyncWallThicknessFromConvention();
            SyncGlobalSettingsFromPlan();
            SyncGeneratorReferences();
        }

        /// <summary>
        /// 禁用窗口
        /// </summary>
        private void OnDisable()
        {
            PersistReferences();
            EditorPrefs.SetFloat(LeftWidthPrefsKey, leftPanelWidth);
            EditorPrefs.SetFloat(RightWidthPrefsKey, rightPanelWidth);
            EditorPrefs.SetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 绘制窗口
        /// </summary>
        private void OnGUI()
        {
            if (paintedPlan == null || brushPresetList.Count == 0)
                EnsureDefaultAssets();

            Rect windowRect = new Rect(0f, 0f, position.width, position.height);
            ClampPanelWidths(windowRect.width);
            Rect brushRect = new Rect(0f, 0f, leftPanelWidth, windowRect.height);
            Rect leftSplitterRect = new Rect(brushRect.xMax, 0f, SplitterWidth, windowRect.height);
            Rect toolRect = new Rect(windowRect.width - rightPanelWidth, 0f, rightPanelWidth, windowRect.height);
            Rect rightSplitterRect = new Rect(toolRect.x - SplitterWidth, 0f, SplitterWidth, windowRect.height);
            Rect gridRect = new Rect(leftSplitterRect.xMax, 0f, rightSplitterRect.x - leftSplitterRect.xMax, windowRect.height);

            DrawPanelBackground(brushRect, new Color(0.18f, 0.18f, 0.18f));
            DrawPanelBackground(gridRect, new Color(0.12f, 0.12f, 0.12f));
            DrawPanelBackground(toolRect, new Color(0.18f, 0.18f, 0.18f));
            DrawPanelBackground(leftSplitterRect, new Color(0.06f, 0.06f, 0.06f));
            DrawPanelBackground(rightSplitterRect, new Color(0.06f, 0.06f, 0.06f));

            DrawBrushPanel(brushRect);
            DrawGridPanel(gridRect);
            DrawToolPanel(toolRect);
            HandleSplitterInput(windowRect, leftSplitterRect, rightSplitterRect);
            HandleKeyboardInput();
        }

        /// <summary>
        /// 绘制面板背景
        /// </summary>
        private void DrawPanelBackground(Rect rect, Color color)
        {
            EditorGUI.DrawRect(rect, color);
        }

        /// <summary>
        /// 绘制笔刷面板
        /// </summary>
        private void DrawBrushPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            brushScrollPos = EditorGUILayout.BeginScrollView(brushScrollPos);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("笔刷", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset == null)
                    continue;

                DrawBrushButton(brushPreset);
            }

            GUILayout.FlexibleSpace();
            DrawBrushGlobalSettings();
            EditorGUILayout.Space(4f);
            if (GUILayout.Button("刷新默认笔刷", GUILayout.Height(28f)))
            {
                EnsureAllBrushPresets();
                LoadBrushPresetList();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制笔刷全局设置
        /// </summary>
        private void DrawBrushGlobalSettings()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("笔刷设置", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            currentFloorIndex = Mathf.Max(0, EditorGUILayout.IntField("当前楼层", currentFloorIndex));
            if (paintedPlan != null)
                EditorGUILayout.LabelField("当前地面高度", $"Y = {paintedPlan.GetFloorBaseY(currentFloorIndex)}");

            wallHeightGridCount = Mathf.Max(1, EditorGUILayout.IntField("全局墙体高度", wallHeightGridCount));
            if (EditorGUI.EndChangeCheck())
            {
                cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(
                    cutoutEndHeightGridCount,
                    cutoutStartHeightGridCount + 1,
                    wallHeightGridCount);

                if (paintedPlan != null && paintedPlan.globalWallHeightGridCount != wallHeightGridCount)
                {
                    Undo.RecordObject(paintedPlan, "修改全局墙体高度");
                    paintedPlan.globalWallHeightGridCount = wallHeightGridCount;
                    EditorUtility.SetDirty(paintedPlan);
                }
            }

            if (currentFloorIndex > 0)
            {
                if (GUILayout.Button("复制上一层布局", GUILayout.Height(22f)))
                    CopyPreviousFloorLayout();
            }
        }

        /// <summary>
        /// 绘制笔刷按钮
        /// </summary>
        private void DrawBrushButton(PaintedBuildingBrushPreset brushPreset)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 34f, GUILayout.ExpandWidth(true));
            bool isSelected = currentBrushPreset == brushPreset;
            Color color = brushPreset.previewColor;
            Color backgroundColor = isSelected
                ? new Color(color.r, color.g, color.b, 0.75f)
                : new Color(color.r, color.g, color.b, 0.35f);
            EditorGUI.DrawRect(rect, backgroundColor);

            Rect labelRect = new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 20f);
            GUI.Label(labelRect, GetBrushLabel(brushPreset), EditorStyles.boldLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                SelectBrush(brushPreset);
                Event.current.Use();
            }
        }

        /// <summary>
        /// 绘制网格面板
        /// </summary>
        private void DrawGridPanel(Rect rect)
        {
            DrawGridHeader(rect);
            Rect gridRect = new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, rect.height - 50f);
            EditorGUI.DrawRect(gridRect, new Color(0.1f, 0.1f, 0.1f));

            GUI.BeginGroup(gridRect);
            Rect localGridRect = new Rect(0f, 0f, gridRect.width, gridRect.height);
            Event e = Event.current;
            UpdateHoverGridPos(localGridRect, e.mousePosition);
            DrawGridLines(localGridRect);
            DrawPaintedCells(localGridRect);
            DrawSelectionRect(localGridRect);
            DrawHoverCell(localGridRect);
            DrawRoomHoverPreview(localGridRect);
            HandleGridInput(localGridRect, e);
            GUI.EndGroup();
        }

        /// <summary>
        /// 绘制网格标题
        /// </summary>
        private void DrawGridHeader(Rect rect)
        {
            Rect titleRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 24f);
            string hoverText = hasHoverGridPos ? $"  格子 {hoverGridPos.x} {hoverGridPos.y}" : string.Empty;
            GUI.Label(titleRect, $"网格  楼层 {currentFloorIndex}{hoverText}", EditorStyles.boldLabel);
        }

        /// <summary>
        /// 绘制工具面板
        /// </summary>
        private void DrawToolPanel(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.Space(6f);
            int newToolPanelTabIndex = GUILayout.Toolbar(toolPanelTabIndex, new[] { "绘制", "通用" });
            if (newToolPanelTabIndex != toolPanelTabIndex)
            {
                toolPanelTabIndex = newToolPanelTabIndex;
                EditorPrefs.SetInt(ToolPanelTabPrefsKey, toolPanelTabIndex);
            }

            EditorGUILayout.Space(4f);
            if (toolPanelTabIndex == 0)
                DrawToolPaintTab();
            else
                DrawToolGeneralTab();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 绘制工具页签
        /// </summary>
        private void DrawToolPaintTab()
        {
            toolPaintScrollPos = EditorGUILayout.BeginScrollView(toolPaintScrollPos);
            EditorGUILayout.Space(4f);
            DrawBrushSettings();
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(GetGridHelpMessage(), MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制通用页签
        /// </summary>
        private void DrawToolGeneralTab()
        {
            toolGeneralScrollPos = EditorGUILayout.BeginScrollView(toolGeneralScrollPos);
            EditorGUILayout.Space(4f);
            DrawAssetFields();
            EditorGUILayout.Space(8f);
            DrawActionButtons();
            EditorGUILayout.Space(8f);
            DrawViewSettings();
            EditorGUILayout.Space(8f);
            DrawOptimizationPanel();
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制资产字段
        /// </summary>
        private void DrawAssetFields()
        {
            EditorGUILayout.LabelField("持久化资产", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            paintedPlan = (PaintedBuildingPlan)EditorGUILayout.ObjectField("绘制蓝图", paintedPlan, typeof(PaintedBuildingPlan), false);
            generator = (PaintedBuildingGenerator)EditorGUILayout.ObjectField("生成器", generator, typeof(PaintedBuildingGenerator), true);
            if (EditorGUI.EndChangeCheck())
            {
                EnsureConventionReference();
                SyncWallThicknessFromConvention();
                SyncGlobalSettingsFromPlan();
                SyncGeneratorReferences();
                PersistReferences();
            }
        }

        /// <summary>
        /// 绘制笔刷设置
        /// </summary>
        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("当前笔刷", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            currentBrushPreset = (PaintedBuildingBrushPreset)EditorGUILayout.ObjectField("笔刷预设", currentBrushPreset, typeof(PaintedBuildingBrushPreset), false);
            if (EditorGUI.EndChangeCheck() && currentBrushPreset != null)
                ApplyBrushPreset(currentBrushPreset);

            currentCellType = (EPaintedBuildingCellType)EditorGUILayout.EnumPopup("绘制类型", currentCellType);

            if (currentCellType == EPaintedBuildingCellType.Floor)
                DrawFloorBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Wall)
                DrawWallBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Cutout)
            {
                cutoutStartHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空起点高度", cutoutStartHeightGridCount), 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空终点高度", cutoutEndHeightGridCount), cutoutStartHeightGridCount + 1, wallHeightGridCount);
                EditorGUILayout.HelpBox("挖空从地面上方的墙体开始计算 地面层不会被墙体或挖空覆盖", MessageType.Info);
            }

            if (currentCellType == EPaintedBuildingCellType.Room)
                DrawRoomBrushTools();

            if (currentCellType == EPaintedBuildingCellType.Erase)
                DrawEraseBrushTools();
        }

        /// <summary>
        /// 绘制擦除笔刷工具
        /// </summary>
        private void DrawEraseBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("右键可逐格擦除 下方按钮可一键清空当前楼层全部地面和结构", MessageType.None);
            if (GUILayout.Button("一键清空当前楼层网格", GUILayout.Height(28f)))
                ClearCurrentFloorGrid();
        }

        /// <summary>
        /// 绘制地面笔刷工具
        /// </summary>
        private void DrawFloorBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("范围填充", EditorStyles.boldLabel);
            floorFillBottomLeftGridPos = EditorGUILayout.Vector2IntField("左下角坐标", floorFillBottomLeftGridPos);
            floorFillTopRightGridPos = EditorGUILayout.Vector2IntField("右上角坐标", floorFillTopRightGridPos);
            if (GUILayout.Button("填充地面范围", GUILayout.Height(28f)))
                FillFloorRange();
        }

        /// <summary>
        /// 绘制墙体笔刷工具
        /// </summary>
        private void DrawWallBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("一键圈墙", EditorStyles.boldLabel);
            wallThicknessGridCount = Mathf.Max(1, EditorGUILayout.IntField("墙体厚度", wallThicknessGridCount));
            wallExtendDirection = (EWallExtendDirection)EditorGUILayout.EnumPopup("延伸方向", wallExtendDirection);
            EditorGUILayout.HelpBox("基于当前楼层地面最外围圈墙 厚度包含最外圈本身", MessageType.None);
            if (GUILayout.Button("一键绘制墙体", GUILayout.Height(28f)))
                PaintPerimeterWalls();
        }

        /// <summary>
        /// 绘制房间笔刷工具
        /// </summary>
        private void DrawRoomBrushTools()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "房间笔刷 = 矩形地面 + 四周一圈墙 + 可选门洞\n" +
                "左键点击网格放置单个房间 或使用下方按钮生成阵列",
                MessageType.Info);

            roomClearBeforeGenerate = EditorGUILayout.Toggle("生成前清空当前楼层", roomClearBeforeGenerate);
            roomAnchorGridPos = EditorGUILayout.Vector2IntField("房间左下角", roomAnchorGridPos);
            roomWidthGridCount = Mathf.Max(2, EditorGUILayout.IntField("房间宽度", roomWidthGridCount));
            roomDepthGridCount = Mathf.Max(2, EditorGUILayout.IntField("房间深度", roomDepthGridCount));
            wallThicknessGridCount = Mathf.Max(1, EditorGUILayout.IntField("墙体厚度", wallThicknessGridCount));
            wallExtendDirection = (EWallExtendDirection)EditorGUILayout.EnumPopup("延伸方向", wallExtendDirection);
            roomEnableDoor = EditorGUILayout.Toggle("生成门洞", roomEnableDoor);

            if (roomEnableDoor)
            {
                roomDoorWallSide = (ERoomDoorWallSide)EditorGUILayout.EnumPopup("门所在方向", roomDoorWallSide);
                roomDoorOffsetGridCount = Mathf.Max(0, EditorGUILayout.IntField("门沿墙偏移", roomDoorOffsetGridCount));
                roomDoorWidthGridCount = Mathf.Max(1, EditorGUILayout.IntField("房间门宽", roomDoorWidthGridCount));
                cutoutStartHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空起点高度", cutoutStartHeightGridCount), 0, wallHeightGridCount - 1);
                cutoutEndHeightGridCount = Mathf.Clamp(EditorGUILayout.IntField("挖空终点高度", cutoutEndHeightGridCount), cutoutStartHeightGridCount + 1, wallHeightGridCount);
            }

            if (GUILayout.Button("在左下角生成单个房间", GUILayout.Height(28f)))
                GenerateSingleRoom();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("房间阵列", EditorStyles.boldLabel);
            roomGridRowCount = Mathf.Max(1, EditorGUILayout.IntField("行数", roomGridRowCount));
            roomGridColumnCount = Mathf.Max(1, EditorGUILayout.IntField("列数", roomGridColumnCount));
            roomCorridorWidthGridCount = Mathf.Max(1, EditorGUILayout.IntField("走廊宽度", roomCorridorWidthGridCount));
            roomGridDoorMode = (ERoomGridDoorMode)EditorGUILayout.EnumPopup("阵列门模式", roomGridDoorMode);

            if (roomGridDoorMode == ERoomGridDoorMode.Symmetric)
                EditorGUILayout.HelpBox("对称模式会按阵列中心镜像门方向和偏移", MessageType.None);

            if (roomGridDoorMode == ERoomGridDoorMode.Random)
                roomGridDoorRandomSeed = EditorGUILayout.IntField("门随机种子", roomGridDoorRandomSeed);

            if (GUILayout.Button("生成房间阵列", GUILayout.Height(28f)))
                GenerateRoomGrid();
        }

        /// <summary>
        /// 绘制优化面板
        /// </summary>
        private void DrawOptimizationPanel()
        {
            EditorGUILayout.LabelField("优化", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("请先生成到场景 再执行优化操作", MessageType.None);

            if (GUILayout.Button("全部合并网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.All);

            if (GUILayout.Button("合并地面网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.Floor);

            if (GUILayout.Button("合并墙体网格", GUILayout.Height(28f)))
                RunMeshMerge(EBuildingMergeTarget.Structure);

            EditorGUILayout.Space(4f);

            if (GUILayout.Button("合并碰撞体", GUILayout.Height(28f)))
                RunCollisionMerge();

            if (GUILayout.Button("开启 GPU Instancing", GUILayout.Height(28f)))
                RunGpuInstancing();
        }

        /// <summary>
        /// 绘制视图设置
        /// </summary>
        private void DrawViewSettings()
        {
            EditorGUILayout.LabelField("视图", EditorStyles.boldLabel);
            cellPixelSize = EditorGUILayout.Slider("缩放", cellPixelSize, MinCellPixelSize, MaxCellPixelSize);
            if (GUILayout.Button("视图居中", GUILayout.Height(24f)))
                gridPanOffset = Vector2.zero;
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("生成", EditorStyles.boldLabel);
            if (GUILayout.Button("一键生成到场景", GUILayout.Height(34f)))
                GenerateBuilding();

            if (GUILayout.Button("清理场景生成物", GUILayout.Height(28f)))
                ClearGenerated();

            if (GUILayout.Button("保存蓝图资产", GUILayout.Height(24f)))
                SaveAssets();
        }

        /// <summary>
        /// 更新悬停格子
        /// </summary>
        private void UpdateHoverGridPos(Rect gridRect, Vector2 localMousePos)
        {
            hasHoverGridPos = gridRect.Contains(localMousePos);
            if (!hasHoverGridPos)
                return;

            hoverGridPos = WindowToGrid(localMousePos, gridRect);
        }

        /// <summary>
        /// 绘制网格线
        /// </summary>
        private void DrawGridLines(Rect gridRect)
        {
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(1f, 1f, 1f, 0.13f);

            Vector2 center = GetGridCenter(gridRect);
            int minX = Mathf.FloorToInt((-center.x) / cellPixelSize) - 1;
            int maxX = Mathf.CeilToInt((gridRect.width - center.x) / cellPixelSize) + 1;
            int minZ = Mathf.FloorToInt((center.y - gridRect.height) / cellPixelSize) - 1;
            int maxZ = Mathf.CeilToInt(center.y / cellPixelSize) + 1;

            for (int x = minX; x <= maxX; x++)
            {
                float pixelX = center.x + x * cellPixelSize;
                Handles.DrawLine(new Vector3(pixelX, 0f), new Vector3(pixelX, gridRect.height));
            }

            for (int z = minZ; z <= maxZ; z++)
            {
                float pixelY = center.y - z * cellPixelSize;
                Handles.DrawLine(new Vector3(0f, pixelY), new Vector3(gridRect.width, pixelY));
            }

            Handles.color = new Color(1f, 0.2f, 0.2f, 0.55f);
            Handles.DrawLine(new Vector3(center.x, 0f), new Vector3(center.x, gridRect.height));
            Handles.DrawLine(new Vector3(0f, center.y), new Vector3(gridRect.width, center.y));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 绘制已有格子
        /// </summary>
        private void DrawPaintedCells(Rect gridRect)
        {
            if (paintedPlan == null)
                return;

            var floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null)
                return;

            DrawCellList(floorData.floorCellDataList, gridRect, 0.62f);
            DrawCellList(floorData.structureCellDataList, gridRect, 0.82f);
        }

        /// <summary>
        /// 绘制格子列表
        /// </summary>
        private void DrawCellList(List<PaintedBuildingCellData> cellDataList, Rect gridRect, float alpha)
        {
            foreach (var cellData in cellDataList)
            {
                if (cellData == null)
                    continue;

                Rect cellRect = GridToWindowCellRect(cellData.gridPos, gridRect);
                if (!cellRect.Overlaps(gridRect))
                    continue;

                Color color = GetCellColor(cellData);
                EditorGUI.DrawRect(cellRect, new Color(color.r, color.g, color.b, alpha));
            }
        }

        /// <summary>
        /// 绘制房间悬停预览
        /// </summary>
        private void DrawRoomHoverPreview(Rect gridRect)
        {
            if (currentCellType != EPaintedBuildingCellType.Room || !hasHoverGridPos)
                return;

            GetGridRectBounds(
                hoverGridPos,
                new Vector2Int(
                    hoverGridPos.x + roomWidthGridCount - 1,
                    hoverGridPos.y + roomDepthGridCount - 1),
                out int minX,
                out int maxX,
                out int minZ,
                out int maxZ);
            Rect minRect = GridToWindowCellRect(new Vector2Int(minX, maxZ), gridRect);
            Rect maxRect = GridToWindowCellRect(new Vector2Int(maxX, minZ), gridRect);
            Rect previewRect = Rect.MinMaxRect(minRect.xMin, minRect.yMin, maxRect.xMax, maxRect.yMax);
            Color color = GetCurrentBrushColor();
            EditorGUI.DrawRect(previewRect, new Color(color.r, color.g, color.b, 0.22f));
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(color.r, color.g, color.b, 0.95f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(previewRect.xMin, previewRect.yMin),
                new Vector3(previewRect.xMax, previewRect.yMin),
                new Vector3(previewRect.xMax, previewRect.yMax),
                new Vector3(previewRect.xMin, previewRect.yMax),
                new Vector3(previewRect.xMin, previewRect.yMin));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 获取网格帮助信息
        /// </summary>
        private string GetGridHelpMessage()
        {
            if (currentCellType == EPaintedBuildingCellType.Room)
                return "左键点击网格放置房间 右键擦除 中键拖拽平移 滚轮缩放";

            return "在中间网格左键绘制 右键擦除 中键拖拽平移 滚轮缩放";
        }

        /// <summary>
        /// 绘制悬停格子
        /// </summary>
        private void DrawHoverCell(Rect gridRect)
        {
            if (!hasHoverGridPos)
                return;

            if (currentCellType == EPaintedBuildingCellType.Room)
                return;

            Rect cellRect = GridToWindowCellRect(hoverGridPos, gridRect);
            Color color = GetCurrentBrushColor();
            EditorGUI.DrawRect(cellRect, new Color(color.r, color.g, color.b, 0.35f));
        }

        /// <summary>
        /// 绘制框选区域
        /// </summary>
        private void DrawSelectionRect(Rect gridRect)
        {
            if (!isSelectingCells)
                return;

            GetGridRectBounds(selectionStartGridPos, selectionEndGridPos, out int minX, out int maxX, out int minZ, out int maxZ);
            Rect minRect = GridToWindowCellRect(new Vector2Int(minX, maxZ), gridRect);
            Rect maxRect = GridToWindowCellRect(new Vector2Int(maxX, minZ), gridRect);
            Rect selectionRect = Rect.MinMaxRect(minRect.xMin, minRect.yMin, maxRect.xMax, maxRect.yMax);
            Color color = GetCurrentBrushColor();
            EditorGUI.DrawRect(selectionRect, new Color(color.r, color.g, color.b, 0.18f));
            Handles.BeginGUI();
            Color oldColor = Handles.color;
            Handles.color = new Color(color.r, color.g, color.b, 1f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(selectionRect.xMin, selectionRect.yMin),
                new Vector3(selectionRect.xMax, selectionRect.yMin),
                new Vector3(selectionRect.xMax, selectionRect.yMax),
                new Vector3(selectionRect.xMin, selectionRect.yMax),
                new Vector3(selectionRect.xMin, selectionRect.yMin));
            Handles.color = oldColor;
            Handles.EndGUI();
        }

        /// <summary>
        /// 处理网格输入
        /// </summary>
        private void HandleGridInput(Rect gridRect, Event e)
        {
            if (!gridRect.Contains(e.mousePosition) && !isSelectingCells && !isPanning)
                return;

            if (e.type == EventType.ScrollWheel)
            {
                float oldCellSize = cellPixelSize;
                cellPixelSize = Mathf.Clamp(cellPixelSize - e.delta.y, MinCellPixelSize, MaxCellPixelSize);
                Vector2 pivot = e.mousePosition - GetGridCenter(gridRect);
                if (!Mathf.Approximately(oldCellSize, cellPixelSize))
                    gridPanOffset -= pivot * (cellPixelSize / oldCellSize - 1f);
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 2)
            {
                isPanning = true;
                lastMousePos = e.mousePosition;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && isPanning)
            {
                gridPanOffset += e.mousePosition - lastMousePos;
                lastMousePos = e.mousePosition;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 2)
            {
                isPanning = false;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (currentCellType == EPaintedBuildingCellType.Room)
                {
                    if (hasHoverGridPos)
                    {
                        roomAnchorGridPos = hoverGridPos;
                        GenerateSingleRoom();
                    }

                    e.Use();
                    return;
                }

                isSelectingCells = true;
                selectionStartGridPos = hoverGridPos;
                selectionEndGridPos = hoverGridPos;
                e.Use();
                return;
            }

            if (e.type == EventType.MouseDrag && e.button == 0 && isSelectingCells)
            {
                selectionEndGridPos = hoverGridPos;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseUp && e.button == 0 && isSelectingCells)
            {
                selectionEndGridPos = hoverGridPos;
                PaintRect(selectionStartGridPos, selectionEndGridPos);
                isSelectingCells = false;
                hasDirtyPaintData = false;
                SaveAssets();
                e.Use();
                return;
            }

            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 1)
            {
                EraseCell();
                e.Use();
            }

            if (e.type == EventType.MouseUp && e.button == 1 && hasDirtyPaintData)
            {
                hasDirtyPaintData = false;
                SaveAssets();
                e.Use();
            }
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.Equals || e.keyCode == KeyCode.KeypadPlus)
            {
                cellPixelSize = Mathf.Clamp(cellPixelSize + 2f, MinCellPixelSize, MaxCellPixelSize);
                e.Use();
                Repaint();
            }

            if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
            {
                cellPixelSize = Mathf.Clamp(cellPixelSize - 2f, MinCellPixelSize, MaxCellPixelSize);
                e.Use();
                Repaint();
            }
        }

        /// <summary>
        /// 处理分隔条输入
        /// </summary>
        private void HandleSplitterInput(Rect windowRect, Rect leftSplitterRect, Rect rightSplitterRect)
        {
            Event e = Event.current;
            EditorGUIUtility.AddCursorRect(leftSplitterRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightSplitterRect, MouseCursor.ResizeHorizontal);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (leftSplitterRect.Contains(e.mousePosition))
                {
                    isDraggingLeftSplitter = true;
                    e.Use();
                }

                if (rightSplitterRect.Contains(e.mousePosition))
                {
                    isDraggingRightSplitter = true;
                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag && isDraggingLeftSplitter)
            {
                leftPanelWidth = Mathf.Clamp(e.mousePosition.x, MinSideWidth, GetMaxPanelWidth(windowRect.width));
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseDrag && isDraggingRightSplitter)
            {
                rightPanelWidth = Mathf.Clamp(windowRect.width - e.mousePosition.x, MinSideWidth, GetMaxPanelWidth(windowRect.width));
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp)
            {
                isDraggingLeftSplitter = false;
                isDraggingRightSplitter = false;
                EditorPrefs.SetFloat(LeftWidthPrefsKey, leftPanelWidth);
                EditorPrefs.SetFloat(RightWidthPrefsKey, rightPanelWidth);
            }
        }

        /// <summary>
        /// 限制面板宽度
        /// </summary>
        private void ClampPanelWidths(float windowWidth)
        {
            float maxPanelWidth = GetMaxPanelWidth(windowWidth);
            leftPanelWidth = Mathf.Clamp(leftPanelWidth, MinSideWidth, maxPanelWidth);
            rightPanelWidth = Mathf.Clamp(rightPanelWidth, MinSideWidth, maxPanelWidth);
        }

        /// <summary>
        /// 获取最大面板宽度
        /// </summary>
        private float GetMaxPanelWidth(float windowWidth)
        {
            return Mathf.Min(MaxSideWidth, Mathf.Max(MinSideWidth, (windowWidth - 280f) * 0.5f));
        }

        /// <summary>
        /// 绘制格子
        /// </summary>
        private void PaintCell()
        {
            if (paintedPlan == null)
                return;

            PaintCell(hoverGridPos);
        }

        /// <summary>
        /// 绘制格子
        /// </summary>
        private void PaintCell(Vector2Int gridPos)
        {
            if (paintedPlan == null || currentCellType == EPaintedBuildingCellType.Room)
                return;

            PaintedBuildingFloorData floorData = paintedPlan.GetOrCreateFloor(currentFloorIndex);
            PaintedBuildingCellData oldCellData = currentCellType == EPaintedBuildingCellType.Floor
                ? floorData.FindFloorCell(gridPos)
                : floorData.FindStructureCell(gridPos);
            if (oldCellData != null
                && oldCellData.cellType == currentCellType
                && oldCellData.heightGridCount == wallHeightGridCount
                && oldCellData.cutoutStartHeightGridCount == cutoutStartHeightGridCount
                && oldCellData.cutoutEndHeightGridCount == cutoutEndHeightGridCount)
                return;

            paintedPlan.SetCell(
                currentFloorIndex,
                gridPos,
                currentCellType,
                wallHeightGridCount,
                cutoutStartHeightGridCount,
                cutoutEndHeightGridCount);
            EditorUtility.SetDirty(paintedPlan);
            hasDirtyPaintData = true;
            Repaint();
        }

        /// <summary>
        /// 框选绘制
        /// </summary>
        private void PaintRect(Vector2Int startGridPos, Vector2Int endGridPos)
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "框选绘制建筑格子");
            GetGridRectBounds(startGridPos, endGridPos, out int minX, out int maxX, out int minZ, out int maxZ);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    PaintCell(new Vector2Int(x, z));
                }
            }
        }

        /// <summary>
        /// 擦除格子
        /// </summary>
        private void EraseCell()
        {
            if (paintedPlan == null)
                return;

            PaintedBuildingFloorData floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null
                || (floorData.FindStructureCell(hoverGridPos) == null && floorData.FindFloorCell(hoverGridPos) == null))
                return;

            Undo.RecordObject(paintedPlan, "擦除建筑格子");
            paintedPlan.RemoveTopCell(currentFloorIndex, hoverGridPos);
            EditorUtility.SetDirty(paintedPlan);
            hasDirtyPaintData = true;
            Repaint();
        }

        /// <summary>
        /// 清空当前楼层网格
        /// </summary>
        private void ClearCurrentFloorGrid()
        {
            if (paintedPlan == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "清空网格",
                    $"确定清空楼层 {currentFloorIndex} 的全部地面和结构格子吗",
                    "清空",
                    "取消"))
                return;

            Undo.RecordObject(paintedPlan, "清空当前楼层网格");
            paintedPlan.ClearFloor(currentFloorIndex);
            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 复制上一层布局
        /// </summary>
        private void CopyPreviousFloorLayout()
        {
            if (paintedPlan == null || currentFloorIndex <= 0)
                return;

            int sourceFloorIndex = currentFloorIndex - 1;
            var sourceFloorData = paintedPlan.FindFloor(sourceFloorIndex);
            if (sourceFloorData == null
                || (sourceFloorData.floorCellDataList.Count == 0 && sourceFloorData.structureCellDataList.Count == 0))
            {
                EditorUtility.DisplayDialog("复制布局", $"楼层 {sourceFloorIndex} 没有可复制的布局", "确定");
                return;
            }

            Undo.RecordObject(paintedPlan, "复制上一层布局");
            bool copied = paintedPlan.CopyFloorLayout(sourceFloorIndex, currentFloorIndex);
            if (!copied)
            {
                EditorUtility.DisplayDialog("复制布局", "复制失败", "确定");
                return;
            }

            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 填充地面范围
        /// </summary>
        private void FillFloorRange()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "填充地面范围");
            paintedPlan.FillFloorRect(currentFloorIndex, floorFillBottomLeftGridPos, floorFillTopRightGridPos);
            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 一键绘制圈墙
        /// </summary>
        private void PaintPerimeterWalls()
        {
            if (paintedPlan == null)
                return;

            var floorData = paintedPlan.FindFloor(currentFloorIndex);
            if (floorData == null || floorData.floorCellDataList.Count == 0)
            {
                EditorUtility.DisplayDialog("一键圈墙", "当前楼层没有地面 请先绘制地面", "确定");
                return;
            }

            var floorGridPosHashList = new HashSet<Vector2Int>();
            foreach (var cellData in floorData.floorCellDataList)
            {
                if (cellData == null)
                    continue;

                floorGridPosHashList.Add(cellData.gridPos);
            }

            var wallGridPosHashList = BuildingPerimeterWallUtility.CalculateWallGridPosHashList(
                floorGridPosHashList,
                wallThicknessGridCount,
                wallExtendDirection);
            if (wallGridPosHashList.Count == 0)
            {
                EditorUtility.DisplayDialog("一键圈墙", "未能计算出墙体范围", "确定");
                return;
            }

            Undo.RecordObject(paintedPlan, "一键绘制圈墙");
            paintedPlan.SetWallCells(currentFloorIndex, wallGridPosHashList, wallHeightGridCount);
            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();
        }

        /// <summary>
        /// 确保格子公约引用
        /// </summary>
        private void EnsureConventionReference()
        {
            if (generator != null && generator.convention != null)
            {
                convention = generator.convention;
                return;
            }

            if (convention == null)
                convention = LoadOrCreateAsset<BuildingGridConvention>(ConventionAssetPath);

            if (generator != null && generator.convention == null)
            {
                generator.convention = convention;
                EditorUtility.SetDirty(generator);
            }
        }

        /// <summary>
        /// 同步墙体厚度
        /// </summary>
        private void SyncWallThicknessFromConvention()
        {
            if (convention == null)
                return;

            wallThicknessGridCount = Mathf.Max(1, convention.WallThicknessGridCount);
        }

        /// <summary>
        /// 生成单个程序化房间
        /// </summary>
        private void GenerateSingleRoom()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "生成单个房间");
            if (roomClearBeforeGenerate)
                paintedPlan.ClearFloor(currentFloorIndex);

            var config = BuildSingleRoomConfig();
            int paintedCellCount = BuildingRoomGenerator.GenerateSingleRoom(paintedPlan, currentFloorIndex, config);
            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();

            if (paintedCellCount <= 0)
                EditorUtility.DisplayDialog("房间笔刷", "生成失败 请检查参数", "确定");
        }

        /// <summary>
        /// 生成房间阵列
        /// </summary>
        private void GenerateRoomGrid()
        {
            if (paintedPlan == null)
                return;

            Undo.RecordObject(paintedPlan, "生成房间阵列");
            if (roomClearBeforeGenerate)
                paintedPlan.ClearFloor(currentFloorIndex);

            var config = BuildRoomGridConfig();
            int paintedCellCount = BuildingRoomGenerator.GenerateRoomGrid(paintedPlan, currentFloorIndex, config);
            EditorUtility.SetDirty(paintedPlan);
            SaveAssets();
            Repaint();

            if (paintedCellCount <= 0)
                EditorUtility.DisplayDialog("房间笔刷", "生成失败 请检查参数", "确定");
        }

        /// <summary>
        /// 构建单个房间配置
        /// </summary>
        private BuildingSingleRoomConfig BuildSingleRoomConfig()
        {
            return new BuildingSingleRoomConfig
            {
                anchorGridPos = roomAnchorGridPos,
                widthGridCount = roomWidthGridCount,
                depthGridCount = roomDepthGridCount,
                wallThicknessGridCount = wallThicknessGridCount,
                wallHeightGridCount = wallHeightGridCount,
                wallExtendDirection = wallExtendDirection,
                enableDoor = roomEnableDoor,
                doorWallSide = roomDoorWallSide,
                doorOffsetGridCount = roomDoorOffsetGridCount,
                roomDoorWidthGridCount = roomDoorWidthGridCount,
                cutoutStartHeightGridCount = cutoutStartHeightGridCount,
                cutoutEndHeightGridCount = cutoutEndHeightGridCount
            };
        }

        /// <summary>
        /// 构建房间阵列配置
        /// </summary>
        private BuildingRoomGridConfig BuildRoomGridConfig()
        {
            return new BuildingRoomGridConfig
            {
                anchorGridPos = roomAnchorGridPos,
                roomWidthGridCount = roomWidthGridCount,
                roomDepthGridCount = roomDepthGridCount,
                rowCount = roomGridRowCount,
                columnCount = roomGridColumnCount,
                corridorWidthGridCount = roomCorridorWidthGridCount,
                wallThicknessGridCount = wallThicknessGridCount,
                wallHeightGridCount = wallHeightGridCount,
                wallExtendDirection = wallExtendDirection,
                enableDoorPerRoom = roomEnableDoor,
                doorWallSide = roomDoorWallSide,
                doorOffsetGridCount = roomDoorOffsetGridCount,
                roomDoorWidthGridCount = roomDoorWidthGridCount,
                cutoutStartHeightGridCount = cutoutStartHeightGridCount,
                cutoutEndHeightGridCount = cutoutEndHeightGridCount,
                gridDoorMode = roomGridDoorMode,
                gridDoorRandomSeed = roomGridDoorRandomSeed
            };
        }

        /// <summary>
        /// 生成建筑
        /// </summary>
        private void GenerateBuilding()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成绘制建筑");
            generator.GenerateBuilding();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
            SaveAssets();
        }

        /// <summary>
        /// 清理生成物
        /// </summary>
        private void ClearGenerated()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "清理绘制建筑");
            generator.ClearGenerated();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
        }

        /// <summary>
        /// 执行网格合并
        /// </summary>
        private void RunMeshMerge(EBuildingMergeTarget mergeTarget)
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            if (generator.transform.Find("__PaintedBuildingGenerated") == null)
            {
                EditorUtility.DisplayDialog("合并网格", "请先生成到场景", "确定");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "合并渲染网格");
            int mergedLayerCount = generator.MergeRenderMeshes(mergeTarget);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();

            if (mergedLayerCount > 0)
                Debug.Log($"[BuildingPainterWindow] 已合并 {mergedLayerCount} 个图层网格");
            else
                EditorUtility.DisplayDialog("合并网格", "没有可合并的渲染网格", "确定");
        }

        /// <summary>
        /// 执行碰撞合并
        /// </summary>
        private void RunCollisionMerge()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            if (generator.transform.Find("__PaintedBuildingGenerated") == null)
            {
                EditorUtility.DisplayDialog("合并碰撞体", "请先生成到场景", "确定");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "合并碰撞体");
            int colliderCount = generator.MergeCollisionBoxes();
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();

            if (colliderCount > 0)
                Debug.Log($"[BuildingPainterWindow] 已生成 {colliderCount} 个合并碰撞盒");
            else
                EditorUtility.DisplayDialog("合并碰撞体", "没有可合并的碰撞体", "确定");
        }

        /// <summary>
        /// 执行 GPU Instancing
        /// </summary>
        private void RunGpuInstancing()
        {
            generator = GetOrCreateGenerator();
            if (generator == null)
                return;

            SyncGeneratorReferences();
            var generatedRoot = generator.transform.Find("__PaintedBuildingGenerated");
            if (generatedRoot == null)
            {
                EditorUtility.DisplayDialog("GPU Instancing", "请先生成到场景", "确定");
                return;
            }

            var materialHashList = new System.Collections.Generic.HashSet<Material>();
            foreach (var meshRenderer in generatedRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null)
                        materialHashList.Add(material);
                }
            }

            foreach (var material in materialHashList)
                Undo.RecordObject(material, "开启 GPU Instancing");

            BuildingGpuInstancingEditorUtility.EnableWithDialog(generatedRoot);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            PersistReferences();
        }

        /// <summary>
        /// 保存资产
        /// </summary>
        private void SaveAssets()
        {
            if (paintedPlan != null)
                EditorUtility.SetDirty(paintedPlan);

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 选择笔刷
        /// </summary>
        private void SelectBrush(PaintedBuildingBrushPreset brushPreset)
        {
            currentBrushPreset = brushPreset;
            ApplyBrushPreset(brushPreset);
            Repaint();
        }

        /// <summary>
        /// 应用笔刷预设
        /// </summary>
        private void ApplyBrushPreset(PaintedBuildingBrushPreset brushPreset)
        {
            if (brushPreset == null)
                return;

            currentCellType = brushPreset.cellType;
            cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
            cutoutEndHeightGridCount = Mathf.Clamp(cutoutEndHeightGridCount, cutoutStartHeightGridCount + 1, wallHeightGridCount);
        }

        /// <summary>
        /// 同步蓝图全局设置
        /// </summary>
        private void SyncGlobalSettingsFromPlan()
        {
            if (paintedPlan == null)
                return;

            wallHeightGridCount = paintedPlan.GlobalWallHeightGridCount;
            cutoutStartHeightGridCount = Mathf.Clamp(cutoutStartHeightGridCount, 0, wallHeightGridCount - 1);
            cutoutEndHeightGridCount = Mathf.Clamp(cutoutEndHeightGridCount, cutoutStartHeightGridCount + 1, wallHeightGridCount);
        }

        /// <summary>
        /// 同步生成器引用
        /// </summary>
        private void SyncGeneratorReferences()
        {
            if (generator == null)
                return;

            generator.paintedPlan = paintedPlan;
            generator.convention = convention;
            generator.brushPresetList = brushPresetList;
            EditorUtility.SetDirty(generator);
        }

        /// <summary>
        /// 确保默认资产
        /// </summary>
        private void EnsureDefaultAssets()
        {
            EnsureFolder("Assets/_Scripts/Mm_ProceduralBuilding", "So");
            EnsureFolder("Assets/_Scripts/Mm_ProceduralBuilding/So", "BrushPresets");

            if (paintedPlan == null)
                paintedPlan = LoadOrCreateAsset<PaintedBuildingPlan>(PlanAssetPath);

            if (convention == null)
                convention = LoadOrCreateAsset<BuildingGridConvention>(ConventionAssetPath);

            EnsureAllBrushPresets();
            LoadBrushPresetList();

            if (currentBrushPreset == null)
            {
                currentBrushPreset = FindBrushPreset(EPaintedBuildingCellType.Wall);
                if (currentBrushPreset != null)
                    ApplyBrushPreset(currentBrushPreset);
            }
        }

        /// <summary>
        /// 确保所有笔刷预设
        /// </summary>
        private void EnsureAllBrushPresets()
        {
            foreach (EPaintedBuildingCellType cellType in Enum.GetValues(typeof(EPaintedBuildingCellType)))
            {
                string assetPath = $"{BrushPresetFolderPath}/{cellType}.asset";
                var brushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(assetPath);
                if (brushPreset != null)
                {
                    UpdateBrushPresetDisplayName(brushPreset);
                    continue;
                }

                brushPreset = CreateInstance<PaintedBuildingBrushPreset>();
                brushPreset.name = GetCellTypeDisplayName(cellType);
                brushPreset.cellType = cellType;
                brushPreset.previewColor = BuildingPainterColorUtility.GetCellColor(cellType);
                brushPreset.defaultHeightGridCount = GetDefaultHeight(cellType);
                AssetDatabase.CreateAsset(brushPreset, assetPath);
                EditorUtility.SetDirty(brushPreset);
            }
        }

        /// <summary>
        /// 更新笔刷预设显示名
        /// </summary>
        private void UpdateBrushPresetDisplayName(PaintedBuildingBrushPreset brushPreset)
        {
            string displayName = GetCellTypeDisplayName(brushPreset.cellType);
            if (brushPreset.name == displayName)
                return;

            brushPreset.name = displayName;
            EditorUtility.SetDirty(brushPreset);
        }

        /// <summary>
        /// 加载笔刷预设列表
        /// </summary>
        private void LoadBrushPresetList()
        {
            brushPresetList.Clear();
            string[] guidList = AssetDatabase.FindAssets("t:PaintedBuildingBrushPreset", new[] { BrushPresetFolderPath });
            foreach (string guid in guidList)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var brushPreset = AssetDatabase.LoadAssetAtPath<PaintedBuildingBrushPreset>(assetPath);
                if (brushPreset != null)
                    brushPresetList.Add(brushPreset);
            }

            brushPresetList.Sort((a, b) => a.cellType.CompareTo(b.cellType));
        }

        /// <summary>
        /// 加载持久引用
        /// </summary>
        private void LoadPersistedReferences()
        {
            string planPath = EditorPrefs.GetString(PlanPrefsKey, PlanAssetPath);
            paintedPlan = AssetDatabase.LoadAssetAtPath<PaintedBuildingPlan>(planPath) ?? paintedPlan;
            generator = LoadPersistedGenerator() ?? UnityEngine.Object.FindObjectOfType<PaintedBuildingGenerator>();
        }

        /// <summary>
        /// 持久化引用
        /// </summary>
        private void PersistReferences()
        {
            if (paintedPlan != null)
                EditorPrefs.SetString(PlanPrefsKey, AssetDatabase.GetAssetPath(paintedPlan));

            if (generator != null)
            {
                GlobalObjectId globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(generator);
                EditorPrefs.SetString(GeneratorPrefsKey, globalObjectId.ToString());
            }
        }

        /// <summary>
        /// 加载持久生成器
        /// </summary>
        private PaintedBuildingGenerator LoadPersistedGenerator()
        {
            string generatorId = EditorPrefs.GetString(GeneratorPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(generatorId))
                return null;

            if (!GlobalObjectId.TryParse(generatorId, out GlobalObjectId globalObjectId))
                return null;

            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId) as PaintedBuildingGenerator;
        }

        /// <summary>
        /// 获取或创建生成器
        /// </summary>
        private PaintedBuildingGenerator GetOrCreateGenerator()
        {
            if (generator != null)
                return generator;

            generator = UnityEngine.Object.FindObjectOfType<PaintedBuildingGenerator>();
            if (generator != null)
                return generator;

            var obj = new GameObject("PaintedBuildingGenerator");
            Undo.RegisterCreatedObjectUndo(obj, "创建绘制建筑生成器");
            generator = obj.AddComponent<PaintedBuildingGenerator>();
            return generator;
        }

        /// <summary>
        /// 查找笔刷预设
        /// </summary>
        private PaintedBuildingBrushPreset FindBrushPreset(EPaintedBuildingCellType cellType)
        {
            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset != null && brushPreset.cellType == cellType)
                    return brushPreset;
            }

            return null;
        }

        /// <summary>
        /// 获取格子颜色
        /// </summary>
        private Color GetCellColor(PaintedBuildingCellData cellData)
        {
            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset == null || brushPreset.cellType != cellData.cellType)
                    continue;

                return brushPreset.previewColor;
            }

            return BuildingPainterColorUtility.GetCellColor(cellData.cellType);
        }

        /// <summary>
        /// 获取当前笔刷颜色
        /// </summary>
        private Color GetCurrentBrushColor()
        {
            if (currentBrushPreset != null)
                return currentBrushPreset.previewColor;

            return BuildingPainterColorUtility.GetCellColor(currentCellType);
        }

        /// <summary>
        /// 获取默认高度
        /// </summary>
        private int GetDefaultHeight(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return 1;
                case EPaintedBuildingCellType.Room:
                    return 3;
                default:
                    return 3;
            }
        }

        /// <summary>
        /// 获取笔刷显示名
        /// </summary>
        private string GetBrushLabel(PaintedBuildingBrushPreset brushPreset)
        {
            if (brushPreset.cellType == EPaintedBuildingCellType.Erase)
                return "擦除";

            if (brushPreset.cellType == EPaintedBuildingCellType.Room)
                return "房间";

            return GetCellTypeDisplayName(brushPreset.cellType);
        }

        /// <summary>
        /// 获取格子类型显示名
        /// </summary>
        private string GetCellTypeDisplayName(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return "地面";
                case EPaintedBuildingCellType.Wall:
                    return "墙体";
                case EPaintedBuildingCellType.Cutout:
                    return "挖空";
                case EPaintedBuildingCellType.Erase:
                    return "擦除";
                case EPaintedBuildingCellType.Room:
                    return "房间";
                default:
                    return "无";
            }
        }

        /// <summary>
        /// 获取网格中心
        /// </summary>
        private Vector2 GetGridCenter(Rect gridRect)
        {
            return new Vector2(gridRect.width * 0.5f, gridRect.height * 0.5f) + gridPanOffset;
        }

        /// <summary>
        /// 窗口坐标转格子
        /// </summary>
        private Vector2Int WindowToGrid(Vector2 localPos, Rect gridRect)
        {
            Vector2 center = GetGridCenter(gridRect);
            Vector2 offset = localPos - center;
            int x = Mathf.FloorToInt(offset.x / cellPixelSize);
            int z = Mathf.FloorToInt(-offset.y / cellPixelSize);
            return new Vector2Int(x, z);
        }

        /// <summary>
        /// 格子转窗口矩形
        /// </summary>
        private Rect GridToWindowCellRect(Vector2Int gridPos, Rect gridRect)
        {
            Vector2 center = GetGridCenter(gridRect);
            float x = center.x + gridPos.x * cellPixelSize;
            float y = center.y - (gridPos.y + 1) * cellPixelSize;
            return new Rect(x + 1f, y + 1f, cellPixelSize - 2f, cellPixelSize - 2f);
        }

        /// <summary>
        /// 获取框选边界
        /// </summary>
        private void GetGridRectBounds(
            Vector2Int startGridPos,
            Vector2Int endGridPos,
            out int minX,
            out int maxX,
            out int minZ,
            out int maxZ)
        {
            minX = Mathf.Min(startGridPos.x, endGridPos.x);
            maxX = Mathf.Max(startGridPos.x, endGridPos.x);
            minZ = Mathf.Min(startGridPos.y, endGridPos.y);
            maxZ = Mathf.Max(startGridPos.y, endGridPos.y);
        }

        /// <summary>
        /// 加载或创建资产
        /// </summary>
        private T LoadOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// 确保文件夹
        /// </summary>
        private void EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = $"{parentPath}/{folderName}";
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
#endif

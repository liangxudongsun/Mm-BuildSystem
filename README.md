# MmBuildSystem

Unity 体素 / 方块建造工程（类似我的世界、生存战争的网格放置玩法）。

## 文档

完整说明见：[Assets/_Scripts/Mm_Builder/README.md](Assets/_Scripts/Mm_Builder/README.md)

## 模块概览

| 模块 | 说明 |
|------|------|
| **建造系统** | 放置 / 破坏 / 旋转、预览、虚拟网格分区、存档、`IBuilderCustom` 扩展 |
| **方块系统** | `CubeBehaviour` 生命周期（放置、移除、更新、交互） |

## 快速入口

- 示例场景：`Assets/_Scripts/Mm_Builder/Scene/MmScene.unity`
- 编辑器：**Tools → MmBuilderSsytem → BuildEditorWindow**
- 运行时入口：`BuilderSystem.Instance`（`SetActiveCubeData` / `UpdateBuilderSystem`）

## 默认操作

| 操作 | 输入 |
|------|------|
| 放置 | 鼠标左键 |
| 破坏 | 鼠标右键 |
| 旋转 | R |

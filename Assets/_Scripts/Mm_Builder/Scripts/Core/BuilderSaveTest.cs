using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 存档功能测试：挂到场景任意物体上，Play 后点 Inspector 按钮测试 Save / Load
    /// </summary>
    public class BuilderSaveTest : MonoBehaviour
    {
        [InfoBox("需在 Play 模式下使用。\n1. 先在 BuilderSystemSetting 点「扫描并注册所有方块数据」\n2. 进 Play 摆几个方块\n3. 点保存 → 停止 Play → 再 Play → 点加载")]
        [SerializeField]
        private BuilderSystem builderSystem;

        [ShowInInspector, ReadOnly, LabelText("存档文件路径")]
        private string SaveFilePathDisplay
        {
            get
            {
                if (!Application.isPlaying || builderSystem?.builderSetting == null)
                    return "(Play 后显示)";
                var folder = string.IsNullOrWhiteSpace(builderSystem.builderSetting.saveFolderName)
                    ? "BuilderSystemData"
                    : builderSystem.builderSetting.saveFolderName;
                return Path.Combine(Application.persistentDataPath, folder, "build.json");
            }
        }

        private BuilderSystem System => builderSystem != null ? builderSystem : BuilderSystem.Instance;

        [Button("保存", ButtonSizes.Large), GUIColor(0.6f, 1f, 0.6f)]
        private void TestSave()
        {
            if (!EnsurePlaying()) return;
            System.SaveBuildData();
        }

        [Button("加载", ButtonSizes.Large), GUIColor(0.6f, 0.8f, 1f)]
        private void TestLoad()
        {
            if (!EnsurePlaying()) return;
            System.LoadBuildData();
        }

        [Button("注册方块反查表", ButtonSizes.Medium)]
        private void TestRegister()
        {
            if (!EnsurePlaying()) return;
            System.RegisterAllCubeData();
            Debug.Log("[Test] 已从 BuilderSystemSetting.allCubeDataList 注册到运行时反查表");
        }

        private bool EnsurePlaying()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Test] 请先进入 Play 模式");
                return false;
            }

            if (System == null)
            {
                Debug.LogError("[Test] 找不到 BuilderSystem，请拖到字段或确保场景里有 BuilderSystem");
                return false;
            }

            return true;
        }
    }
}

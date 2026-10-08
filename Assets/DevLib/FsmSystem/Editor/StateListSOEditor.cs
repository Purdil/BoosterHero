using System.IO;
using System.Linq;
using DevLib.FsmSystem.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevLib.FsmSystem.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset;
        
        private Button _folderBtn;
        private Button _generateBtn;
        private Label _folderPathLabel;
        
        private string _folderPath;
        private StateListSO _targetData;
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            visualTreeAsset.CloneTree(root);

            _folderBtn = root.Q<Button>("FolderBtn");
            _generateBtn = root.Q<Button>("GenerateBtn");
            _folderPathLabel = root.Q<Label>("SelectedFolderLabel");
            _folderPathLabel.text = "No folder selected";

            _folderBtn.clicked += HandleFolderSelectBtn;
            _generateBtn.clicked += HandleGenerateBtn;
            
            _targetData = target as StateListSO;
            if (_targetData != null && !string.IsNullOrEmpty(_targetData.generatePath))
            {
                _folderPath = _targetData.generatePath;
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_targetData.generatePath);
            }
            
            return root;
        }

        private void HandleFolderSelectBtn()
        {
            _folderPath = EditorUtility.OpenFolderPanel("폴더 선택", _folderPath, "");
            
            if(!string.IsNullOrEmpty(_folderPath))
            {
                _targetData.generatePath = _folderPath;
                _folderPathLabel.text = FileUtil.GetProjectRelativePath(_folderPath);
                EditorUtility.SetDirty(_targetData);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void HandleGenerateBtn()
        {
            if (string.IsNullOrEmpty(_folderPath) || !Directory.Exists(_folderPath))
            {
                EditorUtility.DisplayDialog("Error", "경로 설정이 올바르지 않습니다", "OK");
                return;
            }

            int index = 0;
            string enumString = string.Join(", ", _targetData.states.Select(so =>
            {
                so.assetIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));

            string nameSpace = FileUtil.GetProjectRelativePath(_folderPath).Substring("Assets/".Length);
            if (nameSpace.StartsWith("Scripts/"))
            {
                nameSpace = nameSpace.Substring("Scripts/".Length);
            }
            nameSpace = nameSpace.Replace("/", ".");
            
            string code = string.Format(CodeFormat.EnumFormat, nameSpace, _targetData.enumName, enumString);
            
            File.WriteAllText($"{_folderPath}/{_targetData.enumName}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
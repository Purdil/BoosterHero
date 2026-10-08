using System.Linq;
using DevLib.FsmSystem.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevLib.FsmSystem.Editor
{
    [CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset;
        private StateSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            _targetData = target as StateSO;
            VisualElement root = new VisualElement();
            visualTreeAsset.CloneTree(root);

            FillDropDownField(root);
            
            return root;
        }

        private void FillDropDownField(VisualElement root)
        {
            DropdownField dropdownField = root.Q<DropdownField>("ClassNameDropdown");
            
            var choices = TypeCache.GetTypesDerivedFrom<AbstractState>()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => $"{type.FullName}, {type.Assembly.GetName().Name}");

            dropdownField.choices.AddRange(choices);

            if (_targetData != null && 
                !string.IsNullOrEmpty(_targetData.className)
                && dropdownField.choices.Contains(_targetData.className))
            {
                dropdownField.value = _targetData.className;
            }
            else if(_targetData != null && dropdownField.choices.Count > 0)
            {
                _targetData.className = dropdownField.choices.First();
                EditorUtility.SetDirty(_targetData);
            }
            
            AssetDatabase.SaveAssetIfDirty(_targetData);
        }
    }
}
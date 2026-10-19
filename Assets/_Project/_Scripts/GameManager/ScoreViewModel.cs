using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project._Scripts.GameManager
{
    [CreateAssetMenu(fileName = "ScoreViewModel", menuName = "ViewModel/ScoreViewModel", order = 0)]
    public class ScoreViewModel : ScriptableObject, INotifyBindablePropertyChanged
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [CreateProperty] public string MaxScore = null;
        [CreateProperty] public string CurrentScore =  null;

        public void SetMaxScore(int value)
        {
            MaxScore = $"{value}m";
            Notify(nameof(MaxScore));
        }

        public void SetCurrentScore(int value)
        {
            CurrentScore = $"{value}m";
            
            Notify(nameof(CurrentScore));
        }

        private void Notify(string propName)
            => propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propName));
    }
}
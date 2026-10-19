using System;
using _Project._Scripts.Core;
using _Project._Scripts.Player;
using UnityEngine;

namespace _Project._Scripts.GameManager
{
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] private ScoreViewModel viewModel;

        //추후 Firebase 저장 예정
        private float _maxScore;
        
        private float _currentScore;
        private long _beforeScore;
        
        [SerializeField] private Agent target;
        private IMovement _targetMovement;
        private Vector3 _startPosition;

        private void Awake()
        {
            if(target == null)
                CLog.LogWarning("Player를 넣어주세요.");
            viewModel.SetCurrentScore(0);
        }

        private void Start()
        {
            _startPosition = target.transform.position;
            _targetMovement = target.GetModule<IMovement>();
        }

        private void Update()
        {
            CalculateScore();
        }


        private void CalculateScore()
        {
            _currentScore = Vector3.Distance(_startPosition, target.transform.position);
            if (Vector3.Dot(_startPosition - target.transform.position, Vector3.up) > 0)
                _currentScore = 0;    
            
            if(_beforeScore != (int)_currentScore)
                viewModel.SetCurrentScore((int)_currentScore);
            if (_currentScore > _maxScore)
            {
                _maxScore = _currentScore;
                viewModel.SetMaxScore((int)_maxScore);
            }
            _beforeScore = (int)_currentScore;
        }
    }
}
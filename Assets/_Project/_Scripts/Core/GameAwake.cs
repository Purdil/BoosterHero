using System;
using UnityEngine;

namespace _Project._Scripts.Core
{
    public class GameAwake : MonoBehaviour
    {
        private void Awake()
        {
            SRDebug.Init();
        }
    }
}
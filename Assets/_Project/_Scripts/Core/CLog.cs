using UnityEngine;

namespace _Project._Scripts.Core
{
    public static class CLog
    {
        public static void Log(object message)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log(message);
#endif
        }

        public static void LogError(object message)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError(message);
#endif
        }

        public static void LogError(object message, UnityEngine.Object context)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError(message, context);
#endif
        }

        public static void LogWarning(object message)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }

        public static void LogException(System.Exception exception)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogException(exception);
#endif
        }
    }
}
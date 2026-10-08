using UnityEngine;

namespace DevLib.CoreSystem
{
    public abstract class AbstractFeedback : MonoBehaviour
    {
        public abstract void PlayFeedback();
        public virtual void StopFeedback() { }
    }
}
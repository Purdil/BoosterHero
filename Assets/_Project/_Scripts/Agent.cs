using System.Linq;
using DevLib.AnimatorSystem;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts
{
    public abstract class Agent : ModuleOwner
    {
        public IAnimatorRenderer Renderer { get; private set; }

        private bool _modulesInitialized;

        protected override void InitializeModules()
        {
            IAnimatorRenderer[] renderers = _moduleDict.Values
                .OfType<IAnimatorRenderer>()
                .ToArray();

            if (renderers.Length != 1)
            {
                Debug.LogError(
                    $"{name} requires exactly one {nameof(IAnimatorRenderer)} module, "
                    + $"but found {renderers.Length}.",
                    this);
                enabled = false;
                return;
            }

            Renderer = renderers[0];
            base.InitializeModules();
            _modulesInitialized = true;
        }

        protected override void AfterInitializeModules()
        {
            if (_modulesInitialized)
                base.AfterInitializeModules();
        }
    }
}

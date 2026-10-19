using System;
using DevLib.ModuleSystem;
using UnityEngine;

namespace _Project._Scripts.Player
{
    [DisallowMultipleComponent]
    public class PlayerFuelModule : Module, IFuel
    {
        private const float FuelEpsilon = 0.001f;

        [SerializeField, Min(FuelEpsilon)] private float maxFuel = 95f;
        [SerializeField, Min(0f)] private float consumptionPerSecond = 60f;

        public event Action<float, float> FuelChanged;

        public float Current { get; private set; }
        public float Max => maxFuel;
        public float Normalized => maxFuel > FuelEpsilon ? Current / maxFuel : 0f;
        public float ConsumptionPerSecond => consumptionPerSecond;
        public bool HasFuel => Current > FuelEpsilon;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Refill();
        }

        public bool TryConsume(float amount)
        {
            if (amount <= 0f)
                return HasFuel;

            if (!HasFuel)
                return false;

            SetCurrent(Current - amount);
            return true;
        }

        public void Refill()
        {
            SetCurrent(maxFuel);
        }

        private void SetCurrent(float value)
        {
            float nextValue = Mathf.Clamp(value, 0f, maxFuel);
            if (Mathf.Approximately(Current, nextValue))
                return;

            Current = nextValue;
            FuelChanged?.Invoke(Current, maxFuel);
        }

        private void OnValidate()
        {
            maxFuel = Mathf.Max(FuelEpsilon, maxFuel);
            consumptionPerSecond = Mathf.Max(0f, consumptionPerSecond);
            Current = Mathf.Clamp(Current, 0f, maxFuel);
        }
    }
}

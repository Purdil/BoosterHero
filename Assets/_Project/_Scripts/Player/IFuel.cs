using System;

namespace _Project._Scripts.Player
{
    public interface IFuel
    {
        event Action<float, float> FuelChanged;

        float Current { get; }
        float Max { get; }
        float Normalized { get; }
        float ConsumptionPerSecond { get; }
        bool HasFuel { get; }

        bool TryConsume(float amount);
        void Refill();
    }
}

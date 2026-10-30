#if UNITY_EDITOR
using System;

namespace Assets.Scripts.Enums
{
    [Flags]
    public enum DisableFlags
    {
        Used = 1 << 0,
        Decontamination = 1 << 1,
        WarheadDetonated = 1 << 2,
        NTFWaveSpawned = 1 << 3,
        ChaosWaveSpawned = 1 << 4,
        DeadmanSequenceActivated = 1 << 5,
        AnySpawned = NTFWaveSpawned | ChaosWaveSpawned,
    }
}
#endif
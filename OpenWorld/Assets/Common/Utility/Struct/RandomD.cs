using UnityEngine;

[System.Serializable]
public struct RandomD
{
    private ulong state;
    private ulong inc;      // 스트림 식별자 (홀수여야 함)

    // 시드 확산 — SplitMix64
    private static ulong Mix(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    public RandomD(ulong seed, ulong stream = 1UL)
    {
        state = 0UL;
        inc = (stream << 1) | 1UL;      // 반드시 홀수
        NextUInt();
        state += Mix(seed);
        NextUInt();
    }

    // PCG32
    public uint NextUInt()
    {
        ulong old = state;
        state = old * 6364136223846793005UL + inc;
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    public double NextDouble()
    {
        // 상위 53비트를 뽑아 [0,1) 로. double 가수부와 정확히 일치
        ulong bits = ((ulong)NextUInt() << 32) | NextUInt();
        return (bits >> 11) * (1.0 / 9007199254740992.0);   // 2^53
    }
    /// <summary>단정밀도 해상도(2^24)면 충분한 경우. 호출 1회.</summary>
    public double NextDoubleFast()
    {
        return (NextUInt() >> 8) * (1.0 / 16777216.0);   // 2^24
    }
    /// <summary>
    /// 랜덤 범위
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public double Range(double min, double max)
    {
        double r = min + (max - min) * NextDouble();
        return r < max ? r :min;   // 또는 그냥 min 반환
    }
    public uint NextUInt(uint bound)   // [0, bound)
    {
        ulong m = (ulong)NextUInt() * bound;
        uint low = (uint)m;
        if (low < bound)
        {
            uint threshold = (uint)(-(int)bound) % bound;   // 2^32 % bound
            while (low < threshold)
            {
                m = (ulong)NextUInt() * bound;
                low = (uint)m;
            }
        }
        return (uint)(m >> 32);
    }
}
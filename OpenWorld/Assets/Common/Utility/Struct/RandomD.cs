using UnityEngine;

[System.Serializable]
public struct RandomD
{
    private ulong state;
    private ulong inc;      // 스트림 식별자 (홀수여야 함)

    //64비트 난수
    public ulong NextULong() => ((ulong)NextUInt() << 32) | NextUInt();
    public bool NextBool() => (NextUInt() >> 31) != 0;   // 최상위 비트

    //상태 저장/복원, 체크포인트에서 재시작해도 같은 수열
    public (ulong state, ulong inc) GetState() => (state, inc);
    public void SetState(ulong s, ulong i) { state = s; inc = i | 1UL; }
    //시드 없는 생성자
    public static RandomD CreateFromTime()
    => new RandomD((ulong)System.DateTime.Now.Ticks);

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
        if (inc == 0UL) InitDefault();

        ulong old = state;
        state = old * 6364136223846793005UL + inc;
        uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    public double NextDouble()
    {
        ulong hi = NextUInt();
        ulong lo = NextUInt();
        return (((hi << 32) | lo) >> 11) * (1.0 / 9007199254740992.0);
    }
    /// <summary>
    /// 랜덤 값 초기화
    /// </summary>
    public void InitDefault()
    {
        RandomD rng = default;
        rng.NextUInt();
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
        if (max <= min) return min;
        double r;
        do { r = min + (max - min) * NextDouble(); } while (r >= max);
        return r;
    }
    /// <summary>
    /// 랜덤 범위
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public int Range(int min, int max)   // [min, max)
    {
        if (max <= min) return min;
        return min + (int)NextUInt((uint)((long)max - min));
    }
    public uint NextUInt(uint bound)   // [0, bound)
    {
        if (bound == 0) return 0;   // 또는 예외

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
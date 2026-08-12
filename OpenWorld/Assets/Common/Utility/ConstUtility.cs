/// <summary>
/// 상수 모음
/// </summary>
public static class ConstUtility
{
    //천문 거리
    public const double AU = 150000000000;
    //중력 상수
    public const double G = 6.67387;
    //중력 가속도
    public const double gravity = 9.81;

    //원주율
    public const double PI = 3.14159265358979323846;
    public const double TWO_OVER_PI = 0.63661977236758134308;
    public const double PIO2_HI = 1.57079632673412561417e+00;
    public const double PIO2_MID = 6.07710050650619224932e-11;
    public const double PIO2_LO = 2.02226624879595063154e-21;
    public const double PI_2 = 1.57079632679489661923;   // π/2
    public const double PI_3 = 1.04719755119659774615;   // π/3
    public const double PI_4 = 0.78539816339744830962;   // π/4
    public const double PI_6 = 0.52359877559829887308;   // π/6
    public const double TWO_PI = 6.28318530717958647693;   // 2π

    //삼각함수 변환
    public const double Rad2Deg = 57.29578f;
    public const double Deg2Rad = 0.0174533f;

    //거듭제곱 상수
    public const double Epcilon12 = 1e-12;
    public const double Epcilon16 = 1e-16;
    public const double PowM11 = 1e-11;
    public const double e = 2.71828183;

    //로그 상수
    public const double ln2 = 0.69314718055994530942;
    public const double root2 = 1.4142135623730951;
    public const double INV_LN2 = 1.44269504088896340736;   // 1/ln2

    // ln2 = LN2_HI + LN2_LO (Cody-Waite)
    public const double LN2_HI = 6.93147180369123816490e-01;
    public const double LN2_LO = 1.90821492927058770002e-10;

    //탄전트 상수
    public const double TanPi12 = 0.2679491924311227;
    //루트 상수
    public const double InvSqrt3 = 0.57735026918962576451;

    // 홀수 팩토리얼 역수 (sin)
    public const double INV_FACT3 = 1.0 / 6.0;
    public const double INV_FACT5 = 1.0 / 120.0;
    public const double INV_FACT7 = 1.0 / 5040.0;
    public const double INV_FACT9 = 1.0 / 362880.0;
    public const double INV_FACT11 = 1.0 / 39916800.0;
    public const double INV_FACT13 = 1.0 / 6227020800.0;
    public const double INV_FACT15 = 1.0 / 1307674368000.0;

    // 짝수 팩토리얼 역수 (cos)
    public const double INV_FACT2 = 1.0 / 2.0;
    public const double INV_FACT4 = 1.0 / 24.0;
    public const double INV_FACT6 = 1.0 / 720.0;
    public const double INV_FACT8 = 1.0 / 40320.0;
    public const double INV_FACT10 = 1.0 / 3628800.0;
    public const double INV_FACT12 = 1.0 / 479001600.0;
    public const double INV_FACT14 = 1.0 / 87178291200.0;
    public const double INV_FACT16 = 1.0 / 20922789888000.0;
}

using System.IO.Hashing;
using Unity.Mathematics;
using UnityEngine;

public static class MathUtility
{
    #region 기초 함수
    /// <summary>
    /// 더 작은 값
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static double Min(double x, double y)
    {
        return (x>y) ? y : x;
    }
    /// <summary>
    /// 더 큰 값
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static double Max(double x, double y)
    {
        return (x > y) ? x : y;
    }
   
    /// <summary>
    /// 절댓값 구하기
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Abs(double x)
    {
        return (x < 0) ? -x : x;
    }

    #endregion

    #region 보간
    /// <summary>
    /// Clamp, 사이에 있는 값으로 보정
    /// </summary>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static double ClampValue(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }
        return value;
    }
    /// <summary>
    /// 기본 선형 보간 함수
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static double Lerp(double a, double b, double t)
    {
        t = ClampValue(t, 0, 1);
        return a + (b - a) * t;
    }
    /// <summary>
    /// 선형 외삽 포함: t 제한 없음. t>1 또는 t<0이면 구간 밖으로 연장.
    /// </summary>
    public static double LerpUnclamped(double a, double b, double t)
    {
        return a * (1 - t) + b * t;   // Clamp 없음
    }
    /// <summary>
    /// 역보간: value가 a~b 구간에서 차지하는 비율 t를 반환. Lerp의 역함수.
    /// </summary>
    public static double InverseLerp(double a, double b, double value)
    {
        if (a == b) return 0;               // 0으로 나누기 방지
        return ClampValue((value - a) / (b - a),0,1);
    }
    /// <summary>
    /// 보간 벡터버전
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static Vector3D Lerp(Vector3D a,Vector3D b, double t)
    {
        t = ClampValue(t,0,1);
        return new Vector3D(
            Lerp(a.x, b.x, t),
            Lerp(a.y, b.y, t),
            Lerp(a.z, b.z, t)
        );
    }
    /// <summary>
    /// 각도 보간
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static double LerpAngle(double a, double b, double t)
    {
        double delta = (b - a)% 360;      // 차이를 0~360으로 감기
        if (delta > 180) delta -= 360;          // 180 넘으면 반대 방향이 더 짧음
        if (delta < -180) delta += 360;
        return a + delta * ClampValue(t,0,1);
    }
    /// <summary>
    /// 현재값을 목표값으로 임계 감쇠 스프링을 따라 부드럽게 이동시킨다.
    /// currentVelocity는 ref로 상태를 유지한다(호출 간 속도 보존).
    /// </summary>
    /// <param name="current">현재값</param>
    /// <param name="target">목표값</param>
    /// <param name="currentVelocity">현재 속도(ref, 매 호출 갱신됨)</param>
    /// <param name="smoothTime">목표 도달 대략 시간(작을수록 빠름)</param>
    /// <param name="deltaTime">한 스텝 시간(보통 Time.fixedDeltaTime)</param>
    /// <param name="maxSpeed">최대 속도 제한(선택)</param>
    public static double SmoothDamp(
        double current, double target, ref double currentVelocity,
        double smoothTime, double deltaTime,
        double maxSpeed = double.PositiveInfinity)
    {
        // smoothTime이 0이 되지 않게 최소값 보장
        smoothTime = Max(0.0001, smoothTime);
        double omega = 2.0 / smoothTime;              // 자연 진동수 ω = 2/T

        double x = omega * deltaTime;
        // e^(-x) 유리함수 근사 (실시간용). 정밀도 원하면 Exp(-x)로 교체 가능
        double expApprox = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);

        double change = current - target;             // 목표까지 거리 d₀
        double originalTarget = target;

        // 최대 속도 제한 (한 스텝 이동량 클램프)
        double maxChange = maxSpeed * smoothTime;
        change = ClampValue(change, -maxChange, maxChange);
        target = current - change;

        // 임계 감쇠 해석해
        double temp = (currentVelocity + omega * change) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * expApprox;
        double output = target + (change + temp) * expApprox;

        // 오버슈트 방지: 목표를 지나쳤으면 목표에 고정
        if ((originalTarget - current > 0.0) == (output > originalTarget))
        {
            output = originalTarget;
            currentVelocity = (output - originalTarget) / deltaTime;
        }

        return output;
    }
    #endregion

    #region 올림, 내림, 반올림
    /// <summary>
    /// 반올림, 소수 첫번째 자리에서 반올림
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static long RoundToInt(double x)
    {
        // NaN, 무한대, long 범위를 벗어나는 매우 큰 수 처리
        if (double.IsNaN(x) || double.IsInfinity(x) || x >= long.MaxValue || x <= long.MinValue)
            return (long)x;

        double decimalValue = x-(long)x;//소수 값
        long intValue = (long)x;//정수 값

        //딱 떨어짐
        if (decimalValue == 0) return intValue;

        //올림
        if (decimalValue >= 0.5)
            return intValue+1;
        else if(decimalValue<=-0.5)
            return intValue-1;

        //버림
        return intValue;
    }
    /// <summary>
    /// 반올림, 소수 N째자리까지 반올림
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Round(double x,int digit)
    {
        // NaN, 무한대는 그대로 반환
        if (double.IsNaN(x) || double.IsInfinity(x))
            return x;

        double n10 = Pow(10, digit);
        double x10n = x * n10;

        // 스케일 후 long 범위를 벗어나면 안전하게 원본 반환
        if (x10n >= long.MaxValue || x10n <= long.MinValue)
            return x;

        double decimalValue = x10n - (long)x10n;//소수 값
        long intValue = (long)x10n;//정수 값

        //딱 떨어짐
        if (decimalValue == 0) return (double)intValue/n10;

        //올림(숫자 변동)
        if (decimalValue >= 0.5)
            intValue += 1;
        else if (decimalValue <= -0.5)
            intValue -= 1;

        return (double)intValue/n10;
    }

    /// <summary>
    /// 내림 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static long FloorToInt(double x)
    {
        // NaN, 무한대, long 범위를 벗어나는 매우 큰 수 처리
        if (double.IsNaN(x) || double.IsInfinity(x) || x >= long.MaxValue || x <= long.MinValue)
            return (long)x;

        long intX = (long)x;
        if (x < 0 && intX != x)//음수 보정
        {
            intX -= 1;
        }
        return intX;
    }

    /// <summary>
    /// 올림 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static long CeilToInt(double x)
    {
        // NaN, 무한대, long 범위를 벗어나는 매우 큰 수 처리
        if (double.IsNaN(x) || double.IsInfinity(x) || x >= long.MaxValue || x <= long.MinValue)
            return (long)x;

        long intX = (long)x;
        //원래부터 정수
        if (intX == x) return intX;

        //음수 보정
        if (x < 0 && intX != x)
        {
            return intX;
        }
        return intX + 1;
    }

    #endregion

    /// <summary>
    /// 2차 적분
    /// </summary>
    /// <param name="vec0"></param>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static Vector3D Integrate(Vector3D vec0, double dt)
    {
        return vec0 * dt;
    }

    #region 경우의 수
    /// <summary>
    /// 팩토리얼
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Fact(long x)
    {
        //음수 방어
        if (x < 0) return double.NaN;
        //지나치게 큰수
        if(x>= long.MaxValue) return double.NaN;

        //0!, 1!
        if (x < 2) return 1;

        double res = 1;
        for(int i = (int)x; i > 1; i--)
        {
            res *= i;
        }
        return res;
    }
    /// <summary>
    /// 순열
    /// </summary>
    /// <param name="n"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public static double NPR(long n, long r)
    {
        //음수 방어
        if (n < 0 || r<0) return double.NaN;
        //지나치게 큰수
        if (n >= long.MaxValue || r>=long.MaxValue) return double.NaN;
        //정의 위배
        if(n<r) return double.NaN;

        return Fact(n) / Fact(n - r);
    }
    /// <summary>
    /// 조합
    /// </summary>
    /// <param name="n"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public static double NCR(long n, long r)
    {
        //음수 방어
        if (n < 0 || r <= 0) return double.NaN;
        //지나치게 큰수
        if (n >= long.MaxValue || r >= long.MaxValue) return double.NaN;
        //정의 위배
        if (n < r) return double.NaN;

        return NPR(n,r) / Fact(r);
    }
    #endregion

    #region 지수/로그 함수 
    /// <summary>
    /// 거듭지수 계산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static double Pow(double a, long n)
    {
        if (n < 0) return 1 / Pow(a, -n);//음수 지수
        if (n == 0) return 1;
        if (n == 1) return a;

        double half = Pow(a, n / 2);
        if (n % 2 == 0) return half*half;
        else return half * half * a;
    }

    /// <summary>
    /// 거듭지수 계산
    /// 실수일때는 e^xlna로 계산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Pow(double a, double x)
    {
        if (x == 0) return 1;
        if (x == 1) return a;

        //로그 예외
        if (a <= 0) return double.NaN;

        //x를 정수, 소수 분리
        double decimalValue = x - (long)x;//소수 값
        long intValue = (long)x;//정수 값

        //정수 결과
        double axLong = Pow(a,intValue);

        //소수 결과
        //xlna
        double lna = Log(a);
        double xlna = decimalValue * lna;

        //e^xlna, 테일러 급수 활용
        return Exp(xlna)* axLong;
    }

    /// <summary>
    /// e^x 계산
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Exp(double x)
    {
        //범위 축소
        int k = 0;
        while ((x<0?-x:x)>1)
        {
            x *= 0.5;
            k++;
        }

        //테일러 급수, e^(t/2^k)
        double term = 1;
        double sum = 1;
        for(int i = 1; i <= 12; i++)
        {
            term *= (x/i);
            sum += term;
        }

        //다시 2^k만큼 곱함(정확히는 축소한 횟수만큼)
        for (int i = 0; i < k; i++) sum *= sum;
        return sum;
    }

    /// <summary>
    /// 제곱근
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sqrt(double x)
    {
        if (x < 0) return double.NaN;//허수
        if (x == 0) return 0;

        // 범위 축소: x = m · 4^k,  m ∈ [1, 4)  →  √x = √m · 2^k
        int k = 0;
        double m = x;
        while (m >= 4) { m *= 0.25; k++; }
        while (m < 1) { m *= 4; k--; }

        int count = 0;
        int maxIterations = 100;//무한루프 방지
        double rootX = m;
        double prev;
        do
        {
            prev = rootX;
            rootX = (rootX + (m / rootX)) * 0.5;

            count++;
            if (count > maxIterations) break;//최대 반복수

        } while (Abs(rootX - prev) > ConstUtility.Epcilon12*rootX);

        //축소한만큼 다시 곱함
        if (k >= 0) for (int i = 0; i < k; i++) rootX *= 2;
        else for (int i = 0; i < -k; i++) rootX *= 0.5;
        return rootX;
    }
    /// <summary>
    /// 세제곱근 계산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static double Cbrt(double x)
    {
        if (x == 0) return 0;
        if (double.IsNaN(x) || double.IsInfinity(x)) return x;//NaN
        if (x < 0) return -Cbrt(-x);//음수 정의역

        // 범위 축소: x = m · 8^k,  m ∈ [1, 8)  →  ∛x = ∛m · 2^k
        int k = 0;
        double m = x;
        while (m >= 8) { m *= 0.125; k++; }
        while (m < 1) { m *= 8; k--; }

        double y = 0.5 + 0.3 * m;
        const int MAX_ITER = 20;
        for (int i = 0; i < MAX_ITER; i++)
        {
            double prev = y;
            y = (2.0 * y + m / (y * y)) / 3.0;
            if (Abs(y - prev) <= ConstUtility.Epcilon12 * y) break;
        }

        //축소한만큼 다시 곱함
        if (k >= 0) for (int i = 0; i < k; i++) y *= 2;
        else for (int i = 0; i < -k; i++) y *= 0.5;
        return y;
    }

    /// <summary>
    /// 자연로그 계산
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Log(double x)
    {
        //범위 예외
        if (x <= 0) return double.NaN;

        //1
        if (x == 1) return 0;

        //지수,가수 분해
        double mantissa = x;
        double arqumenbt = 0;
        while (mantissa >= 2)
        {
            mantissa = mantissa / 2;
            arqumenbt += 1;
        }
        while (mantissa < 1.0) { mantissa *= 2.0; arqumenbt-=1; }
        // √2 조정: 가수를 [0.707, 1.414)로 더 좁힘
        double halfCorrection = 0;
        if (mantissa > ConstUtility.root2)   // √2 ≈ 1.41421356
        {
            mantissa /= ConstUtility.root2;
            halfCorrection = 0.5;            // ln(√2) = ln2/2 만큼 나중에 더함
        }

        //계산
        double x1 = mantissa - 1;
        double x12 = x1 * x1;
        double x14 = x12 * x12;
        double x18 = x14 * x14;

        double res1to4 = x1 - (x12 / 2) + (x12 * x1 / 3) - (x14/4);
        double res5to8 = (x14*x1/5) - (x14*x12 / 6) + (x14 * x12*x1 / 7) - (x18 / 8);
        double res9to12 = (x18 * x1 / 9) - (x18 * x12 / 10) + (x18 * x12 * x1 / 11) - (x18*x14 / 12);
        double res13to16 = (x18*x14 * x1 / 13) - (x18*x14 * x12 / 14) + (x18*x14 * x12 * x1 / 15) - (x18*x18 / 16);
        double talorResult = res1to4 + res5to8 + res9to12 + res13to16;

        return talorResult + (arqumenbt+halfCorrection)*ConstUtility.ln2;
    }

    /// <summary>
    /// 상용로그 계산
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Log10(double x)
    {
        if (x <= 0) return double.NaN;//범위 예외
        if (x == 1) return 0;//1

        return Log(x) / Log(10);
    }
    /// <summary>
    /// 밑이 k인 로그 계산
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double LogK(double x, double k)
    {
        if (x <= 0) return double.NaN;//범위 예외
        if(k==1) return double.NaN;//정의 위배
        if (x == 1) return 0;//1

        return Log(x) / Log(k);
    }

    #endregion

    #region 삼각함수
    /// <summary>
    /// 도를 라디안으로
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ToRadianAngle(double x)
    {
        //정의역 조절
        x = x % 360;

        return x * ConstUtility.PI/180;
    }

    /// <summary>
    /// 라디안을 육십분법으로
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ToDegreeAngle(double x)
    {
        //정의역 조절
        x = x % (2 * ConstUtility.PI);

        return x * 180/ConstUtility.PI;
    }
   

    /// <summary>
    /// Sin 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sin(double x)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) return double.NaN;

        // π/2 단위로 접기 → |r| ≤ π/4
        double k = RoundToInt(x * ConstUtility.TWO_OVER_PI);
        double r = x - k * ConstUtility.PIO2_HI;
        r -= k * ConstUtility.PIO2_MID;
        r -= k * ConstUtility.PIO2_LO;

        int q = (int)((long)k & 3L);
        if (q < 0) q += 4;

        switch (q)
        {
            case 0: return SinCore(r);
            case 1: return CosCore(r);
            case 2: return -SinCore(r);
            default: return -CosCore(r);
        }
    }
    /// <summary>
    /// Cos 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Cos(double x)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) return double.NaN;

        double k = RoundToInt(x * ConstUtility.TWO_OVER_PI);
        double r = x - k * ConstUtility.PIO2_HI;
        r -= k * ConstUtility.PIO2_MID;
        r -= k * ConstUtility.PIO2_LO;

        int q = (int)((long)k & 3L);
        if (q < 0) q += 4;

        switch (q)
        {
            case 0: return CosCore(r);
            case 1: return -SinCore(r);
            case 2: return -CosCore(r);
            default: return SinCore(r);
        }
    }
    /// <summary>
    /// sin 급수 본체. |r| ≤ π/4 전제.
    /// 호출 전에 반드시 range reduction을 거칠 것.
    /// </summary>
    public static double SinCore(double r)
    {
        double r2 = r * r;
        double r4 = r2 * r2;
        double r8 = r4 * r4;

        double t3 = (r2 * r) * ConstUtility.INV_FACT3;    // r^3  / 3!
        double t5 = (r4 * r) * ConstUtility.INV_FACT5;    // r^5  / 5!
        double t7 = (r4 * r2 * r) * ConstUtility.INV_FACT7;    // r^7  / 7!
        double t9 = (r8 * r) * ConstUtility.INV_FACT9;    // r^9  / 9!
        double t11 = (r8 * r2 * r) * ConstUtility.INV_FACT11;   // r^11 / 11!
        double t13 = (r8 * r4 * r) * ConstUtility.INV_FACT13;   // r^13 / 13!
        double t15 = (r8 * r4 * r2 * r) * ConstUtility.INV_FACT15;// r^15 / 15!

        return r - t3 + t5 - t7 + t9 - t11 + t13 - t15;
    }
    /// <summary>
    /// cos 급수 본체. |r| ≤ π/4 전제.
    /// </summary>
    public static double CosCore(double r)
    {
        double r2 = r * r;
        double r4 = r2 * r2;
        double r8 = r4 * r4;

        double t2 = r2 * ConstUtility.INV_FACT2;    // r^2  / 2!
        double t4 = r4 * ConstUtility.INV_FACT4;    // r^4  / 4!
        double t6 = (r4 * r2) * ConstUtility.INV_FACT6;    // r^6  / 6!
        double t8 = r8 * ConstUtility.INV_FACT8;    // r^8  / 8!
        double t10 = (r8 * r2) * ConstUtility.INV_FACT10;   // r^10 / 10!
        double t12 = (r8 * r4) * ConstUtility.INV_FACT12;   // r^12 / 12!
        double t14 = (r8 * r4 * r2) * ConstUtility.INV_FACT14;   // r^14 / 14!
        double t16 = (r8 * r8) * ConstUtility.INV_FACT16;   // r^16 / 16!

        return 1.0 - t2 + t4 - t6 + t8 - t10 + t12 - t14 + t16;
    }


    /// <summary>
    /// Tan 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Tan(double x)
    {
        return Sin(x) / Cos(x);
    }
    /// <summary>
    /// 1/Sin 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Cosec(double x)
    {
        return 1 / Sin(x);
    }
    /// <summary>
    /// 1/Cos 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sec(double x)
    {
        return 1 / Cos(x);
    }
    /// <summary>
    /// 1/Tan 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Cot(double x)
    {
        return 1/Tan(x);
    }
    /// <summary>
    /// Sin^-1 함수
    /// 반환은 호도법으로 
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ArkSin(double x)
    {
        //정의역 설정
        if (x == 1) return ConstUtility.PI/2;
        if (x == -1) return -ConstUtility.PI / 2;

        if (Abs(x) > 1) return double.NaN;

        //ArkTan 경유
        double newX = x / Sqrt(1-x*x);
        return ArkTan(newX);
    }
    /// <summary>
    /// Cos^-1 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ArkCos(double x)
    {
        //정의역 설정
        if (Abs(x) > 1) return double.NaN;

        return ConstUtility.PI/2-ArkSin(x);
    }
    /// <summary>
    /// Tan^-1 함수
    /// x=1 경계에서 2.5e-3 오차 있음
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ArkTan(double x)
    {
        //부호 분리 -> x>=0에서 계산
        double sign = 1;
        if (x < 0)
        {
            sign = -1;
            x = -x;
        }

        //범위 축소
        bool invert = false;
        if (x > 1)
        {
            x = 1 / x;
            invert = true;
        }

        //2차 축소
        bool shift = false;
        if (x > ConstUtility.TanPi12)
        {
            x = (x - ConstUtility.InvSqrt3) / (1 + x * ConstUtility.InvSqrt3);
            shift = true;
        }

        double arkTan = x;
        double term = x;
        for (long n = 1; n < 99; n++)
        {
            term *= (-1.0*x * x);
            double add = term / (2 * n + 1);
            arkTan += add;

            //조기 종료
            if (Abs(add) < ConstUtility.Epcilon12) break;
        }

        if (shift) arkTan += ConstUtility.PI / 6;
        if (invert) arkTan = ConstUtility.PI / 2 - arkTan;
        return sign*arkTan;
    }
    /// <summary>
    /// Tan^-1 함수, 인자 2개
    /// x축 양의 방향 기준 (x,y)의 각도, 범위 -π ~ π
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double ArkTan2(double y, double x)
    {
        //0나누기 방지
        if (x == 0)
        {
            if(y>0) return ConstUtility.PI/2;
            if (y < 0) return -ConstUtility.PI / 2;
            return 0;//둘다 0
        }

        double arkTan = ArkTan(y / x);//1,4사분면

        if (x > 0) return arkTan;//1,4
        if (y >= 0) return arkTan + ConstUtility.PI;//2
        return arkTan - ConstUtility.PI;//3
    }
    /// <summary>
    /// 쌍곡선 함수 -Sinh
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sinh(double x)
    {
        return (Exp(x) - Exp(-x)) / 2;
    }
    /// <summary>
    /// 쌍곡선 함수 -Cosh
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Cosh(double x)
    {
        return (Exp(x) + Exp(-x)) / 2;
    }
    /// <summary>
    /// 쌍곡선 함수 -Tanh
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Tanh(double x)
    {
        return Sinh(x) / Cosh(x);
    }
    #endregion

    #region FFT
    /// <summary>
    /// 짝수항 트위들 팩터 계산
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static Complex Even_TwittleFactor(double[] data, float k)
    {
        int N = data.Length;
        Complex X = new Complex(0,0);
        for (int m = 0; m < N/2; m++)
        {
            double angle = -2 * ConstUtility.PI*m*k / N;
            Complex angleComplex = new Complex(Cos(angle), Sin(angle));
            X += (angleComplex*data[2*m]);
        }
        return X;
    }
    
    public static Complex[] Cal_FFT(double[] data)
    {
        int N = data.Length;
        Complex[] result = new Complex[N];//시계열을 복소수화
        for(int i = 0; i < N; i++)
        {
            result[i] = new Complex(data[i], 0);
        }
        return FFT(result);
    }
    public static Complex[] FFT(Complex[] x)
    {
        int N = x.Length;

        //길이가 1
        if (x.Length == 1) return new Complex[] { x[0] };

        //짝, 홀 분할
        Complex[] even = new Complex[N / 2];
        Complex[] odd = new Complex[N / 2];
        for (int m = 0; m < N / 2; m++)
        {
            even[m] = x[2 * m];
            odd[m] = x[2 * m + 1];
        }

        // 재귀 호출
        Complex[] E = FFT(even);
        Complex[] O = FFT(odd);

        //버터플라이
        Complex[] result = new Complex[N];
        for (int k = 0; k < N / 2; k++)
        {
            double angle = -2 * ConstUtility.PI * k / N;
            Complex wk = new Complex(Cos(angle), Sin(angle));
            Complex t = wk * O[k];

            result[k] = E[k] +t;
            result[k + N / 2] = E[k]-t;
        }
        return result;
    }
    /// <summary>
    /// 반복 FFT
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Complex[] FFT_Iterative(double[] data)
    {
        int N = data.Length;
        Complex[] result = new Complex[N];

        //비트 반전 순서로 배치
        for(int idx = 0; idx < N; idx++)
        {
            result[BitReverse(idx, N)] = new Complex(data[idx],0);
        }

        //버터플라이
        for (int size = 2; size <= N; size *= 2)//2^n
        {
            for (int start = 0; start < N; start += size)//Size 간격
            {
                for (int k = 0; k < size / 2; k++)
                {
                    double angle = -2 * ConstUtility.PI * k / size;
                    Complex wk = new Complex(Cos(angle), Sin(angle));

                    int iEven = start + k;//앞 절반
                    int iOdd = start + k+size/2;//뒤 절반

                    //옛값 저장
                    Complex t = wk * result[iOdd];//홀수
                    Complex u = result[iEven];//짝수

                    //갱신
                    result[iEven] = u + t;
                    result[iOdd] = u - t;
                }
            }
        }
        return result;
    }
    /// <summary>
    /// 비트 반전
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="N"></param>
    /// <returns></returns>
    public static int BitReverse(int idx, int N)
    {
        int bits = 0;
        int temp = N;

        //비트 개수
        while (temp > 1)
        {
            bits++;
            temp >>= 1;
        }

        //비트 뒤집기
        int result = 0;
        for(int i = 0; i < bits; i++)
        {
            if ((idx & (1 << i)) != 0)
            {
                result |= 1<<(bits-i-1);
            }
        }
        return result;
    }
    #endregion
}
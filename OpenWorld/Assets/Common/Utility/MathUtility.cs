using Unity.Mathematics;
using UnityEngine;

public static class MathUtility
{
    /// <summary>
    /// 거듭지수 계산
    /// </summary>
    /// <param name="a"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    public static double Pow(double a, int n)
    {
        if(n==1) return a;

        if(n%2==0) return Pow(a,n/2)*Pow(a,n/2);
        else return Pow(a, n / 2) * Pow(a, n / 2)*a;
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

    /// <summary>
    /// 제곱근
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sqrt(double x)
    {
        if (x < 0) x = Abs(x);
        if (x == 0) return 0;

        double rootX = x;
        double prev;
        do
        {
            prev = rootX;
            rootX = (rootX + (x / rootX)) * 0.5;
        } while (Abs(rootX-prev)>ConstUtility.Epcilon12);
        return rootX;
    }

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

    /// <summary>
    /// 팩토리얼
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Fact(double x)
    {
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
    /// 내림 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Floor(double x)
    {
        long intX = (long)x;
        if(x<0 && intX != x)//음수 보정
        {
            intX -= 1;
        }
        return (double) intX;
    }

    /// <summary>
    /// 올림 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Ceil(double x)
    {
        long intX = (long)x;
        if (x < 0 && intX != x)//음수 보정
        {
            return (double)intX;
        }
        return (double)intX+1;
    }

    #region 삼각함수
    /// <summary>
    /// Sin 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sin(double x)
    {
        double twoPi = 2 * ConstUtility.PI;
        x = x - twoPi * Floor((x + ConstUtility.PI) / twoPi);

        double x2 = x * x;
        double x4 = x2 * x2;
        double x8 = x4 * x4;
        double second = (x * x * x)/Fact(3);
        double third = (x2 * x2 * x) / Fact(5);
        double fourth = (x4 * x2 * x) / Fact(7);
        double fifth = (x8 * x) / Fact(9);
        double sixth = (x8 * x2*x) / Fact(11);
        double seventh = (x8 * x4*x) / Fact(13);
        double eightth = (x8 * x4 * x2*x) / Fact(15);

        return x -second + third - fourth + fifth-sixth+seventh-eightth;
    }
    /// <summary>
    /// Cos 함수
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Cos(double x)
    {
        double twoPi = 2 * ConstUtility.PI;
        x = x - twoPi * Floor((x + ConstUtility.PI) / twoPi);

        double x2 = x * x;
        double x4 = x2 * x2;
        double x8 = x4 * x4;
        double second = x2 / Fact(2);
        double third = x4 / Fact(4);
        double fourth = (x2*x4) / Fact(6);
        double fifth = x8 / Fact(8);
        double sixth = (x8 * x2) / Fact(10);
        double seventh = (x8* x4) / Fact(12);
        double eightth = (x8 * x4 * x2) / Fact(14);
        double nineth = (x8 * x8) / Fact(16);

        return 1 - second + third - fourth + fifth-sixth+seventh-eightth+nineth;
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

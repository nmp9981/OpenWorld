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
}

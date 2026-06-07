public static class MathUtility
{
    /// <summary>
    /// °ÅµìÁö¼ö °è»ê
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
    /// Á¦°ö±Ù
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sqrt(double x)
    {
        return x * x;
    }
}

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
    /// Àý´ñ°ª ±¸ÇÏ±â
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Abs(double x)
    {
        return (x < 0) ? -x : x;
    }

    /// <summary>
    /// Á¦°ö±Ù
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static double Sqrt(double x)
    {
        if (x < 0) x = Abs(x);

        double rootX = x;
        for (int i = 0; i < 11; i++)
        {
            rootX = (rootX + (x / rootX)) *0.5;
        }
        return rootX;
    }
}

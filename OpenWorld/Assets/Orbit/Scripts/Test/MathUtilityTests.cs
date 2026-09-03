using NUnit.Framework;
using UnityEngine;

public class MathUtilityTests : MonoBehaviour
{
    public void Sin_Cos_MatchesSystemMath()
    {
        float maxErr = 0;
        for (int i = 0; i <= 200000; i++)
        {
            double x = -Mathf.PI + 2.0 * Mathf.PI * i / 200000.0;
            maxErr = Mathf.Max(maxErr, Mathf.Abs((float)MathUtility.Sin(x) - Mathf.Sin((float)x)));
            maxErr = Mathf.Max(maxErr, Mathf.Abs((float)MathUtility.Cos(x) - Mathf.Cos((float)x)));
        }
        TestContext.WriteLine($"max |¥Ä| = {maxErr:E3}");
        Assert.Less(maxErr, 5e-16);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlanerManager : MonoBehaviour
{
    static PlanerManager _instance;

    public static PlanerManager Instance { get { Init(); return _instance; } }

    static void Init()
    {
        if (_instance == null)
        {
            GameObject gm = GameObject.Find("PlanetManager");
            if (gm == null)
            {
                gm = new GameObject { name = "PlanetManager" };

                gm.AddComponent<PlanerManager>();
            }
            DontDestroyOnLoad(gm);
            _instance = gm.GetComponent<PlanerManager>();
        }
    }

    public List<PlanetInfo> _planetList = new List<PlanetInfo>();

    private void FixedUpdate()
    {
        //가속도 계산
        CalAccelEachPlanet();
        //속도, 위치 계산
        CalVelocity_Position_EachPlanet();
        //위치 반영
        //SetPlanetPosition();
    }

    /// <summary>
    /// 각 행성의 가속도 계산
    /// </summary>
    void CalAccelEachPlanet()
    {
        foreach (var planet in _planetList)
        {
            planet.accel = planet.TotalAccel();
            CalVelocity_Position_EachPlanet();
        }
    }

    /// <summary>
    /// 속도, 위치 계산
    /// </summary>
    void CalVelocity_Position_EachPlanet()
    {
        foreach (var planet in _planetList)
        {
            planet.velocity += MathUtility.Integrate(planet.accel, Time.fixedTimeAsDouble);
            planet.position += MathUtility.Integrate(planet.velocity, Time.fixedTimeAsDouble);
            planet.SetPosition(planet.position);
        }
    }

    /// <summary>
    /// 각 행성별 위치 설정
    /// </summary>
    void SetPlanetPosition()
    {
        foreach (var planet in _planetList)
        {
            planet.SetPosition(planet.position);
        }
    }
}

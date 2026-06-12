using System.Collections.Generic;
using UnityEngine;

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

    float dt;
    public List<PlanetInfo> _planetList = new List<PlanetInfo>();

    private void Awake()
    {
        dt = Time.fixedDeltaTime;
    }

    private void Start()
    {
        Cal_TotalMomentum();
    }

    private void FixedUpdate()
    {
        //속도, 위치 계산
        CalVelocity_Position_EachPlanet();
    }

    /// <summary>
    /// 전체 운동량 계산
    /// </summary>
    void Cal_TotalMomentum()
    {
        Vector3D totalMomentum = Vector3D.ZeroVector();
        PlanetInfo sun = new PlanetInfo();
        foreach (var planet in _planetList)
        {
            if (planet.isStar)
            {
                sun = planet;
                continue;
            }
            totalMomentum += PhysicsFormula.Cal_Momentum(planet.Mass, planet.velocity);
        }
        //반대방향으로
        sun.velocity = totalMomentum * (-1.0 / sun.Mass);
    }

    /// <summary>
    /// 속도, 위치 계산
    /// </summary>
    void CalVelocity_Position_EachPlanet()
    {
        //옛 가속도 저장
        foreach (var planet in _planetList)
        {
            planet.accelOld = planet.TotalAccel();
        }

        //위치 갱신
        foreach (var planet in _planetList)
        {
            planet.position += planet.velocity*dt+planet.accelOld*(dt*dt*0.5);
            planet.SetPosition(planet.position);
        }

        //새 가속도 저장
        foreach (var planet in _planetList)
        {
            planet.accelNew = planet.TotalAccel();
        }

        //속도 갱신(a는 old, new가속도 평균)
        foreach (var planet in _planetList)
        {
            planet.velocity += (planet.accelOld+planet.accelNew)*0.5*dt;
        }

        foreach (var planet in _planetList)
        {
            if (planet.name == "Earth")
            {
                Vector3D d = planet.centerPlanet.position - planet.position;
                double E = 0.5 * planet.velocity.Magnitude() * planet.velocity.Magnitude()
                    - ConstUtility.G * 198800 / d.Magnitude();
                Debug.Log(E);
            }
        }
    }
}

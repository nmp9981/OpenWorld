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

    public List<PlanetInfo> _planetList = new List<PlanetInfo>();

    private void FixedUpdate()
    {
        //여기서 행성 가속도, 위치 계산

        //가속도 계산

        //위치 계산
    }
}

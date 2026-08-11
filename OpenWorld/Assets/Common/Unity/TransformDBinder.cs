using UnityEngine;

public class TransformDBinder : MonoBehaviour
{
    public TransformD State { get; set; }

    private void Awake()=> State=transform.ToDouble();//초기값
    private void LateUpdate()=>State.ApplyTo(transform);//이후 단방향
}

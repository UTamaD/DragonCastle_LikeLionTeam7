using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dash Info", menuName = "Scriptable Object/Dash Info", order = int.MaxValue)]
public class DashInfo : ScriptableObject
{
    public float dashDis;
    public float dashTime;
}

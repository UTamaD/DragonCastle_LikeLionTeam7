using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponName
{
    Move = 0,
    IFrame,
    Melee,
}

public class WeaponManager : MonoBehaviour
{
    public WeaponBase CurrentWeapon { get; private set; }
    
    public Dictionary<WeaponName, WeaponBase> WeaponList;
}

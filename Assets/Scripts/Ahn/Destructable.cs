using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : Hittable
{
    public int health = 10;

    public int CurrentHealth { get; set; }
    public bool Invincible { get; set; }
}

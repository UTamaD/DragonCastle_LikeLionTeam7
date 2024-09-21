using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HB_AnimController : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            _animator.SetTrigger("Attack1");
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            _animator.SetTrigger("Attack2");
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            _animator.SetTrigger("Attack3");
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            _animator.SetTrigger("Attack4");
    }
}

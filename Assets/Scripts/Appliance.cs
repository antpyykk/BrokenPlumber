﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Appliance : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    public Sprite workingState;
    public Sprite brokenState;

    public bool isWorking = true; 
    
    // Start is called before the first frame update
    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = isWorking ? workingState : brokenState;
    }
    
    
}
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D _rgd2d;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider2D _boxCollider2D;

    private Vector2 cur_velo = Vector2.zero;
    
    private List<ContactPoint2D> contact_list = new List<ContactPoint2D>();
    private List<ContactPoint2D> ladder_contacts = new List<ContactPoint2D>();

    private bool is_grounded = false;
    private bool is_dropping = false;
    private bool pressed_jump = false;
    private bool pressed_fall = false;
    public bool ladder_mode = false;
    public bool pressed_repair = false;

    LayerMask ladderModeMask;
    ContactFilter2D  ladderModeFilter;
    
    [SerializeField]
    [Range(0, 40)]
    private float m_speed = 10.0f;
    
    [SerializeField]
    [Range(0.1f, 1.0f)]
    private float m_smooth_time = .05f;
    
    [SerializeField]
    [Range(0, 40)]
    private float m_jump_velocity = 5f;
    
    private float m_ladder_threshold = 0.4f;
    
    // Start is called before the first frame update
    private void Awake()
    {
        _rgd2d = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _boxCollider2D = GetComponent<BoxCollider2D>();

        ladderModeMask = LayerMask.GetMask("Ground");
        ladderModeFilter = new ContactFilter2D();
        ladderModeFilter.useLayerMask = true;
        ladderModeFilter.SetLayerMask(ladderModeMask);
    }

    private void Update()
    {
        pressed_jump |= Input.GetButtonDown("Jump");
        pressed_repair |= Input.GetKeyDown(KeyCode.R);
        pressed_fall |=  Input.GetButton("Jump") && Input.GetKey(KeyCode.S);
        Debug.Log(pressed_fall);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (ladder_mode)
        {
            ladderMode();
            return;            
        }

        var contactsCount = _rgd2d.GetContacts(contact_list);
        is_grounded = (contact_list.Take(contactsCount).Any(contact => contact.normal.y > 0.9f));
        
        if (is_grounded && pressed_fall)
        {
            _rgd2d.velocity = Vector2.zero;
            Debug.Log("FREEFALLING");
            _boxCollider2D.isTrigger = true;
            is_dropping = true;
        }

        Vector2 velocity;
        var newSpeed = is_dropping ? 0 : m_speed;
        var newVelocity = new Vector2(Input.GetAxisRaw("Horizontal") * newSpeed, (velocity = this._rgd2d.velocity).y);
        _rgd2d.velocity = Vector2.SmoothDamp(velocity, newVelocity, ref cur_velo, m_smooth_time);

        var absX = Math.Abs(velocity.x);
        if (absX > .1f)
        {
            _spriteRenderer.flipX = velocity.x > 0;
        }

        _animator.SetBool("Moving", !is_dropping && absX > 0.1f);

        if (is_dropping)
        {
            return;
        }
        
        if (is_grounded && pressed_jump)
        {
            is_grounded = false;
            _rgd2d.AddForce(new Vector2(0f, m_jump_velocity * 50));
            Debug.Log("PRESSED JUMP");
        }

        
        if (pressed_repair)
        {
            StartCoroutine(PlayerIsFixing());
        }
        
        pressed_jump = false;
        pressed_fall = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "PASSABLE_GROUND")
        {
            _boxCollider2D.isTrigger = false;
            is_dropping = false;
            Debug.Log("stop falling");
        }
    }

    private void ladderMode()
    {
        _rgd2d.bodyType = RigidbodyType2D.Kinematic;
        var newVelocity = new Vector2(0, Input.GetAxisRaw("Vertical") * 5);
        _rgd2d.velocity = newVelocity;

        var bounds = _boxCollider2D.bounds;
        var bottomBound = bounds.extents;
        var boxCenter = bounds.center - bottomBound;
        boxCenter.x += 0.5f;
        
        Debug.DrawLine(boxCenter, boxCenter + (Vector3.down * m_ladder_threshold), Color.magenta);
        
        var hit = Physics2D.Raycast(boxCenter, Vector2.down, m_ladder_threshold, ladderModeMask);
        var contactsCount = _rgd2d.GetContacts(ladderModeFilter, contact_list);
        Debug.Log("Contacts " + contactsCount);
        
        if(contactsCount <= 0 && hit)
        { 
            _rgd2d.bodyType = RigidbodyType2D.Dynamic; 
            ladder_mode = false;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("Stay");
        if (other.tag == "LADDER")
        {
            Debug.Log("FOUND LADDER");
        }
        if (!ladder_mode && other.tag == "LADDER" && Input.GetKey(KeyCode.K))
        {
            Debug.Log("HELLO");
            var transform1 = _rgd2d.transform;
            transform1.position = new Vector3(other.transform.position.x, transform1.position.y + 0.5f, 0);
            ladder_mode = true;
        }
    }

    IEnumerator PlayerIsFixing()
    {
        yield return new WaitForSeconds(0.1f);
        pressed_repair = false;
    }
}

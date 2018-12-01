﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimController : MonoBehaviour
{
    public enum gravityDirection { DOWN, LEFT, RIGHT, UP };
    public Animator anim;
    private Rigidbody rb;
    float speed;
    public float moveVertical;
    public float moveHorizontal;
    float maxJump;
    gravityDirection gravityDir;
    public float angle;
    bool CtrlPressed;
    bool AltPressed;

    public LayerMask groundLayers;
    public BoxCollider cldr;
    private float jumpAccel;
    private float gravAccel;
    public float verticalVelocity = 0.0f;
    private Vector3[] gravVectors = { new Vector3(0, 1, 0), new Vector3(0, -1, 0), new Vector3(-1, 0, 0), new Vector3(1, 0, 0) }; // Down, Up, Left, Right
    private bool jumping = false;
    public bool isGrounded = false;
    private bool canJump = true;
    private float finishedTime = .3f;

    // Use this for initialization
    void Start()
    {
        //Time.timeScale = .25f;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        speed = 2;
        moveVertical = 0;
        moveHorizontal = 0;
        maxJump = 8;
        rb.useGravity = false;
        gravityDir = gravityDirection.DOWN;
        angle = 0;
        CtrlPressed = false;
        AltPressed = false;

        cldr = GetComponent<BoxCollider>();
        jumpAccel = Physics.gravity.magnitude / 2;
        gravAccel = Physics.gravity.magnitude;
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level1");
        }
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            gravityDir = gravityDirection.UP;
            CtrlPressed = true;
        }
        if(Input.GetKeyDown(KeyCode.LeftAlt))
        {
            if (gravityDir == gravityDirection.RIGHT)//If already on right wall, go to left wall
            {
                gravityDir = gravityDirection.LEFT;
            }
            else// go to right wall
            {
                gravityDir = gravityDirection.RIGHT;
            }
            AltPressed = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded()) {     
            StartCoroutine(JumpTimer());
            jumping = true;
        }
        else
            jumping = false;

        //We can't have this code in the above if as lerp needs to keep working also after the key space gets pressed and only stop
        //when a 180 degrees rotation has been reached.
        if(CtrlPressed)
        {
            angle = Mathf.LerpAngle(angle, 180, 8 * Time.deltaTime);
            
            //stop rotation when the character is upside down
            if (transform.rotation.eulerAngles.x < 180)
            {
                transform.Rotate(new Vector3(angle * Mathf.Deg2Rad, 0, 0));
            }
            else
            {
                CtrlPressed = false;
            }
        }
        if(AltPressed)
        {
            float targetAngle = 90;
            if(Mathf.Abs(90 - angle) < 0.1)//We are on the right wall
            {
                targetAngle = -90;//rotate to left wall angle
            }
            angle = Mathf.LerpAngle(angle, targetAngle, 8 * Time.deltaTime);

            if((Mathf.Abs(transform.rotation.eulerAngles.z - targetAngle) > 0.1))
            {
                transform.Rotate(new Vector3(0, 0, angle * Mathf.Deg2Rad));
            }
            else
            {
                AltPressed = false;
            }
        }
    }
    private void FixedUpdate()
    {
        isGrounded = IsGrounded();
        moveVertical = Input.GetAxis("Vertical");
        moveHorizontal = Input.GetAxis("Horizontal");
        Vector3 movement;
        if (gravityDir == gravityDirection.DOWN || gravityDir == gravityDirection.UP)
        {
            movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        }
        else if(gravityDir == gravityDirection.RIGHT)
        {
            movement = new Vector3(0, moveHorizontal, moveVertical);
        }
        else//gravity direction is left
        {
            movement = new Vector3(0,-moveHorizontal, moveVertical);
        }
        //if (isGrounded)
        //{
            if (moveVertical != 0 || moveHorizontal != 0)
            {
                Vector3 upVector;
                if(gravityDir == gravityDirection.UP)
                {
                    upVector = new Vector3(0, -1, 0);
                }
                else if(gravityDir == gravityDirection.DOWN)
                {
                    upVector = new Vector3(0, 1, 0);
                }
                else if(gravityDir == gravityDirection.RIGHT)
                {
                    upVector = new Vector3(-1, 0, 0);
                }
                else//LEFT
                {
                    upVector = new Vector3(1, 0, 0);
                }
                
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement, upVector), 0.15F);
                anim.Play("run");
            }
            else
            {
                anim.Play("idle");
            }
        //}
        //else
        //{
        //    anim.Play("jump-float");
        //}
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
        if (IsGrounded())
        {
            if (jumping && canJump)
                rb.AddForce(new Vector3(0, 1, 0) * jumpAccel, ForceMode.Impulse);
        }
        rb.AddForce(getCurrentGravity());
    }

    private Vector3 getCurrentGravity() // Currently Not In Use
    {
        switch (gravityDir)
        {
            case gravityDirection.DOWN:
                {
                    return new Vector3(0, -Physics.gravity.magnitude, 0);
                }
            case gravityDirection.UP:
                {
                    return new Vector3(0, Physics.gravity.magnitude, 0);
                }
            case gravityDirection.LEFT:
                {
                    return new Vector3(-Physics.gravity.magnitude, 0, 0);
                }
            default://RIGHT
                {
                    return new Vector3(Physics.gravity.magnitude, 0, 0);
                }

                //still need cases for front and back
        }
    }

    private Vector3 getDirectionVector()
    {
        switch (gravityDir)
        {
            case gravityDirection.DOWN:
                return gravVectors[0];
            case gravityDirection.UP:
                return gravVectors[1];
            case gravityDirection.LEFT:
                return gravVectors[2];
            default://RIGHT
                return gravVectors[3];

            //still need cases for front and back
        }

    }

    public IEnumerator JumpTimer()
    {
        while (finishedTime > 0)
        {
            yield return new WaitForSeconds(0.1f);
            finishedTime -= 0.1f;
        }
        finishedTime = .3f;
        canJump = true;
    }

    // Checks if player is on the ground (currently only works if gravity is pointing down) - Justin
    // https://www.youtube.com/watch?v=vdOFUFMiPDU - video I used as a jumping tutorial
    private bool IsGrounded()
    {
        return Physics.CheckCapsule(cldr.bounds.center, new Vector3(cldr.bounds.center.x, cldr.bounds.min.y, cldr.bounds.center.z), Mathf.Min(cldr.size.x, cldr.size.z)/2 * .25f, groundLayers);
    }

    public gravityDirection getGravityDir()
    {
        return gravityDir;
    }

    public void takeDamage()
    {
        System.Random rnd = new System.Random();
        //add a bit of randomness in which director character is forced
        int xForceDir = rnd.Next(-1,1);
        int zForceDir = rnd.Next(-1, 1);
        Vector3 v = getDirectionVector();
        v.x = 3 * xForceDir;
        v.z = 3 * zForceDir;
        if(v.x == 0 && v.z == 0)
        {
            v.x = 3;
        }
        v.y *= 3;

        rb.AddForce(v, ForceMode.Impulse);
    }

}

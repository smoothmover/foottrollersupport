using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem;


public class FoottrollerCtrl : MonoBehaviour
{
    public static FoottrollerCtrl instance = null;
    public float joy_x = 0.0f;
    public float joy_y = 0.0f;
    public float heading_RF = 0.0f;
    public float heading_LF = 0.0f;
    private GameObject mainCam = null;
    private Vector3 campos;
    public float headsetheading;
    public float joystick_x, joystick_y;
    private int movState;
    private float movSpeed;
    public InputActionReference CalibRef = null;   // https://www.youtube.com/watch?v=ONlMEZs9Rgw
    public InputActionReference UpnDownRef = null;
    float refheadsetheading_cali;
    float headingRF_cur;
    float refheadingRF_cali;
    float ref_headingAngle, pre_heading, prepre_heading, preprepre_heading, prepreprepre_heading;
    public float MovHeading;
    public float upndownctrl;

    private GameObject forwardmark = null;
    private GameObject centermark = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    void OnEnable()
    {
    }

    void OnDisable()
    {
    }
    // Start is called before the first frame update
    void Start()
    {
        mainCam = GameObject.Find("Main Camera");
        if (CalibRef != null) {
            CalibRef.action.started += FoottrollerCalib;
        }
        movState = 0;
        movSpeed = 0f;
        forwardmark = GameObject.Find("forwardmark");
        centermark = GameObject.Find("centermark");
        upndownctrl = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (UpnDownRef != null)
        {
            upndownctrl = UpnDownRef.action.ReadValue<float>();
        }
        else {
            upndownctrl = 0.0f;
        }
        

        if (FoottrollerNet.instance.udpConnReady)
        {
            joystick_x = FoottrollerNet.instance.joystick_x;
            joystick_y = FoottrollerNet.instance.joystick_y;
            heading_RF = FoottrollerNet.instance.RFheading - 180.0f;
        }
        else {
            joystick_x = 0f;
            joystick_y = 0f;
        }
        
        

        Quaternion headsetrotation = mainCam.transform.rotation;
        headsetheading = headsetrotation.eulerAngles.y;
        headsetheading = deltaAngeval(headsetheading);

        headingRF_cur = - heading_RF;
        ref_headingAngle = headingRF_cur - refheadingRF_cali + refheadsetheading_cali;

        float tempH = ref_headingAngle - MovHeading;
        tempH = deltaAngeval(tempH);

        MovHeading = MovHeading + 3 * Time.deltaTime * tempH;

        if (Mathf.Abs(MovHeading) > 180)
        {
            MovHeading = deltaAngeval(MovHeading);
        }

        campos = Vector3.zero;
        if (mainCam != null)
        {
            campos = mainCam.transform.position;
        }
        

        if (Mathf.Abs(joystick_y) > Mathf.Abs(joystick_x))
        {
            if (joystick_y > 0)
            {
                movState = 1;
                movSpeed = joystick_y;
            }
            else
            {
                movState = -1;
                movSpeed = -joystick_y;
            }
        }
        else
        {
            if (joystick_x > 0)
            {
                movState = -2;
                movSpeed = joystick_x;
            }
            else
            {
                movState = 2;
                movSpeed = -joystick_x;
            }

        }

        float curmovSpeed = 0f;
        if (movState == 1)
        {
            curmovSpeed = movSpeed;
            ref_headingAngle = MovHeading;
        }
        else if (movState == -1)
        {
            curmovSpeed = -movSpeed;
            ref_headingAngle = MovHeading;
        }
        else if (movState == -2)
        {
            curmovSpeed = movSpeed;
            ref_headingAngle = MovHeading + 90f;
        }
        else if (movState == 2)
        {
            curmovSpeed = -movSpeed;
            ref_headingAngle = MovHeading + 90f;
        }
        Vector3 temppos;
        temppos = transform.position;
        Vector3 move = transform.forward;



        float fwd_x = move.x;
        float fwd_z = move.z;

        float dirx = Mathf.Cos(-ref_headingAngle * Mathf.PI / 180f) * fwd_x - Mathf.Sin(-ref_headingAngle * Mathf.PI / 180f) * fwd_z;
        float dirz = Mathf.Sin(-ref_headingAngle * Mathf.PI / 180f) * fwd_x + Mathf.Cos(-ref_headingAngle * Mathf.PI / 180f) * fwd_z;

        float movDist = 3 * curmovSpeed * Time.deltaTime;

        temppos.x = temppos.x + movDist * dirx;
        temppos.z = temppos.z + movDist * dirz;

        // up down movement control
        if (upndownctrl > 0.1f || upndownctrl < -0.1f)
        {
            temppos.y = temppos.y + upndownctrl * Time.deltaTime;
        }


        transform.position = temppos;


        Vector3 centerpos = temppos;
        //centerpos.x = centerpos.x + campos.x;
        //centerpos.z = centerpos.z + campos.z;
        centerpos = campos;

        if (centermark != null)
        {
            // Vector3 temppos2 = centerpos;
            // temppos2.y = temppos.y;
            centermark.transform.position = temppos; // temppos2;
        }

        if (forwardmark != null)
        {
            float dirBallx = Mathf.Cos(-MovHeading * Mathf.PI / 180f) * fwd_x - Mathf.Sin(-MovHeading * Mathf.PI / 180f) * fwd_z;
            float dirBallz = Mathf.Sin(-MovHeading * Mathf.PI / 180f) * fwd_x + Mathf.Cos(-MovHeading * Mathf.PI / 180f) * fwd_z;

            Vector3 ballpos;
            ballpos.x = 3f * dirBallx + centerpos.x;
            ballpos.z = 3f * dirBallz + centerpos.z;
            ballpos.y = centerpos.y;
            forwardmark.transform.position = ballpos;
        }

       //  return;

    }

    // utility funtions
    private float deltaAngeval(float deltaH)
    {

        float deltaH1 = deltaH + 360;
        float deltaH2 = deltaH - 360;
        if (Mathf.Abs(deltaH1) < Mathf.Abs(deltaH) && Mathf.Abs(deltaH1) < Mathf.Abs(deltaH2))
        {
            return deltaH1;
        }
        else if (Mathf.Abs(deltaH2) < Mathf.Abs(deltaH) && Mathf.Abs(deltaH2) < Mathf.Abs(deltaH1))
        {
            return deltaH2;
        }
        else
        {
            return deltaH;
        }

    }
    private void FoottrollerUpnDownMovCtrl(InputAction.CallbackContext context)
    {

    }
    private void FoottrollerCalib(InputAction.CallbackContext context)
    {
        // https://www.youtube.com/watch?v=jOn0YWoNFVY
        Debug.Log("Calib Button pressed");
        float bias = 130f; // Suppose to be zerio, but in different games, this bias may need to be adjusted to compensate a difference between headset direction and movement direction,
        // such that when calibration button is pressed, the forward movement direction (indicated by the marker) aligns with the current looking direction.
        refheadsetheading_cali = headsetheading - bias;
        //refheadingRF_cali = FoottrollerNet.instance.RFheading;
        refheadingRF_cali = headingRF_cur;
    }
}

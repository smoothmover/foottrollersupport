using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;


public class FoottrollerCtrl : MonoBehaviour
{
    public static FoottrollerCtrl instance = null;

    private GameObject mainCam = null;
    public InputActionReference CalibRef = null;
    public InputActionReference UpnDownRef = null;
    public InputActionReference LH_Pos = null;
    public InputActionReference RH_Pos = null;
    public InputActionReference btn_X_ref = null;
    public InputActionReference btn_Y_ref = null;
    public InputActionReference btn_A_ref = null;
    public InputActionReference btn_B_ref = null;
    public InputActionReference trigger_R_ref = null;
    public InputActionReference trigger_L_ref = null;
    public InputActionReference trigger_R_val_ref = null;
    public InputActionReference trigger_L_val_ref = null;
    public InputActionReference joystick_RH_val_ref = null;
    public InputActionReference joystick_LH_val_ref = null;

    public float headsetheading;
    float refheadsetheading_cali;
    float headingRF_cur;
    float refheadingRF_cali;
    float refheadingLF_cali;
    public float joystick_x, joystick_y;
    float ref_headingAngle, pre_heading, prepre_heading, preprepre_heading, prepreprepre_heading;
    int headingcheck;
    bool headingcheckpass;
    private int movState;
    private float movSpeed;
    public float MovHeading;
    private GameObject forwardmark = null;
    private GameObject centermark = null;
    public float upndownctrl;
    public Vector3 campos;
    // private Gamepad gamepad;
    // private Joystick joystick;
    public Vector2 moveInput;
    public Vector3 Left_Hand_Pos;
    public Vector3 Right_Hand_Pos;
    public float direction = 0;
    public bool btnA = false;
    public bool btnB = false;
    public bool btnX = false;
    public bool btnY = false;
    public bool triggerL = false;
    public bool triggerR = false;
    public float triggerL_value = 0;
    public float triggerR_value = 0;
    public Vector2 joystick_RH;
    public Vector2 joystick_LH;


    public int controlmode = 2;
    public float control_angle = 0;
    // input system setup for foottroller with Unity
    // https://youtu.be/HmXU4dZbaMw?si=31qSGjlFAsR6axoq

    // XT note: follow the vidoe above to set up the input system. Use listen to associate Foottroller inputs to controls.
    // To allow accurate passing of foot orientation info into unity, the axis's dead zone need to be disabled.
    // This cannot be done by adding a deadzon filter to the axis at input action - action maps - actions - processors - deadzone
    // The deadzone setting for the whole project needs to be removed.
    // This is done at Edit->Project Settings->Input System Package 
    // set Default Deadzone Min = 0, and Default Deadzone Max = 1 
    // Dont set deadzon max greater than 1, which will cause max axis value to be samller than 1, i.e., range be compressed.

    public Foottrollertest movCtrl;
    public InputAction Move;
    public InputAction Direction_rf;



    // Start is called before the first frame update

    void Start()
    {
        CalibRef.action.started += FoottrollerCalib;
        mainCam = GameObject.Find("Main Camera");
        refheadingRF_cali = 0f;
        refheadingLF_cali = 0f;
        movState = 0;
        movSpeed = 0f;
        forwardmark = GameObject.Find("forwardmark");
        centermark = GameObject.Find("centermark");
        upndownctrl = 0f;
        headingcheck = -1;
        // gamepad = Gamepad.current;
        // joystick = Joystick.current;
    }
    private void Awake()
    {
        movCtrl = new Foottrollertest();
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    private void OnEnable()
    {
        Move = movCtrl.move_foottroller.movement;
        Move.Enable();
        Direction_rf = movCtrl.move_foottroller.direction;
        Direction_rf.Enable();
    }
    private void OnDisable()
    {
        Move.Disable();
        Direction_rf.Disable();
    }
    // Update is called once per frame
    void Update()
    {


        upndownctrl = UpnDownRef.action.ReadValue<float>();
        Left_Hand_Pos = LH_Pos.action.ReadValue<Vector3>();
        Right_Hand_Pos = RH_Pos.action.ReadValue<Vector3>();
        if (btn_A_ref.action.ReadValue<float>() != 0)
        {
            btnA = true;
        }
        else {
            btnA = false;
        }

        if (btn_B_ref.action.ReadValue<float>() != 0)
        {
            btnB = true;
        }
        else
        {
            btnB = false;
        }
        if (btn_X_ref.action.ReadValue<float>() != 0)
        {
            btnX = true;
        }
        else
        {
            btnX = false;
        }
        if (btn_Y_ref.action.ReadValue<float>() != 0)
        {
            btnY = true;
        }
        else
        {
            btnY = false;
        }
        if (trigger_L_ref.action.ReadValue<float>() != 0)
        {
            triggerL = true;
        }
        else
        {
            triggerL = false;
        }
        if (trigger_R_ref.action.ReadValue<float>() != 0)
        {
            triggerR = true;
        }
        else
        {
            triggerR = false;
        }
        triggerL_value = trigger_L_val_ref.action.ReadValue<float>();
        triggerR_value = trigger_R_val_ref.action.ReadValue<float>();

        float dz = Right_Hand_Pos.z -  Left_Hand_Pos.z;
        float dx = Right_Hand_Pos.x - Left_Hand_Pos.x;
        float dy = Right_Hand_Pos.y - Left_Hand_Pos.y;
        //float angle0 = Mathf.Asin(dy / Mathf.Sqrt(dx * dx + dz * dz + dy * dy));
        float angle0 = dy / (2*Mathf.Sqrt(dx * dx + dz * dz));
        control_angle = angle0;
        if (control_angle > 1) {
            control_angle = 1;
        } else if (control_angle < -1) {
            control_angle = -1;
        }

            joystick_LH = joystick_LH_val_ref.action.ReadValue<Vector2>();
        joystick_RH = joystick_RH_val_ref.action.ReadValue<Vector2>();


        Quaternion headsetrotation = mainCam.transform.rotation;
        headsetheading = headsetrotation.eulerAngles.y;
        headsetheading = deltaAngeval(headsetheading);

        campos = Vector3.zero;
        if (mainCam != null)
        {
            campos = mainCam.transform.position;
        }

        moveInput = Move.ReadValue<Vector2>();
        direction = -Direction_rf.ReadValue<float>();

        //joystick_x = FoottrollerNet.instance.joystick_x;
        //joystick_y = FoottrollerNet.instance.joystick_y;

        if (controlmode == 1)
        {
            joystick_x = moveInput.x;
            joystick_y = moveInput.y;
        }
        else if (controlmode == 2)
        {
            joystick_x = FoottrollerNet.instance.joystick_x;
            joystick_y = FoottrollerNet.instance.joystick_y;
        }

        if (true)
        {
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
            if (controlmode == 1)
            {
                headingRF_cur = direction * 180.0f;
            }
            else if (controlmode == 2)
            {
                headingRF_cur = FoottrollerNet.instance.RFheading - 180.0f;
                headingRF_cur = -headingRF_cur;
            }

            // ref_headingAngle = FoottrollerNet.instance.RFheading - refheadingRF_cali + refheadsetheading_cali;
            ref_headingAngle = headingRF_cur - refheadingRF_cali + refheadsetheading_cali;

            float tempH = ref_headingAngle - MovHeading;
            tempH = deltaAngeval(tempH);
            /*
            if (Mathf.Abs(tempH) > 50)
            {
                //MovHeading = ref_headingAngle;
                headingcheckpass = false;
                if (headingcheck < 0)
                {
                    headingcheck = 1; // start heading check
                }
                else {
                    headingcheck = headingcheck + 1;
                }
                if (headingcheck > 10) {  // check if angle change is confirmed
                    if (Mathf.Abs(pre_heading - ref_headingAngle) < 20 && Mathf.Abs(prepre_heading - ref_headingAngle) < 20) {
                        if (Mathf.Abs(preprepre_heading - ref_headingAngle) < 20 && Mathf.Abs(prepreprepre_heading - ref_headingAngle) < 20) {
                            headingcheckpass = true;
                        }
                    }
                }
                prepreprepre_heading = preprepre_heading;
                preprepre_heading = prepre_heading;
                prepre_heading = pre_heading;
                pre_heading = ref_headingAngle;
            }
            else {
                headingcheck = -1;
            }
            if (headingcheck < 0 || headingcheckpass) {
                MovHeading = MovHeading + 3 * Time.deltaTime * tempH;
            }
            
             */

            MovHeading = MovHeading + 3 * Time.deltaTime * tempH;

            if (Mathf.Abs(MovHeading) > 180)
            {
                MovHeading = deltaAngeval(MovHeading);
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
                Vector3 temppos2 = centerpos;
                temppos2.y = temppos.y;
                centermark.transform.position = temppos2;
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
        }



    }

    private void FoottrollerUpnDownMovCtrl(InputAction.CallbackContext context)
    {

    }

    private void FoottrollerCalib(InputAction.CallbackContext context)
    {
        // https://www.youtube.com/watch?v=jOn0YWoNFVY
        // Debug.Log("Calib Button pressed");
        refheadsetheading_cali = headsetheading;
        //refheadingRF_cali = FoottrollerNet.instance.RFheading;
        refheadingRF_cali = headingRF_cur;
    }
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
}

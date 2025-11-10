using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoyconDemo : MonoBehaviour {
	
	private List<Joycon> joycons;

    // Values made available via Unity
    public float[] stick;
    public Vector3 gyro;
    public Vector3 accel;
    public int jc_ind = 0;
    public Quaternion orientation;

    // Movement using accel
    private Vector3 velocity = Vector3.zero;
    public float accelMoveScale = 3f;    // multiplicador para ajustar sensibilidade do acelerômetro
    public float accelDamping = 1.5f;    // amortecimento (quanto maior, mais rápido para de se mover)
    public float maxSpeed = 5f;          // velocidade máxima em m/s
    public float accelDeadzone = 0.05f;  // deadzone para ignorar ruído pequeno (em Gs)

    private Rigidbody rb;

    void Start ()
    {
        gyro = new Vector3(0, 0, 0);
        accel = new Vector3(0, 0, 0);
        
        // get the public Joycon array attached to the JoyconManager in scene
        joycons = JoyconManager.Instance.j;
        if (joycons.Count < jc_ind + 1)
        {
            Destroy(gameObject);
        }
        
        rb = GetComponent<Rigidbody>();
	}

void Update () {
    if (joycons.Count > 0)
    {
        Joycon j = joycons[jc_ind];

        if (j.GetButtonDown(Joycon.Button.SHOULDER_2))
        {
            Debug.Log("Shoulder button 2 pressed");
            Debug.Log(string.Format("Stick x: {0:N} Stick y: {1:N}", j.GetStick()[0], j.GetStick()[1]));
            j.Recenter();
        }
        if (j.GetButtonUp(Joycon.Button.SHOULDER_2))
        {
            Debug.Log("Shoulder button 2 released");
        }
        if (j.GetButton(Joycon.Button.SHOULDER_2))
        {
            Debug.Log("Shoulder button 2 held");
        }

        if (j.GetButtonDown(Joycon.Button.DPAD_DOWN)) {
            Debug.Log("Rumble");
            j.SetRumble(160, 320, 0.6f, 200);
        }

        stick = j.GetStick();
        gyro = j.GetGyro();
        accel = j.GetAccel();
        orientation = j.GetVector();
		Debug.Log("Stick:" + stick);
		Debug.Log("Gyro:" + gyro);
		Debug.Log("ACCEL:" + accel);
		Debug.Log("Vetor:" + orientation);

        // Cor do objeto só para feedback
		//	if (j.GetButton(Joycon.Button.DPAD_UP))
		//	{
		//		gameObject.GetComponent<Renderer>().material.color = Color.red;
		//	}
		//	else
		//	{
		//		gameObject.GetComponent<Renderer>().material.color = Color.blue;
		//	}

        // --- ROTACIONAR ---
        gameObject.transform.rotation = orientation;

        // --- MOVER ---
        // Usa o stick como entrada de movimento
        Vector3 move = new Vector3(stick[0], 0, stick[1]);

        // Aplica movimento relativo à rotação atual
        float moveSpeed = 2f; // ajusta a velocidade
        gameObject.transform.Translate(move * moveSpeed * Time.deltaTime, Space.Self);

        // Opcional: se quiser usar o acelerômetro também
        //gameObject.transform.Translate(accel * 0.1f * Time.deltaTime, Space.World);
    }
}

}

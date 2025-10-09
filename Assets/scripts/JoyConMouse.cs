using UnityEngine;

// Attach to any GameObject to make it behave like a mouse cursor using Joy-Con gyro.
// Requires your Joycon library (JoyconManager.Instance.j).
public class JoyConMouse : MonoBehaviour
{
    public float sensitivity = 18f;   // bigger -> faster
    public float smoothing = 0.85f;   // 0..1, closer to 1 = smoother
    public float deadzone = 0.02f;
    public bool invertX = false, invertY = true;
    public bool gyroIsRadians = false; // set true if gyro returns rad/s
    [SerializeField]
    public Camera cam;

    Vector2 screenCursor;    // pixel coordinates
    Vector2 vel = Vector2.zero;
    Vector2 screen;

    void Start()
    {
        screen = new Vector2(Screen.width, Screen.height);
        screenCursor = screen * 0.5f;
    }

    void Update()
    {
        var list = JoyconManager.Instance?.j;
        if (list == null || list.Count == 0) return;
        var j = list[0];

        // recenter button
        if (j.GetButtonDown(Joycon.Button.SHOULDER_2)) j.Recenter();

        // read gyro and prepare angular vector (tweak mapping if needed)
        Vector3 g = j.GetGyro();               // typical: deg/s or rad/s
        Vector2 ang = new Vector2(g.x, g.y);   // map axes (swap if it feels wrong)
        if (gyroIsRadians) ang *= Mathf.Rad2Deg;

        if (Mathf.Abs(ang.x) < deadzone) ang.x = 0;
        if (Mathf.Abs(ang.y) < deadzone) ang.y = 0;

        Vector2 delta = new Vector2((invertX ? -1 : 1) * ang.x,
                                    (invertY ? -1 : 1) * ang.y) * (sensitivity * Time.deltaTime);

        // smoothing and pixel scale
        vel = Vector2.Lerp(vel, delta, 1f - smoothing);
        screenCursor += new Vector2(vel.x, -vel.y) * 10f; // 10 = pixel scale (tune or expose if desired)

        // clamp to screen
        screenCursor.x = Mathf.Clamp(screenCursor.x, 0, screen.x);
        screenCursor.y = Mathf.Clamp(screenCursor.y, 0, screen.y);

        // convert to world position while keeping original Z
        if (cam != null)
        {
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenCursor.x, screenCursor.y,
                                        Mathf.Abs(cam.transform.position.z - transform.position.z)));
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);
        }
        else
        {
            // fallback: move in 2D using orthographic assumptions
            transform.position = new Vector3(screenCursor.x / screen.x - 0.5f, screenCursor.y / screen.y - 0.5f, transform.position.z);
        }
    }
}

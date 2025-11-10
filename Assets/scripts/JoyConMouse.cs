using UnityEngine;

// Attach to any GameObject to make it behave like a mouse cursor using Joy-Con accel.
// Requires your Joycon library (JoyconManager.Instance.j).
public class JoyConMouseAccel : MonoBehaviour
{
    [Header("Movement")]
    public float sensitivity = 600f;   // bigger -> faster (tweak)
    [Range(0f, 0.99f)] public float smoothing = 0.85f;   // 0..1, closer to 1 = smoother
    public float deadzone = 0.02f;    // ignore tiny noise
    public float pixelScale = 10f;    // how many pixels per accel unit (tweak)

    [Header("Axes")]
    public bool invertX = false;
    public bool invertY = true;

    [Header("Accel options")]
    public bool removeGravity = true; // try true if accel includes gravity (often yes)
    public Vector3 gravityEstimate = new Vector3(0f, -1f, 0f); // world gravity direction for subtraction (adjust if needed)
    public float gravityBlend = 0.98f; // low-pass filter speed to estimate gravity if removeGravity=true

    [Header("References")]
    public Camera cam;

    // internal
    Vector2 screenCursor;    // pixel coordinates
    Vector2 vel = Vector2.zero;
    Vector2 screen;
    Vector3 gravityLowPass = Vector3.zero; // for estimating gravity

    void Start()
    {
        screen = new Vector2(Screen.width, Screen.height);
        screenCursor = screen * 0.5f;
        gravityLowPass = gravityEstimate; // seed
    }

    void Update()
    {
        var list = JoyconManager.Instance?.j;
        if (list == null || list.Count == 0) return;
        var j = list[0];

        // recenter button (same as before)
        if (j.GetButtonDown(Joycon.Button.SHOULDER_2)) j.Recenter();

        // read accel (typical: Gs)
        Vector3 a = j.GetAccel(); // usually in G (1 = 1g) or m/s^2 depending on library

        // optional: estimate and remove gravity with low-pass filter
        Vector3 linear = a;
        if (removeGravity)
        {
            // low-pass filter to estimate gravity component in device space
            gravityLowPass = Vector3.Lerp(gravityLowPass, a, 1f - gravityBlend);
            linear = a - gravityLowPass;
        }

        // choose which axes to use for cursor movement; map device axes to screen axes.
        // You may need to swap x/y depending on how the Joy-Con is held.
        Vector2 accel2D = new Vector2(linear.x, linear.y);

        // apply deadzone per component
        if (Mathf.Abs(accel2D.x) < deadzone) accel2D.x = 0f;
        if (Mathf.Abs(accel2D.y) < deadzone) accel2D.y = 0f;

        // compute delta in pixels (scale by sensitivity and Time.deltaTime so it's framerate-independent)
        Vector2 delta = new Vector2((invertX ? -1f : 1f) * accel2D.x,
                                    (invertY ? -1f : 1f) * accel2D.y) * (sensitivity * Time.deltaTime);

        // smoothing and accumulate
        vel = Vector2.Lerp(vel, delta, 1f - smoothing);
        screenCursor += new Vector2(vel.x, -vel.y) * pixelScale;

        // clamp to screen
        screenCursor.x = Mathf.Clamp(screenCursor.x, 0f, screen.x);
        screenCursor.y = Mathf.Clamp(screenCursor.y, 0f, screen.y);

        // convert to world position while keeping original Z
        if (cam != null)
        {
            Vector3 wp = cam.ScreenToWorldPoint(new Vector3(screenCursor.x, screenCursor.y,
                                        Mathf.Abs(cam.transform.position.z - transform.position.z)));
            transform.position = new Vector3(wp.x, wp.y, transform.position.z);
        }
        else
        {
            // fallback: move in 2D using normalized screen coords
            transform.position = new Vector3(screenCursor.x / screen.x - 0.5f, screenCursor.y / screen.y - 0.5f, transform.position.z);
        }
    }
}

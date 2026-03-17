using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script allows the user to pickup predefined dice and then roll them. When you pick up the dice and
public class RollDrop : MonoBehaviour
{
    // This list shows what dice are going to be and can be edited via code if you want or in the inspector. 
    [SerializeField] List<GameObject> diceGroup = new List<GameObject>();
    //Height in which the dice will be picked up at. 
    [SerializeField] float pickUpHeight = 2;
    [SerializeField] float followStrength = 18f;
    [SerializeField] float maxFollowSpeed = 14f;
    [SerializeField] float minThrowSpeed = 3f;
    [SerializeField] float maxThrowSpeed = 16f;
    [SerializeField] float throwUpBoost = 2.4f;
    [SerializeField] float throwTorqueStrength = 11f;
    [SerializeField] float chargeDuration = 1.15f;
    [SerializeField] float holdShakePosition = 0.28f;
    [SerializeField] float holdShakeTorque = 9f;

    Camera cam;
    bool isHolding;
    Vector3 lastTarget;
    Vector3 sampledVelocity;
    float lastSampleTime;
    float dragPlaneY;
    float holdStartTime;
    float holdNoiseSeed;

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }
    // Update is called once per frame
    void Update()
    {
        PickupDropBehavior();
    }
    void PickupDropBehavior()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return;
            }
        }

        bool holdingNow = Input.GetMouseButton(0);

        // When the user holds the mouse, dice follows the mouse target at lift height.
        if (holdingNow)
        {
            Vector3 target;
            if (TryGetMouseTarget(out target))
            {
                target.y = pickUpHeight;

                if (!isHolding)
                {
                    dragPlaneY = EstimateDragPlaneY();
                    holdStartTime = Time.time;
                    holdNoiseSeed = Random.value * 100f;
                    lastTarget = target;
                    sampledVelocity = Vector3.zero;
                    lastSampleTime = Time.time;
                }

                float holdCharge = GetHoldCharge01();
                target += GetHoldShakeOffset(holdCharge);

                float dt = Mathf.Max(Time.time - lastSampleTime, 0.0001f);
                Vector3 instantVelocity = (target - lastTarget) / dt;
                sampledVelocity = Vector3.Lerp(sampledVelocity, instantVelocity, 0.5f);
                lastTarget = target;
                lastSampleTime = Time.time;

                for (int i = 0; i < diceGroup.Count; i++)
                {
                    if (diceGroup[i] == null)
                    {
                        continue;
                    }

                    Rigidbody rb = diceGroup[i].GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        continue;
                    }

                    Vector3 delta = target - rb.position;
                    Vector3 desiredVelocity = Vector3.ClampMagnitude(delta * followStrength, maxFollowSpeed);
                    rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, 0.75f);
                    rb.angularVelocity *= 0.85f;
                    if (holdShakeTorque > 0f)
                    {
                        rb.AddTorque(Random.onUnitSphere * holdShakeTorque * (0.35f + holdCharge), ForceMode.Acceleration);
                    }
                }
            }
        }

        // On release, apply a throw impulse derived from drag direction and speed.
        if (isHolding && Input.GetMouseButtonUp(0))
        {
            float holdCharge = GetHoldCharge01();
            Vector3 planarVelocity = new Vector3(sampledVelocity.x, 0f, sampledVelocity.z);
            float planarSpeed = planarVelocity.magnitude;

            Vector3 throwDir;
            if (planarSpeed > 0.05f)
            {
                throwDir = planarVelocity.normalized;
            }
            else
            {
                throwDir = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (throwDir.sqrMagnitude < 0.001f)
                {
                    throwDir = Vector3.forward;
                }
            }

            float throwSpeed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, holdCharge);
            throwSpeed = Mathf.Clamp(throwSpeed, 0f, maxThrowSpeed);
            float upBoost = throwUpBoost + throwSpeed * 0.08f;

            Vector3 throwVelocity;
            if (throwSpeed <= 0.001f && throwUpBoost <= 0.001f)
            {
                throwVelocity = Vector3.zero;
            }
            else
            {
                throwVelocity = throwDir * throwSpeed + Vector3.up * upBoost;
            }

            for (int i = 0; i < diceGroup.Count; i++)
            {
                if (diceGroup[i] == null)
                {
                    continue;
                }

                Rigidbody rb = diceGroup[i].GetComponent<Rigidbody>();
                if (rb == null)
                {
                    continue;
                }

                rb.linearVelocity = throwVelocity;
                if (throwTorqueStrength > 0f)
                {
                    rb.AddTorque(Random.onUnitSphere * throwTorqueStrength * (0.75f + holdCharge), ForceMode.VelocityChange);
                }
                rb.WakeUp();
            }
        }

        isHolding = holdingNow;
    }

    bool TryGetMouseTarget(out Vector3 target)
    {
        target = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Use a fixed horizontal drag plane so the die tracks mouse reliably
        // instead of snapping to its own collider or hidden arena walls.
        Plane dragPlane = new Plane(Vector3.up, new Vector3(0f, dragPlaneY, 0f));
        float enter;
        if (dragPlane.Raycast(ray, out enter))
        {
            target = ray.GetPoint(enter);
            return true;
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 500f))
        {
            target = hit.point;
            return true;
        }

        return false;
    }

    float EstimateDragPlaneY()
    {
        for (int i = 0; i < diceGroup.Count; i++)
        {
            if (diceGroup[i] == null)
            {
                continue;
            }

            Rigidbody rb = diceGroup[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                return rb.position.y;
            }

            return diceGroup[i].transform.position.y;
        }

        return 0f;
    }

    float GetHoldCharge01()
    {
        if (chargeDuration <= 0.001f)
        {
            return 1f;
        }

        return Mathf.Clamp01((Time.time - holdStartTime) / chargeDuration);
    }

    Vector3 GetHoldShakeOffset(float holdCharge)
    {
        if (holdShakePosition <= 0f || holdCharge <= 0f)
        {
            return Vector3.zero;
        }

        float t = Time.time * 40f;
        float x = (Mathf.PerlinNoise(holdNoiseSeed, t) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(holdNoiseSeed + 17f, t) - 0.5f) * 2f;
        float y = Mathf.Abs(Mathf.Sin(Time.time * 32f + holdNoiseSeed)) * 0.25f;
        return new Vector3(x, y, z) * (holdShakePosition * holdCharge);
    }
}

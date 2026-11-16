using UnityEngine;

/// <summary>Flying hull: hovers with a gentle bob, banks into sideways motion, pitches with
/// forward/back speed and drives thruster particles.</summary>
public class FlyingHullVisual : HullVisual
{
    [Header("Body (the part that tilts and bobs)")]
    [SerializeField] Transform bodyRoot;
    [SerializeField] float maxRoll = 22f;
    [SerializeField] float maxPitch = 12f;
    [SerializeField] float tiltPerSpeed = 3f;       // degrees per m/s
    [SerializeField] float tiltSmooth = 8f;
    [SerializeField] float bobAmplitude = 0.08f;
    [SerializeField] float bobSpeed = 2.5f;

    [Header("Thrusters")]
    [SerializeField] ParticleSystem[] thrusters;
    [SerializeField] float idleRate = 6f;
    [SerializeField] float maxRate = 45f;
    [SerializeField] float speedForMaxRate = 8f;

    Quaternion startRot;
    Vector3 startPos;

    void Awake()
    {
        if (!bodyRoot) return;
        startRot = bodyRoot.localRotation;
        startPos = bodyRoot.localPosition;
    }

    protected override void Tick(float dt)
    {
        if (bodyRoot)
        {
            float roll = Mathf.Clamp(-LocalVelocity.x * tiltPerSpeed, -maxRoll, maxRoll);   // bank into the move
            float pitch = Mathf.Clamp(LocalVelocity.z * tiltPerSpeed, -maxPitch, maxPitch); // nose down when going forward
            Quaternion target = startRot * Quaternion.Euler(pitch, 0f, roll);
            bodyRoot.localRotation = Quaternion.Slerp(bodyRoot.localRotation, target, 1f - Mathf.Exp(-tiltSmooth * dt));
            bodyRoot.localPosition = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }

        float t = Mathf.Clamp01(LocalVelocity.magnitude / speedForMaxRate);
        float rate = Mathf.Lerp(idleRate, maxRate, t);
        foreach (var ps in thrusters)
        {
            if (!ps) continue;
            var e = ps.emission;
            e.rateOverTime = rate;
        }
    }
}

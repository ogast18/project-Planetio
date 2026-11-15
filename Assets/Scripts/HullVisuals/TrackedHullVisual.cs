using UnityEngine;

/// <summary>Tank hull: each track scrolls at its own speed, so turning on the spot
/// makes the tracks run in opposite directions.</summary>
public class TrackedHullVisual : HullVisual
{
    [SerializeField] Renderer leftTrack;
    [SerializeField] Renderer rightTrack;
    [SerializeField] float trackHalfWidth = 1.0f;   // centre of hull to the middle of one track
    [SerializeField] float scrollPerMetre = 0.5f;   // texture offset per metre travelled
    [SerializeField] Vector2 scrollDirection = Vector2.up;
    [SerializeField] string textureProperty = "_BaseMap";   // URP Lit; use _MainTex for Built-in

    Material leftMat, rightMat;
    float leftOffset, rightOffset;

    void Awake()
    {
        if (leftTrack) leftMat = leftTrack.material;     // instances, so tracks don't share offsets
        if (rightTrack) rightMat = rightTrack.material;
    }

    protected override void Tick(float dt)
    {
        float turn = YawRate * trackHalfWidth * Scale;   // right turn: left track faster
        float left = ForwardSpeed + turn;
        float right = ForwardSpeed - turn;

        leftOffset = Mathf.Repeat(leftOffset + left * scrollPerMetre * dt, 1f);
        rightOffset = Mathf.Repeat(rightOffset + right * scrollPerMetre * dt, 1f);

        if (leftMat) leftMat.SetTextureOffset(textureProperty, scrollDirection * leftOffset);
        if (rightMat) rightMat.SetTextureOffset(textureProperty, scrollDirection * rightOffset);
    }

    void OnDestroy()
    {
        if (leftMat) Destroy(leftMat);
        if (rightMat) Destroy(rightMat);
    }
}

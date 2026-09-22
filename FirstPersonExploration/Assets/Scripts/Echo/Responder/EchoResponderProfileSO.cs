// EchoResponderProfileSO
// Responsibility: Shared, read-only rules and look for echo responders: when an echo engages versus
// only acknowledges, how forgiving reach is, the answer timing, and the answering shell. Distances
// are measured to the object's surface. The answering shell is always full size; only its
// brightness tells engage from acknowledge. Flyweight asset; one profile per kind of object, never
// written to at runtime.
using Game.Core;
using UnityEngine;

namespace Game.Echo
{
    [CreateAssetMenu(fileName = "EchoResponderProfile", menuName = "Game/Echo/Responder Profile")]
    public sealed class EchoResponderProfileSO : ScriptableObject
    {
        [Header("Reach")]
        [Tooltip("Forgiveness in metres. The object counts as reached, and as in range, this much before the shell actually touches its surface.")]
        [SerializeField, Min(0f)] private float reachGrace = 0.25f;

        [Header("Engage (commits state)")]
        [Tooltip("Only echoes emitted within this distance of the object's surface engage. 0 or less lets the whole wave engage, which is dangerous for anything irreversible.")]
        [SerializeField] private float engageDistance = 2.5f;
        [Tooltip("Below this charge the echo only acknowledges.")]
        [SerializeField, Range(0f, 1f)] private float minimumCharge = 0f;
        [Tooltip("Engage once, then only ever acknowledge. Leave off if the reaction guards its own completed state.")]
        [SerializeField] private bool engageOnce;
        [Tooltip("Seconds after an engage during which further echoes only acknowledge.")]
        [SerializeField, Min(0f)] private float refractoryPeriod = 0.5f;
        [Tooltip("Engage only if nothing on the occlusion layers blocks the straight line from the echo origin. Sound passes walls; commitment does not.")]
        [SerializeField] private bool requireLineOfSight = true;
        [Tooltip("Layers that block engaging. Exclude the Player layer.")]
        [SerializeField] private LayerMask occlusionMask = 1;

        [Header("Acknowledge (expressive only)")]
        [Tooltip("Answer whenever the wave reaches this object but does not engage it. Untick to answer only on engage.")]
        [SerializeField] private bool acknowledge = true;

        [Header("Timing")]
        [Tooltip("Beat between the front arriving and the answer. 0.06 to 0.12 feels like call and response.")]
        [SerializeField, Min(0f)] private float responseDelay = 0.08f;

        [Header("Answering shell")]
        [SerializeField] private bool emitShell = true;
        [SerializeField] private PooledInstance shellPrefab;
        [SerializeField] private EchoShellProfileSO shellProfile;
        [Tooltip("Radius of every answer, engage or acknowledge, at any distance.")]
        [SerializeField, Min(0.05f)] private float shellRadius = 1.8f;
        [SerializeField, Min(0.05f)] private float shellDuration = 0.45f;
        [SerializeField, Range(0f, 0.5f)] private float shellStartRadius01 = 0.15f;
        [ColorUsage(true, true)]
        [SerializeField] private Color shellColour = new Color(0.7f, 1.4f, 1.5f, 1f);
        [Tooltip("Brightness of an acknowledge answer relative to an engage answer. Set to 1 to make both identical.")]
        [SerializeField, Range(0f, 1f)] private float acknowledgeBrightness = 0.6f;

        public float ReachGrace => reachGrace;
        public float EngageDistance => engageDistance;
        public float MinimumCharge => minimumCharge;
        public bool EngageOnce => engageOnce;
        public float RefractoryPeriod => refractoryPeriod;
        public bool RequireLineOfSight => requireLineOfSight;
        public LayerMask OcclusionMask => occlusionMask;
        public bool Acknowledge => acknowledge;
        public float ResponseDelay => responseDelay;
        public bool EmitShell => emitShell;
        public PooledInstance ShellPrefab => shellPrefab;
        public EchoShellProfileSO ShellProfile => shellProfile;
        public float ShellRadius => shellRadius;
        public float ShellDuration => shellDuration;
        public float ShellStartRadius01 => shellStartRadius01;

        public Color ShellColourFor(bool engaged)
        {
            Color colour = shellColour * (engaged ? 1f : acknowledgeBrightness);
            colour.a = 1f;
            return colour;
        }
    }
}
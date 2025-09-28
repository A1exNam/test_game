using UnityEngine;

namespace GW.Gameplay
{
    /// <summary>
    /// Runtime data for a foil pattern instance. Allows the renderer to display procedural
    /// variation without mutating the underlying definition asset.
    /// </summary>
    public readonly struct FoilPatternRuntime
    {
        public FoilPatternRuntime(
            FoilPatternDef definition,
            Vector2 flowDirection,
            float frequency,
            float specular,
            float embossDepth,
            Color primaryTint,
            Color secondaryTint,
            float pulseSpeed)
        {
            Definition = definition;
            FlowDirection = flowDirection.sqrMagnitude > Mathf.Epsilon ? flowDirection.normalized : Vector2.right;
            Frequency = Mathf.Max(0.01f, frequency);
            Specular = Mathf.Clamp01(specular);
            EmbossDepth = Mathf.Clamp01(embossDepth);
            PrimaryTint = primaryTint;
            SecondaryTint = secondaryTint;
            PulseSpeed = Mathf.Max(0.01f, pulseSpeed);
        }

        public FoilPatternDef Definition { get; }
        public Vector2 FlowDirection { get; }
        public float Frequency { get; }
        public float Specular { get; }
        public float EmbossDepth { get; }
        public Color PrimaryTint { get; }
        public Color SecondaryTint { get; }
        public float PulseSpeed { get; }
    }
}

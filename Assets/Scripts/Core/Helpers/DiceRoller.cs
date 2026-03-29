using UnityEngine;

namespace GlobalDomination
{
    /// <summary>
    /// Dice math and profiles for UI / game data (not the third-party animated roller component).
    /// </summary>
    public static class DiceRoller
    {
        public struct BuildCityLaunchProfile
        {
            public float sideSign;
            public float sideDistance;
            public float forwardOffset;
            public float holdHeight;
            public Vector2 handHoldPos;
            public Vector2 handReleasePos;
            public Vector2 nearZeroThrowFallback;
        }

        public struct ThrowForceProfile
        {
            public float throwImpulse;
            public float upImpulse;
            public Vector3 throwSpin;
            public Quaternion releaseRotation;
        }

        /// <summary>
        /// Rolls a single 6-sided die.
        /// </summary>
        /// <returns>An integer between 1 and 6.</returns>
        public static int RollD6()
        {
            return Random.Range(1, 7);
        }

        /// <summary>
        /// Rolls a specified number of 6-sided dice and returns their sum.
        /// </summary>
        public static int Roll(int numDice)
        {
            int total = 0;
            for (int i = 0; i < numDice; i++)
            {
                total += RollD6();
            }

            return total;
        }

        /// <summary>
        /// Produces a randomized launch profile used by the build-city physical dice throw.
        /// </summary>
        public static BuildCityLaunchProfile CreateBuildCityLaunchProfile()
        {
            int launchPreset = Random.Range(0, 6);
            float sideSign = Random.value < 0.5f ? -1f : 1f;

            BuildCityLaunchProfile profile = new BuildCityLaunchProfile
            {
                sideSign = sideSign
            };

            if (launchPreset <= 1)
            {
                profile.sideDistance = Random.Range(1.9f, 2.75f);
                profile.forwardOffset = Random.Range(-0.65f, 0.55f);
                profile.holdHeight = Random.Range(11.7f, 12.9f);
                profile.handHoldPos = new Vector2(sideSign * Random.Range(145f, 205f), Random.Range(82f, 108f));
                profile.handReleasePos = profile.handHoldPos + new Vector2(-sideSign * Random.Range(120f, 150f), Random.Range(18f, 34f));
                profile.nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(190f, 235f), Random.Range(16f, 40f));
            }
            else if (launchPreset <= 3)
            {
                profile.sideDistance = Random.Range(0.35f, 1.05f) * sideSign;
                profile.forwardOffset = Random.Range(-0.5f, 0.5f);
                profile.holdHeight = Random.Range(11.4f, 12.5f);
                profile.handHoldPos = new Vector2(sideSign * Random.Range(65f, 120f), Random.Range(88f, 116f));
                profile.handReleasePos = profile.handHoldPos + new Vector2(-sideSign * Random.Range(95f, 128f), Random.Range(16f, 30f));
                profile.nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(165f, 205f), Random.Range(14f, 30f));
            }
            else
            {
                profile.sideDistance = Random.Range(1.2f, 2.2f) * sideSign;
                profile.forwardOffset = Random.Range(-1.2f, 1.2f);
                profile.holdHeight = Random.Range(11.8f, 13.2f);
                profile.handHoldPos = new Vector2(sideSign * Random.Range(120f, 188f), Random.Range(78f, 122f));
                profile.handReleasePos = profile.handHoldPos + new Vector2(-sideSign * Random.Range(105f, 148f), Random.Range(20f, 38f));
                profile.nearZeroThrowFallback = new Vector2(-sideSign * Random.Range(180f, 240f), Random.Range(18f, 45f));
            }

            return profile;
        }

        /// <summary>
        /// Generates impulse and torque settings for the physical throw from the current hold charge.
        /// </summary>
        public static ThrowForceProfile CreateThrowForceProfile(Vector3 throwDir, float holdCharge)
        {
            float forceFromShake = Mathf.Lerp(0.62f, 1.45f, holdCharge);
            float throwImpulse = Mathf.Lerp(5.4f, 9.8f, holdCharge) * forceFromShake * Random.Range(0.92f, 1.14f);
            float upImpulse = Mathf.Lerp(1.15f, 2.25f, holdCharge) * Mathf.Lerp(0.92f, 1.22f, holdCharge) * Random.Range(0.9f, 1.16f);

            Vector3 spinAxis = Vector3.Cross(Vector3.up, throwDir).normalized;
            Vector3 throwSpin = spinAxis * Mathf.Lerp(5f, 9f, holdCharge)
                + Vector3.up * Mathf.Lerp(7f, 13f, holdCharge);
            throwSpin += Random.onUnitSphere * Random.Range(2.4f, 6.4f) * Mathf.Lerp(0.8f, 1.35f, holdCharge);

            return new ThrowForceProfile
            {
                throwImpulse = throwImpulse,
                upImpulse = upImpulse,
                throwSpin = throwSpin,
                releaseRotation = Random.rotationUniform
            };
        }

        /// <summary>
        /// Uses the detected animated face value when valid, otherwise falls back to a random D6.
        /// </summary>
        public static int ResolveAnimatedD6Result(int detectedSide)
        {
            return detectedSide >= 1 && detectedSide <= 6 ? detectedSide : RollD6();
        }
    }
}

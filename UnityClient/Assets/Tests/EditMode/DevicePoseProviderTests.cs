using NUnit.Framework;
using TransparentEarth.Sensors;
using UnityEngine;

namespace TransparentEarth.Tests
{
    public sealed class DevicePoseProviderTests
    {
        [Test]
        public void CircularSmoothingUsesShortPathAcrossNorth()
        {
            var result = DevicePoseProvider.SmoothCircularHeading(359f, 1f, .1f, 2f, 90f, 0f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(result, 1f)), Is.LessThan(2f));
            Assert.That(Mathf.DeltaAngle(359f, result), Is.GreaterThan(0f));
        }

        [Test]
        public void CircularSmoothingIgnoresCompassNoiseInsideDeadZone()
        {
            var result = DevicePoseProvider.SmoothCircularHeading(42f, 42.5f, .1f, 2f, 90f, .8f);
            Assert.That(result, Is.EqualTo(42f).Within(1e-6f));
        }

        [Test]
        public void HorizontalHeadingIsStableForOrdinaryYaw()
        {
            Assert.That(DevicePoseProvider.TryGetHorizontalHeading(Quaternion.Euler(0f, 90f, 0f),
                out var heading), Is.True);
            Assert.That(heading, Is.EqualTo(90f).Within(1e-4f));
        }

        [Test]
        public void HeadingIsUndefinedWhenLookingVertically()
        {
            Assert.That(DevicePoseProvider.TryGetHorizontalHeading(Quaternion.Euler(90f, 0f, 0f), out _),
                Is.False);
        }

        [Test]
        public void StationarySensorRecenteringIsRejected()
        {
            var current = Quaternion.identity;
            var recentered = Quaternion.Euler(0f, 28f, 0f);
            var result = DevicePoseProvider.StabilizeAttitude(current, recentered, 0f, 1f / 60f);
            Assert.That(Quaternion.Angle(current, result), Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void PhysicalRotationAllowsAttitudeToMove()
        {
            var current = Quaternion.identity;
            var target = Quaternion.Euler(0f, 12f, 0f);
            var result = DevicePoseProvider.StabilizeAttitude(current, target, 90f * Mathf.Deg2Rad, 1f / 60f);
            Assert.That(Quaternion.Angle(current, result), Is.GreaterThan(0f));
            Assert.That(Quaternion.Angle(current, result), Is.LessThan(Quaternion.Angle(current, target)));
        }
    }
}

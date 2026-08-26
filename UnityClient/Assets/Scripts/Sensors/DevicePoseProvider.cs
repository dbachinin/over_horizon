using UnityEngine;

namespace TransparentEarth.Sensors
{
    public sealed class DevicePoseProvider : MonoBehaviour
    {
        [SerializeField, Range(1f, 30f)] private float smoothing = 5.5f;
        [SerializeField, Range(.1f, 5f)] private float compassSmoothing = 1.35f;
        [SerializeField, Range(.1f, 5f)] private float maximumNorthCorrectionSpeed = .5f;
        [SerializeField, Range(0f, 5f)] private float compassDeadZoneDegrees = 1.2f;
        [SerializeField, Range(1f, 15f)] private float maximumCompassInnovationDegrees = 6f;
        [SerializeField, Range(0f, 10f)] private float maximumNorthDriftDegrees = 2.5f;
        public float HeadingDegrees { get; private set; }
        public float PitchDegrees { get; private set; }
        public bool SensorAvailable { get; private set; }

        private Quaternion _smoothedRotation = Quaternion.identity;
        private Quaternion _acceptedSensorRotation = Quaternion.identity;
        private float _northOffsetDegrees;
        private float _initialNorthOffsetDegrees;
        private float _smoothedCompassHeading;
        private bool _hasNorthFix;
        private bool _hasCompassHeading;
        private bool _hasPose;
        private bool _hasAcceptedSensorRotation;

        private void OnEnable()
        {
            Input.gyro.enabled = SystemInfo.supportsGyroscope;
            Input.gyro.updateInterval = 1f / 60f;
            Input.compass.enabled = true;
            SensorAvailable = SystemInfo.supportsGyroscope;
            _hasNorthFix = false;
            _hasCompassHeading = false;
            _hasPose = false;
            _hasAcceptedSensorRotation = false;
        }

        private void Update()
        {
            if (SystemInfo.supportsGyroscope)
            {
                var attitude = Input.gyro.attitude;
                var unityAttitude = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                var screenCorrection = Quaternion.Euler(90f, 0f, 0f);
                var target = screenCorrection * unityAttitude;
                if (Input.compass.enabled && Input.compass.headingAccuracy >= 0f && Input.compass.timestamp > 0d)
                {
                    var accuracy = Mathf.Clamp(Input.compass.headingAccuracy, 0f, 60f);
                    var accuracyWeight = 1f - Mathf.InverseLerp(5f, 45f, accuracy);
                    var headingResponse = Mathf.Lerp(.35f, compassSmoothing, accuracyWeight);
                    var headingSpeed = Mathf.Lerp(8f, 45f, accuracyWeight);
                    if (!_hasCompassHeading)
                    {
                        _smoothedCompassHeading = Input.compass.trueHeading;
                        _hasCompassHeading = true;
                    }
                    else
                    {
                        _smoothedCompassHeading = SmoothCircularHeading(_smoothedCompassHeading,
                            Input.compass.trueHeading, Time.unscaledDeltaTime, headingResponse, headingSpeed,
                            compassDeadZoneDegrees);
                    }

                    // Euler yaw becomes unstable while looking up or down. A horizontal forward
                    // projection provides a stable gyro heading and deliberately has no value
                    // near the vertical, where azimuth is physically undefined.
                    if (TryGetHorizontalHeading(target, out var gyroHeading))
                    {
                        var measuredOffset = Mathf.DeltaAngle(gyroHeading, _smoothedCompassHeading);
                        if (!_hasNorthFix)
                        {
                            _northOffsetDegrees = measuredOffset;
                            _initialNorthOffsetDegrees = measuredOffset;
                            _hasNorthFix = true;
                        }
                        else
                        {
                            var innovation = Mathf.DeltaAngle(_northOffsetDegrees, measuredOffset);
                            if (Mathf.Abs(innovation) <= maximumCompassInnovationDegrees)
                            {
                                var smoothedOffset = SmoothCircularHeading(_northOffsetDegrees, measuredOffset,
                                    Time.unscaledDeltaTime, .4f, maximumNorthCorrectionSpeed, .35f);
                                var signedOffset = Mathf.DeltaAngle(0f, smoothedOffset);
                                var drift = Mathf.Clamp(Mathf.DeltaAngle(_initialNorthOffsetDegrees, signedOffset),
                                    -maximumNorthDriftDegrees, maximumNorthDriftDegrees);
                                _northOffsetDegrees = Mathf.DeltaAngle(0f, _initialNorthOffsetDegrees + drift);
                            }
                        }
                    }
                }

                if (_hasNorthFix)
                    target = Quaternion.AngleAxis(_northOffsetDegrees, Vector3.up) * target;

                if (!_hasAcceptedSensorRotation)
                {
                    _acceptedSensorRotation = target;
                    _hasAcceptedSensorRotation = true;
                }
                else
                {
                    _acceptedSensorRotation = StabilizeAttitude(_acceptedSensorRotation, target,
                        Input.gyro.rotationRateUnbiased.magnitude, Time.unscaledDeltaTime);
                }
                target = _acceptedSensorRotation;

                var blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
                if (!_hasPose)
                {
                    _smoothedRotation = target;
                    _hasPose = true;
                }
                else _smoothedRotation = Quaternion.Slerp(_smoothedRotation, target, blend);
                transform.localRotation = _smoothedRotation;
            }
            else
            {
                var yaw = Mathf.Sin(Time.time * .18f) * 12f;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, yaw, 0), .03f);
            }

            if (TryGetHorizontalHeading(transform.localRotation, out var visibleHeading))
                HeadingDegrees = visibleHeading;
            PitchDegrees = NormalizeAngle(transform.eulerAngles.x);
        }

        public static float SmoothCircularHeading(float current, float target, float deltaTime, float response,
            float maximumDegreesPerSecond, float deadZoneDegrees)
        {
            var delta = Mathf.DeltaAngle(current, target);
            var magnitude = Mathf.Abs(delta);
            if (magnitude <= deadZoneDegrees) return Mathf.Repeat(current, 360f);
            var effectiveDelta = Mathf.Sign(delta) * (magnitude - deadZoneDegrees);
            var responseStep = effectiveDelta * (1f - Mathf.Exp(-Mathf.Max(0f, response) * deltaTime));
            var maximumStep = Mathf.Max(0f, maximumDegreesPerSecond) * Mathf.Max(0f, deltaTime);
            return Mathf.Repeat(current + Mathf.Clamp(responseStep, -maximumStep, maximumStep), 360f);
        }

        public static bool TryGetHorizontalHeading(Quaternion rotation, out float heading)
        {
            var forward = rotation * Vector3.forward;
            var horizontalMagnitudeSquared = forward.x * forward.x + forward.z * forward.z;
            if (horizontalMagnitudeSquared < .0025f)
            {
                heading = 0f;
                return false;
            }

            heading = Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
            return true;
        }

        public static Quaternion StabilizeAttitude(Quaternion current, Quaternion target,
            float angularSpeedRadiansPerSecond, float deltaTime)
        {
            var errorDegrees = Quaternion.Angle(current, target);
            var physicalSpeedDegrees = Mathf.Abs(angularSpeedRadiansPerSecond) * Mathf.Rad2Deg;

            // A large attitude change with an almost motionless gyroscope is a sensor-fusion
            // recentering event, not a real movement of the phone.
            if (physicalSpeedDegrees < 3f && errorDegrees > 4f) return current;

            var maximumStep = (physicalSpeedDegrees * 1.45f + 10f) * Mathf.Max(0f, deltaTime);
            return Quaternion.RotateTowards(current, target, Mathf.Max(.02f, maximumStep));
        }

        private static float NormalizeAngle(float degrees) => degrees > 180f ? degrees - 360f : degrees;
    }
}

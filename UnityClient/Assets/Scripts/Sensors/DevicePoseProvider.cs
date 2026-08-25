using UnityEngine;

namespace TransparentEarth.Sensors
{
    public sealed class DevicePoseProvider : MonoBehaviour
    {
        [SerializeField, Range(1f, 30f)] private float smoothing = 14f;
        public float HeadingDegrees { get; private set; }
        public float PitchDegrees { get; private set; }
        public bool SensorAvailable { get; private set; }

        private Quaternion _smoothedRotation = Quaternion.identity;

        private void OnEnable()
        {
            Input.gyro.enabled = SystemInfo.supportsGyroscope;
            Input.compass.enabled = true;
            SensorAvailable = SystemInfo.supportsGyroscope;
        }

        private void Update()
        {
            if (SystemInfo.supportsGyroscope)
            {
                var attitude = Input.gyro.attitude;
                var unityAttitude = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
                var screenCorrection = Quaternion.Euler(90f, 0f, 0f);
                var target = screenCorrection * unityAttitude;
                var blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
                _smoothedRotation = Quaternion.Slerp(_smoothedRotation, target, blend);
                transform.localRotation = _smoothedRotation;
            }
            else
            {
                var yaw = Mathf.Sin(Time.time * .18f) * 12f;
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(0, yaw, 0), .03f);
            }

            HeadingDegrees = Input.compass.enabled ? Input.compass.trueHeading : transform.eulerAngles.y;
            PitchDegrees = NormalizeAngle(transform.eulerAngles.x);
        }

        private static float NormalizeAngle(float degrees) => degrees > 180f ? degrees - 360f : degrees;
    }
}

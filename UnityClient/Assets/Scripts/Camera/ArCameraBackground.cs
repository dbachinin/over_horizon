using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace TransparentEarth.CameraFeed
{
    public sealed class ArCameraBackground : MonoBehaviour
    {
        private Camera _camera;
        private Transform _screen;
        private Material _material;
        private WebCamTexture _feed;
        private int _lastRotation = -1;
        private int _lastWidth;
        private int _lastHeight;

        public bool IsRunning => _feed != null && _feed.isPlaying;

        public void Initialize(Camera sceneCamera)
        {
            _camera = sceneCamera;
            BuildScreen();
            StartCoroutine(StartCamera());
        }

        private void BuildScreen()
        {
            var screenObject = new GameObject("AR Camera Background", typeof(MeshFilter), typeof(MeshRenderer));
            _screen = screenObject.transform;
            _screen.SetParent(_camera.transform, false);
            _screen.localPosition = new Vector3(0, 0, _camera.nearClipPlane + .012f);
            var mesh = new Mesh { name = "Camera Background Quad" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            screenObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            _material = new Material(Shader.Find("TransparentEarth/CameraBackground"));
            screenObject.GetComponent<MeshRenderer>().sharedMaterial = _material;
            FitToCamera();
        }

        private IEnumerator StartCamera()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera)) yield return null;
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) yield break;
#endif
            var devices = WebCamTexture.devices;
            if (devices.Length == 0) yield break;
            var selected = devices[0];
            foreach (var device in devices)
                if (!device.isFrontFacing) { selected = device; break; }
            _feed = new WebCamTexture(selected.name, 1280, 720, 30);
            _material.mainTexture = _feed;
            _feed.Play();
        }

        private void Update()
        {
            if (_feed == null || !_feed.didUpdateThisFrame) return;
            if (_feed.width <= 16 || _feed.height <= 16) return;
            if (_lastRotation != _feed.videoRotationAngle || _lastWidth != _feed.width || _lastHeight != _feed.height)
            {
                _lastRotation = _feed.videoRotationAngle;
                _lastWidth = _feed.width;
                _lastHeight = _feed.height;
                UpdateUvTransform();
            }
            _material.SetFloat("_MirrorY", _feed.videoVerticallyMirrored ? 1f : 0f);
            FitToCamera();
        }

        private void UpdateUvTransform()
        {
            var quarterTurns = Mathf.RoundToInt(_feed.videoRotationAngle / 90f) & 3;
            var rotated = quarterTurns is 1 or 3;
            var sourceAspect = rotated ? _feed.height / (float)_feed.width : _feed.width / (float)_feed.height;
            var screenAspect = Mathf.Max(.01f, _camera.aspect);
            var scale = Vector2.one;
            if (sourceAspect > screenAspect) scale.x = screenAspect / sourceAspect;
            else scale.y = sourceAspect / screenAspect;
            _material.SetFloat("_QuarterTurns", quarterTurns);
            var offset = (Vector2.one - scale) * .5f;
            _material.SetVector("_UvScale", new Vector4(scale.x, scale.y, 0f, 0f));
            _material.SetVector("_UvOffset", new Vector4(offset.x, offset.y, 0f, 0f));
        }

        private void FitToCamera()
        {
            if (_camera == null || _screen == null) return;
            var distance = _screen.localPosition.z;
            var height = 2f * distance * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * .5f);
            _screen.localScale = new Vector3(height * _camera.aspect, height, 1f);
        }

        private void OnDestroy()
        {
            if (_feed != null && _feed.isPlaying) _feed.Stop();
        }
    }
}

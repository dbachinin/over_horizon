using TransparentEarth.Geo;
using TransparentEarth.Sensors;
using UnityEngine;

namespace TransparentEarth.Rendering
{
    public sealed class EarthRenderer : MonoBehaviour
    {
        private const float Radius = 120f;
        private const float EyeHeight = .12f;
        private const float HorizonDistance = 42f;
        private static readonly Vector3 EarthCenter = new(0, -Radius - EyeHeight, 0);
        private Material _hazeMaterial;
        private Material _earthMaterial;
        private Material _interiorMaterial;
        private Camera _observerCamera;
        private LocationProvider _location;
        private Transform _globeRoot;
        private Transform _horizonRoot;
        private Transform _antipodeFlag;
        private GeoBoundaryLayer _boundaries;
        private float _smoothedHaze = 1f;
        private Vector2 _lastPointer;
        private bool _dragging;
        private bool _returningToReal;
        private bool _interactionEnabled = true;
        private float _manualLookPitch;

        public bool GridVisible { get; private set; } = true;
        public bool ContinentsVisible { get; private set; } = true;
        public bool CountriesVisible { get; private set; }
        public bool ReferencesVisible { get; private set; } = true;
        public Quaternion GeographicRotation => _globeRoot == null ? Quaternion.identity : _globeRoot.localRotation;
        public bool IsManuallyOriented => Quaternion.Angle(_globeRoot == null ? Quaternion.identity : _globeRoot.localRotation,
            Quaternion.identity) > .2f || Mathf.Abs(_manualLookPitch) > .2f;
        public float ManualLookPitchDegrees => _manualLookPitch;
        public Vector3 AntipodeWorldPoint => _antipodeFlag == null
            ? transform.position
            : _antipodeFlag.position;
        public Vector3 HorizonWorldPoint => _horizonRoot == null
            ? Vector3.forward * HorizonDistance
            : _horizonRoot.TransformPoint(new Vector3(0, HorizonLocalY, HorizonDistance));

        private static float HorizonLocalY
        {
            get
            {
                var dip = Mathf.Acos(Radius / (Radius + EyeHeight));
                return -Mathf.Tan(dip) * HorizonDistance;
            }
        }

        public void Build(Camera observerCamera, LocationProvider location)
        {
            _observerCamera = observerCamera;
            _location = location;
            _globeRoot = new GameObject("Rotatable Globe").transform;
            _globeRoot.SetParent(transform, false);
            _globeRoot.localPosition = EarthCenter;
            _horizonRoot = new GameObject("Local Horizon Frame").transform;
            _horizonRoot.SetParent(transform, false);
            var mesh = CreateSphereMesh(longitudeSegments: 128, latitudeSegments: 80);

            var earth = CreateMeshObject("Earth Surface", mesh, _globeRoot);
            earth.transform.localPosition = Vector3.zero;
            earth.transform.localScale = Vector3.one * Radius;
            _earthMaterial = new Material(Shader.Find("TransparentEarth/AtmosphericGrid"))
            {
                name = "Earth Surface Runtime Material"
            };
            _earthMaterial.SetColor("_LandColor", new Color(.004f, .006f, .006f, .985f));
            _earthMaterial.SetColor("_OceanColor", new Color(.005f, .105f, .090f, .78f));
            _earthMaterial.SetColor("_DeepColor", new Color(.001f, .032f, .034f, .82f));
            _earthMaterial.SetColor("_CausticColor", new Color(.34f, .90f, .70f, 1f));
            _earthMaterial.SetColor("_GridColor", new Color(.92f, .77f, .42f, .50f));
            _earthMaterial.SetColor("_PulseColor", new Color(1f, .76f, .28f, 1f));
            _earthMaterial.SetColor("_HazeColor", new Color(.86f, .72f, .46f, 1f));
            earth.GetComponent<MeshRenderer>().sharedMaterial = _earthMaterial;

            var interior = CreateMeshObject("Golden Earth Interior", mesh, _globeRoot);
            interior.transform.localPosition = Vector3.zero;
            interior.transform.localScale = Vector3.one * (Radius - .28f);
            _interiorMaterial = new Material(Shader.Find("TransparentEarth/GoldenInterior"))
            {
                name = "Golden Interior Runtime Material"
            };
            _interiorMaterial.SetColor("_InteriorColor", new Color(.92f, .65f, .20f, .24f));
            _interiorMaterial.SetColor("_PulseColor", new Color(1f, .86f, .48f, 1f));
            interior.GetComponent<MeshRenderer>().sharedMaterial = _interiorMaterial;

            var haze = CreateMeshObject("Flat Horizon Haze", CreateHorizonBandMesh(), _horizonRoot);
            _hazeMaterial = new Material(Shader.Find("TransparentEarth/HorizonHaze"))
            {
                name = "Horizon Haze Runtime Material"
            };
            _hazeMaterial.SetColor("_HazeColor", new Color(.91f, .78f, .54f, .72f));
            haze.GetComponent<MeshRenderer>().sharedMaterial = _hazeMaterial;

            _boundaries = gameObject.AddComponent<GeoBoundaryLayer>();
            _boundaries.Initialize(_globeRoot, location, Radius + .055f, _earthMaterial, _interiorMaterial);

            BuildHorizon();
            BuildReticle();
        }

        public void SetInteractionEnabled(bool enabled) => _interactionEnabled = enabled;

        public void SetGridVisible(bool visible)
        {
            GridVisible = visible;
            if (_earthMaterial != null) _earthMaterial.SetFloat("_GridOpacity", visible ? 1f : 0f);
        }

        public void SetContinentsVisible(bool visible)
        {
            ContinentsVisible = visible;
            _boundaries?.SetCoastlinesVisible(visible);
        }

        public void SetCountriesVisible(bool visible)
        {
            CountriesVisible = visible;
            _boundaries?.SetCountriesVisible(visible);
        }

        public void SetReferencesVisible(bool visible)
        {
            ReferencesVisible = visible;
            if (_earthMaterial != null) _earthMaterial.SetFloat("_ReferenceOpacity", visible ? 1f : 0f);
        }

        public void RestoreRealOrientation()
        {
            _returningToReal = true;
            _dragging = false;
        }

        public float ScanPulseAt(float centralAngleDegrees)
        {
            var phase = CurrentScanPhase;
            if (phase > 1f) return 0f;
            var distance = Mathf.Clamp01(centralAngleDegrees / 180f);
            var width = Mathf.Lerp(.055f, .009f, distance);
            return 1f - Mathf.SmoothStep(width * .16f, width, Mathf.Abs(distance - phase));
        }

        public float MarkerRevealAt(float centralAngleDegrees)
        {
            var phase = CurrentScanPhase;
            if (phase > 1f) return 1f;
            var distance = Mathf.Clamp01(centralAngleDegrees / 180f);
            var drawWidth = Mathf.Lerp(.045f, .012f, distance);
            return Mathf.SmoothStep(distance, distance + drawWidth, phase);
        }

        public Vector3 GeographicSurfacePoint(GeoPoint point)
        {
            if (_globeRoot == null || _location == null) return transform.position;
            var normal = GeoMath.SurfaceNormalEnu(_location.Current, point);
            return _globeRoot.TransformPoint(normal * (Radius + .085f));
        }

        private static float CurrentScanPhase => Mathf.Repeat(Time.unscaledTime * .30f, 1.18f);

        public Transform CreateFlag(GeoProjection projection, string label, bool isAntipode)
        {
            var centralAngle = projection.CentralAngleDegrees * Mathf.Deg2Rad;
            var bearing = projection.BearingDegrees * Mathf.Deg2Rad;
            var normal = new Vector3(
                Mathf.Sin((float)centralAngle) * Mathf.Sin((float)bearing),
                Mathf.Cos((float)centralAngle),
                Mathf.Sin((float)centralAngle) * Mathf.Cos((float)bearing)).normalized;
            return CreateFlagAtNormal(normal, label, isAntipode);
        }

        public Transform CreateAntipodeFlag()
        {
            if (_antipodeFlag != null) return _antipodeFlag;
            _antipodeFlag = CreateFlagAtNormal(Vector3.down, "Exact antipode", true);
            return _antipodeFlag;
        }

        private Transform CreateFlagAtNormal(Vector3 normal, string label, bool isAntipode)
        {
            var root = new GameObject("Flag · " + label).transform;
            root.SetParent(_globeRoot, false);
            root.localPosition = normal * (Radius + .08f);
            root.localRotation = Quaternion.FromToRotation(Vector3.up, normal);

            var color = isAntipode ? TransparentEarthStyle.Signal : TransparentEarthStyle.Mint;
            var pole = new GameObject("Pole").AddComponent<LineRenderer>();
            pole.transform.SetParent(root, false);
            pole.useWorldSpace = false;
            pole.positionCount = 2;
            pole.SetPosition(0, Vector3.zero);
            pole.SetPosition(1, Vector3.up * (isAntipode ? .7f : .42f));
            pole.startWidth = pole.endWidth = isAntipode ? .035f : .022f;
            pole.sharedMaterial = NewLineMaterial(color);

            var meshObject = new GameObject("Pennant", typeof(MeshFilter), typeof(MeshRenderer));
            meshObject.transform.SetParent(root, false);
            var height = isAntipode ? .68f : .40f;
            var width = isAntipode ? .38f : .24f;
            var mesh = new Mesh { name = "Flag Pennant" };
            mesh.vertices = new[] { new Vector3(0, height, 0), new Vector3(width, height - .1f, 0), new Vector3(0, height - .22f, 0) };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            meshObject.GetComponent<MeshRenderer>().sharedMaterial = NewLineMaterial(color);
            if (isAntipode) CreateAntipodeTarget(root, color);
            return root;
        }

        private void Update()
        {
            if (_observerCamera == null || _hazeMaterial == null) return;
            UpdateGlobeInteraction();
            if (_returningToReal)
            {
                _globeRoot.localRotation = Quaternion.Slerp(_globeRoot.localRotation, Quaternion.identity,
                    1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                _manualLookPitch = Mathf.Lerp(_manualLookPitch, 0f, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                if (Quaternion.Angle(_globeRoot.localRotation, Quaternion.identity) < .08f && Mathf.Abs(_manualLookPitch) < .04f)
                {
                    _globeRoot.localRotation = Quaternion.identity;
                    _manualLookPitch = 0f;
                    _returningToReal = false;
                }
            }
            _observerCamera.transform.localRotation = Quaternion.Euler(_manualLookPitch, 0f, 0f);
            var depthLook = Mathf.InverseLerp(-.15f, -.82f, _observerCamera.transform.forward.y);
            var targetHaze = 1f - Mathf.SmoothStep(0f, 1f, depthLook);
            _smoothedHaze = Mathf.Lerp(_smoothedHaze, targetHaze, 1f - Mathf.Exp(-5f * Time.deltaTime));
            _hazeMaterial.SetFloat("_HazeAmount", _smoothedHaze);
            _earthMaterial.SetFloat("_HazeAmount", _smoothedHaze);
            _interiorMaterial.SetFloat("_HazeAmount", _smoothedHaze);
            if (_antipodeFlag != null)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.2f) * .055f;
                _antipodeFlag.localScale = Vector3.one * pulse;
            }
        }

        private void LateUpdate()
        {
            if (_observerCamera == null || _horizonRoot == null) return;
            var flatForward = Vector3.ProjectOnPlane(_observerCamera.transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude > .0001f)
                _horizonRoot.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        private void UpdateGlobeInteraction()
        {
            if (!_interactionEnabled || Input.touchCount == 0)
            {
                _dragging = false;
                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _lastPointer = touch.position;
                _dragging = touch.position.y > Screen.height * .18f && touch.position.y < Screen.height * .82f;
                return;
            }
            if (!_dragging) return;
            if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) { _dragging = false; return; }
            var delta = touch.position - _lastPointer;
            _lastPointer = touch.position;
            if (delta.sqrMagnitude < 2f) return;
            _returningToReal = false;
            var yaw = Quaternion.AngleAxis(-delta.x * .085f, Vector3.up);
            _globeRoot.localRotation = yaw * _globeRoot.localRotation;
            _manualLookPitch = Mathf.Clamp(_manualLookPitch + delta.y * .055f, -15f, 70f);
        }

        private static GameObject CreateMeshObject(string name, Mesh mesh, Transform parent)
        {
            var result = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            result.transform.SetParent(parent, false);
            result.GetComponent<MeshFilter>().sharedMesh = mesh;
            return result;
        }

        private static Mesh CreateSphereMesh(int longitudeSegments, int latitudeSegments)
        {
            var vertices = new Vector3[(longitudeSegments + 1) * (latitudeSegments + 1)];
            var normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[longitudeSegments * latitudeSegments * 6];

            var vertex = 0;
            for (var latitude = 0; latitude <= latitudeSegments; latitude++)
            {
                var v = latitude / (float)latitudeSegments;
                var phi = v * Mathf.PI;
                var sinPhi = Mathf.Sin(phi);
                var cosPhi = Mathf.Cos(phi);
                for (var longitude = 0; longitude <= longitudeSegments; longitude++)
                {
                    var u = longitude / (float)longitudeSegments;
                    var theta = u * Mathf.PI * 2f;
                    var normal = new Vector3(sinPhi * Mathf.Cos(theta), cosPhi, sinPhi * Mathf.Sin(theta));
                    vertices[vertex] = normal;
                    normals[vertex] = normal;
                    uv[vertex] = new Vector2(u, 1f - v);
                    vertex++;
                }
            }

            var triangle = 0;
            for (var latitude = 0; latitude < latitudeSegments; latitude++)
            {
                for (var longitude = 0; longitude < longitudeSegments; longitude++)
                {
                    var current = latitude * (longitudeSegments + 1) + longitude;
                    var next = current + longitudeSegments + 1;
                    triangles[triangle++] = current;
                    triangles[triangle++] = next;
                    triangles[triangle++] = current + 1;
                    triangles[triangle++] = current + 1;
                    triangles[triangle++] = next;
                    triangles[triangle++] = next + 1;
                }
            }

            var mesh = new Mesh { name = "Smooth Earth Sphere 128x80" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHorizonBandMesh()
        {
            const float halfWidth = 90f;
            const float halfHeight = 3.6f;
            var centerY = HorizonLocalY;
            var mesh = new Mesh { name = "Flat Atmospheric Horizon Band" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, centerY - halfHeight, HorizonDistance),
                new Vector3(halfWidth, centerY - halfHeight, HorizonDistance),
                new Vector3(halfWidth, centerY + halfHeight, HorizonDistance),
                new Vector3(-halfWidth, centerY + halfHeight, HorizonDistance)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildHorizon()
        {
            var horizon = new GameObject("Physical Horizon").AddComponent<LineRenderer>();
            horizon.transform.SetParent(_horizonRoot, false);
            horizon.useWorldSpace = false;
            horizon.positionCount = 2;
            horizon.SetPosition(0, new Vector3(-90, HorizonLocalY, HorizonDistance));
            horizon.SetPosition(1, new Vector3(90, HorizonLocalY, HorizonDistance));
            horizon.startWidth = horizon.endWidth = .012f;
            horizon.material = NewLineMaterial(TransparentEarthStyle.Mint * new Color(1, 1, 1, .36f));
        }

        private void BuildReticle()
        {
            var ring = new GameObject("Reticle").AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 72;
            const float distance = 5f;
            const float radius = .18f;
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, distance));
            }
            ring.startWidth = ring.endWidth = .006f;
            ring.material = NewLineMaterial(TransparentEarthStyle.Mint);
        }

        private static void CreateAntipodeTarget(Transform parent, Color color)
        {
            var ring = new GameObject("Antipode Target Ring").AddComponent<LineRenderer>();
            ring.transform.SetParent(parent, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = 48;
            const float radius = .34f;
            for (var i = 0; i < ring.positionCount; i++)
            {
                var angle = i / (float)ring.positionCount * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .012f, Mathf.Sin(angle) * radius));
            }
            ring.startWidth = ring.endWidth = .018f;
            ring.sharedMaterial = NewLineMaterial(color);

            CreateTargetAxis("Antipode Target East West", parent,
                new Vector3(-.48f, .014f, 0f), new Vector3(.48f, .014f, 0f), color);
            CreateTargetAxis("Antipode Target North South", parent,
                new Vector3(0f, .014f, -.48f), new Vector3(0f, .014f, .48f), color);
        }

        private static void CreateTargetAxis(string name, Transform parent, Vector3 start, Vector3 end, Color color)
        {
            var axis = new GameObject(name).AddComponent<LineRenderer>();
            axis.transform.SetParent(parent, false);
            axis.useWorldSpace = false;
            axis.positionCount = 2;
            axis.SetPosition(0, start);
            axis.SetPosition(1, end);
            axis.startWidth = axis.endWidth = .012f;
            axis.sharedMaterial = NewLineMaterial(color);
        }

        private static Material NewLineMaterial(Color color) =>
            new Material(Shader.Find("Sprites/Default")) { color = color };
    }
}

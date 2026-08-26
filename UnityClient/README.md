# OverHorizon — Unity client

Unity 6.3 LTS (`6000.3.22f1`) Android client for the OverHorizon spatial viewer.

## Open and run

1. In Unity Hub, install Unity 6.3 LTS with **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK**.
2. Open this `UnityClient` directory as a project.
3. The editor configurator creates `Assets/Scenes/Main.unity` and applies Android settings automatically.
4. Press Play. In Editor, orientation has a subtle demo drift; on Android it uses the gyroscope and compass.
5. Use **File → Build Profiles → Android** to build an APK or AAB.

The bottom navigation contains **Overview**, **Antipode**, and **Places**. The Places screen performs
explicit user-triggered settlement searches through OpenStreetMap Nominatim. Added places are cached
locally, restored on the next launch, and rendered through the same streamed marker system as built-in cities.

No scene authoring is required: `TransparentEarthBootstrap` creates the camera, translucent Earth, horizon, targets, GPS provider, sensor provider, and instrument UI at runtime.

## Rendering and coordinates

- Earth geometry uses a spherical WGS84 mean-radius approximation (`6371.0088 km`).
- Geographic points are converted to ECEF and then to the observer's local ENU frame.
- Unity axes are `x = east`, `y = up`, `z = north`.
- The sphere uses a lightweight transparent unlit shader suitable for mobile GPUs.
- Far-side labels stay visible because markers are projected independently from the transparent Earth depth.

## Tests

Open **Window → General → Test Runner → EditMode** and run `TransparentEarth.Tests`.

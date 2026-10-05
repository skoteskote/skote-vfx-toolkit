# Skote VFX Toolkit

Reusable VFX Graph subgraphs and helper components.

## Contents

### Subgraphs (`VFX/`)

| Subgraph | |
|---|---|
| `Vortex.vfxoperator` | Vortex force field |
| `SignedDistanceFieldAttractor.vfxoperator` | Attract particles to an SDF surface |
| `ColorComparison.vfxoperator` | Compare colors (e.g. for keying / masking) |

`Shaders/EdgeDither.shadergraph` is a URP Shader Graph used as a VFX output.

### Components

Namespace `Skote.Vfx`:

- `VFXManager`: set float/Vector2/bool properties and send events across groups of VisualEffects.
- `VFXVector2Controller`: drive Vector2 properties from single float inputs.
- `VFXPlayRate`: set an int `Rate` property on start.
- `CameraFrustumColliders`: feeds camera-frustum planes to a VFX Graph for collisions.

Namespace `Skote.Vfx.Audio`:

- `SharedMicrophone`, `MicrophoneToAudioSource`, `MicrophoneDecibelValues`, `AudioSourceDecibelValues`: microphone and AudioSource level analysis (band-filtered dB).
- `VFXAudioDecibelBinder`, `VFXAudioEQBinder`, `VFXAudioPitchBinder`: VFX property binders.
- `AudioSourcePitchValues`, `MicrophonePitchValues`: pitch detection using the bundled RAPT detector (`Skote.Vfx.Audio.PitchDetection`, separate assembly with unsafe code).

### Optional: Mesh-to-SDF (`Skote.Vfx.Sdf`)

Only compiles when [`com.unity.demoteam.mesh-to-sdf`](https://github.com/Unity-Technologies/com.unity.demoteam.mesh-to-sdf) is installed. Git packages can't be declared as dependencies, so install it yourself:

```json
"com.unity.demoteam.mesh-to-sdf": "https://github.com/Unity-Technologies/com.unity.demoteam.mesh-to-sdf.git",
"com.whinarn.unitymeshsimplifier": "https://github.com/Whinarn/UnityMeshSimplifier.git"
```

- `SDFTextureHelper`: creates the SDF RenderTexture at runtime and assigns it to `SDFTexture` and a VFX Graph.
- `SDFUpdateManager`: staggers updates across many `MeshToSDF` components.
- `AnimatorManager`: synchronised control of several animators plus switching SDF groups on a VFX Graph.
- `SDFMeshSimplifier`: simplifies a skinned mesh at runtime for cheaper SDF generation (also needs UnityMeshSimplifier).

## Install

Add to `Packages/manifest.json` (pin a tag):

```json
"com.skote.vfx-toolkit": "https://github.com/skoteskote/skote-vfx-toolkit.git#v0.1.0"
```

## Editing from any project

Clone the repo into the project's `Packages/` folder. Unity uses the embedded copy in place of the manifest entry, so you can edit, commit and push from there:

```
git clone git@github.com:skoteskote/skote-vfx-toolkit.git Packages/com.skote.vfx-toolkit
```

Add `Packages/com.skote.vfx-toolkit` to the project's Plastic `ignore.conf` (or `.gitignore`). When you're done, tag a release and bump the `#tag` in the manifest.

## License

MIT. See [LICENSE.md](LICENSE.md).

`Runtime/Audio/PitchDetection` is a C# port of RAPT by Outloud Oy (2017), derived from the ESPS toolkit (c) 2002 Microsoft Corp. It is distributed under its original BSD-style licence, which is kept in each file header.

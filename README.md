# SAO Clothing Dissolve

Unity editor addon for VRChat avatars that prepares a SAO-inspired clothing dissolve effect without Udon, world scripts, or custom runtime C# on the uploaded avatar.

Target Unity version: `2022.3.22f1`

## Avatar-Only Scope

This package is for avatar projects using the VRChat Avatars SDK. It does not use Udon, does not create world behaviours, and does not require scripts to run after upload. The editor tool bakes ordinary avatar assets: generated child objects, meshes, materials, shaders, and animation clips for the avatar FX controller.

## Install in Unity

For local development, copy or keep this repository and add the package folder through Unity:

1. Open a VRChat avatar project.
2. Open `Window > Package Manager`.
3. Choose `+ > Add package from disk...`.
4. Select `Packages/com.juanz.sao-clothing-dissolve/package.json`.
5. Open `Tools > SAO Clothing Dissolve`.

## Use as a VCC/VPM repository

This repository includes a `vpm.json` listing. Before publishing:

1. Replace `YOUR_GITHUB_USER` and `YOUR_REPO` in `vpm.json`.
2. Create a zip release containing the package contents.
3. Update the release URL and SHA-256 zip checksum in `vpm.json`.
4. Host `vpm.json` through GitHub Pages or another public URL.
5. Add that URL in VRChat Creator Companion as a user repository.

VPM packages use Unity-compatible `package.json` manifests, and VPM repository listings expose package versions in a JSON listing read by Creator Companion. See the official VCC docs for the current package and repository formats:

- [VPM packages](https://vcc.docs.vrchat.com/vpm/packages/)
- [VPM repositories](https://vcc.docs.vrchat.com/vpm/repos/)

## Workflow

1. Select a clothing `SkinnedMeshRenderer`.
2. Create or assign an effect config asset.
3. Adjust glow, flash, fragment, and timing options.
4. Click `Validate` to catch missing references.
5. Click `Generate / Update`.
6. Use the generated animation clips in your avatar FX controller or in VRCFury.

Generated assets are placed under `Assets/SAOClothingDissolve/Generated/<RendererName>/` by default. The generator is non-destructive: it does not modify the original mesh asset.

## VRChat integration notes

The package generates standard avatar-safe Unity assets: materials, meshes, animation clips, and a generated helper root under the avatar. Uploaded avatars do not need arbitrary C# runtime scripts.

For a clothing toggle:

- On hide: play the generated hide clip, then disable the original clothing renderer at the end.
- On show: enable the clothing renderer at the start, then play the generated show clip.
- Keep the same bool/parameter names as your existing avatar controller when possible.

VRCFury can be used to wire these clips into toggles, but it is intentionally not a package dependency.

## Limitations

- Fragment meshes are generated in the editor by sampling triangles from the baked skinned mesh. This is practical and reusable, but it is not a perfect physical fracture of the garment.
- Skinned clothing is baked once at generation time. Regenerate after changing the mesh, blendshape pose, or outfit proportions.
- VRChat performance depends heavily on generated fragment renderers, material count, and avatar complexity. Keep fragment counts modest.


# Installation and Test Procedure

## Local package test

1. Open a Unity `2022.3.22f1` VRChat avatar project.
2. Add this package from disk through Package Manager.
3. Open `Tools > SAO Clothing Dissolve`.
4. Create a config asset.
5. Assign a clothing `SkinnedMeshRenderer`.
6. Click `Validate`.
7. Click `Generate / Update`.
8. Confirm that Unity created:
   - a generated root under the avatar root,
   - fragment mesh assets,
   - generated materials,
   - hide and show animation clips.
9. Click `Preview In Editor`.
10. Add the generated clips to your avatar FX controller or a VRCFury avatar toggle.

This is not a world/Udon workflow. The generated clips should be driven by avatar parameters in the avatar FX controller.

## Toggle timing

Use a bool such as `JacketVisible`.

Hide path:

- Enter dissolve state when `JacketVisible` becomes false.
- Play the generated hide clip.
- Let the clip disable the original renderer at the end.

Show path:

- Enter appear state when `JacketVisible` becomes true.
- Play the generated show clip.
- The original renderer is enabled immediately and the dissolve property returns to visible.

## Performance guidance

Start with 24 to 48 fragments. Increase only after testing avatar performance rank and visual quality in the VRChat SDK control panel.


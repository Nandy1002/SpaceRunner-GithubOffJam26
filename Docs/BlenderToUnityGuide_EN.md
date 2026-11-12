# Blender to Unity — Environment Art Handoff

For the Space Runner tutorial map. The project currently uses **Unity 6000.6.8f1** and **Universal Render Pipeline (URP) 17.6.0**. This guide covers static environment meshes and textures; character rigs and animation need a separate export setup.

Use one small wall as a trial asset before processing the whole map. Agree on its size, origin, orientation and material appearance in Unity first.

## 1. Install FBX Exporter in Unity

This package is needed to export the existing Unity greybox to Blender. Unity can already import FBX files without this package.

1. Exit Play mode and save the scene.
2. Open **Window → Package Management → Package Manager**. Some editor versions place **Package Manager** directly under **Window**.
3. Select **Unity Registry**, search for **FBX Exporter**, select the package and click **Install**.
4. Alternatively, open the **+ / Install** menu in the Package Manager toolbar and choose **Install package by name**.
5. Enter `com.unity.formats.fbx` in **Name**. Leave **Version** empty to let Unity select a compatible released version, then click **Install**.
6. Wait for downloading, importing and script compilation to finish. Select a scene object and check for **GameObject → Export To FBX…**.

Installation needs access to Unity's package registry. If searching returns nothing, check that the list is set to **Unity Registry**, or use the package-name method above. [Unity package installation reference](https://docs.unity3d.com/cn/6000.0/Manual/upm-ui-quick.html), [FBX Exporter package identifier](https://docs.unity3d.com/ja/current/Manual/com.unity.formats.fbx.html).

## 2. Export the Unity greybox for reference

1. Open the **TutorialGreybox** scene and save it.
2. In **Hierarchy**, expand **TutorialGeometry**. Select one numbered section to work on, or select **TutorialGeometry** for the whole layout.
3. Choose **GameObject → Export To FBX…**.
4. Set **Export Format** to **Binary** and **Include** to **Model(s) Only**.
5. For a section that must return to the same location, use **World Absolute** under **Object(s) Position**. Use the same coordinate convention for every section.
6. Export a reference file, such as `Tutorial_03_Reference.fbx`.
7. In Blender, use **File → Import → FBX (.fbx)**.

Keep the imported reference in a separate Blender collection. Do not move or rescale it while building replacement art. Export new artwork separately from the reference meshes.

FBX carries supported geometry, transforms and material data. Unity gameplay scripts, layers and collider behaviour remain in the Unity project. The Unity FBX Exporter does not provide automatic Blender material conversion or live synchronization. [Unity FBX export options](https://docs.unity3d.com/ja/Packages/com.unity.formats.fbx%404.1/manual/exporting.html).

## 3. Prepare the model in Blender

- Work at real-world scale: use **Metric**, with **Unit Scale = 1**, and verify dimensions against the imported reference.
- Keep reusable walls, floors and props as separate objects with clear names, such as `WallRun_Left_Art` or `Ramp_Return_Art`.
- For static meshes, apply **Rotation & Scale** using **Ctrl+A** after confirming the intended orientation. Keep the agreed origin and section placement.
- Check normals: visible surfaces should face outward. Inspect thin walls and ceiling undersides from the player's side.
- UV unwrap surfaces that use image textures. Inspect a checker texture for stretching before painting.
- Keep the gameplay shape: landing sizes, gaps, ramp slopes and doorway clearance should continue to match the reference.

For this map, blue stripes identify the three dedicated wall-run surfaces. Preserve their identities in object names even when replacing the placeholder blue graphics with final art.

## 4. Save textures as external image files

Save painted images through Blender's Image Editor using **Image → Save As**. A saved `.blend` file alone is not the texture handoff.

Deliver the maps the asset actually needs. PNG and TGA are suitable image formats; use a format with an alpha channel for packed metallic/smoothness data.

| File example | Purpose |
| --- | --- |
| `Wall_BaseColor.png` | Surface colour, without baked directional lighting. |
| `Wall_Normal.png` | Tangent-space surface detail. |
| `Wall_Metallic.png` | Metallic/non-metallic mask. |
| `Wall_Roughness.png` | Surface roughness; requires conversion for URP Lit. |
| `Wall_AO.png` | Optional ambient occlusion. |
| `Wall_Emission.png` | Optional emissive colour. |

If the appearance comes from procedural nodes such as Noise Texture or ColorRamp, bake the required channels into image textures. Blender's shader-node network does not become a matching Unity shader automatically.

For a simple base-colour bake in Cycles: UV unwrap the mesh, create a destination image, add and select an **Image Texture** node using that image in each relevant material, choose **Diffuse** baking with only **Color** enabled, bake, and save the resulting image. Bake other channels separately; use **Tangent** space for a normal-map bake. [Blender texture baking reference](https://docs.blender.org/manual/en/5.0/render/cycles/baking.html).

## 5. Export the finished mesh from Blender

For a static environment asset, use these starting settings and verify the result with the trial wall:

1. Select only the finished artwork and any required organizational empties.
2. Choose **File → Export → FBX (.fbx)**.
3. Enable **Selected Objects**. Include **Mesh**, plus **Empty** if your hierarchy needs it. Omit cameras and lights for this handoff.
4. Use **Scale = 1**, **Forward = -Z**, and **Up = Y** as the initial axis settings.
5. Enable **Apply Modifiers** if the exported mesh should include the evaluated modifier result. Disable **Bake Animation** for static scenery.
6. Export the FBX alongside the separate texture folder.

Blender versions differ in FBX options. Confirm the imported dimensions and orientation in Unity before batching exports; do not compensate for an unexplained 100× scale mismatch by manually resizing each scene instance. [Blender FBX options](https://docs.blender.org/manual/en/4.2/addons/import_export/scene_fbx.html).

Example delivery:

```text
Tutorial_03_Art/
  Tutorial_03_Art.fbx
  Textures/
    Wall_BaseColor.png
    Wall_Normal.png
    Wall_Metallic.png
    Wall_Roughness.png
  Source/
    Tutorial_03_Art.blend
  Readme.txt
```

In `Readme.txt`, record the export settings, section origin, texture naming and any packed-channel layout. The `.blend` file is the editable source; the FBX and image files are the Unity import assets.

## 6. Import and create materials in Unity

1. Copy the FBX and textures into the team's agreed folders under **Assets/Graphics**.
2. Select the FBX and inspect its **Model** import settings. Confirm the result against the unchanged greybox or a known-size Unity cube. For a static environment mesh, set **Animation Type** to **None** under **Rig**, where available.
3. In Project, right-click and choose **Create → Material**. Name it after the surface, for example `Wall_Material`.
4. Set its shader to **Universal Render Pipeline/Lit** and use the **Metallic** workflow.
5. Configure the textures and material inputs:

| Texture | Import setting | URP Lit input |
| --- | --- | --- |
| Base colour | Default texture; sRGB on | **Base Map**; set its tint to white. |
| Normal | **Texture Type: Normal map**; Apply | **Normal Map**. |
| Metallic/smoothness packed map | Default texture; sRGB off; preserve alpha | **Metallic Map**; choose **Metallic Alpha** for smoothness. |
| AO | Default texture; sRGB off | **Occlusion Map**; AO data belongs in green. |
| Emission colour | Default texture; sRGB on for a standard colour image | Enable **Emission**, assign the map and adjust intensity. |

URP Lit uses **Smoothness**, while Blender commonly uses **Roughness**. Convert with `Smoothness = 1 - Roughness`. For a packed metallic/smoothness texture, store metallic in **red** and smoothness in **alpha**. Set the material's smoothness multiplier to **1** initially. A separate roughness image cannot be assigned directly to a roughness slot in standard URP Lit.

For a first visual check, use the Base Map and Normal Map, then adjust metallic and smoothness with sliders. Match lighting separately. [URP Lit material reference](https://docs.unity.cn/Manual/urp/lit-shader.html).

6. Assign the new material in the model renderer's **Materials** list. For several material slots, map each slot to the appropriate Unity material.

## 7. Replace the greybox visuals without breaking gameplay

For this project, keep the existing gameplay colliders and replace the visible art around them:

1. Place the imported art in the appropriate numbered **TutorialGeometry** section and align it with the reference.
2. Use a unit-scale art parent. Existing greybox cubes are often scaled; parenting a finished mesh directly under one can stretch it again.
3. Disable the replaced cube's **Mesh Renderer** only. Keep its GameObject and collider active.
4. Keep dedicated wall-run colliders on **Wall (7)**. Keep floor, gravity-wall and ceiling-walking colliders on **Ground (6)**.
5. Avoid duplicate colliders on the visual mesh while the original collision shape remains. Check that the art actually matches the physical surface.
6. Preserve player/watch/combat objects and enemy prefab bindings. The FBX supplies their visual surroundings, not their behaviour.

These are the project's current integration conventions. If artwork needs a different collision shape, make that a deliberate gameplay change and retest the affected section.

## 8. Check the handoff in Unity

- Model size and orientation match the reference, including ceiling undersides.
- Textures are present and UVs are correct; materials use URP-compatible shaders.
- Walking, all jumps and wall runs still work with the final surfaces.
- Gravity walking reaches the wall, ceiling and upper exit.
- All five recovery ramps and their return doorways remain clear.
- Tower barrels and firing lanes are visible and unobstructed where intended.
- Gameplay scripts, collider layers and prefab bindings are preserved.

If a model is pink, inspect its shader compatibility. If it is untextured, check material slots and texture assignments. If highlights look inverted or unexpectedly glossy, inspect normal-map import and roughness-to-smoothness conversion before changing the lighting.

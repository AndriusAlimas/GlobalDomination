England Soldier unit assets

Runtime Resources paths:
  Model: Battle/Countries/England/Units/Soldier/Model/Soldier
  Idle:  Battle/Countries/England/Units/Soldier/Animations/Idle

Folder roles:
  Model/       Soldier.fbx with skin/rig. The visible body must import as a SkinnedMeshRenderer.
  Animations/  Animation-only FBXs for this unit (Idle.fbx first).
  Materials/   Extracted Unity materials from Soldier.fbx.
  Textures/    Source texture maps used by the materials.

After replacing Soldier.fbx or Idle.fbx:
  1. Run Global Domination > Battle > Apply England FBX rig (Soldier + Idle).
  2. Confirm Soldier.fbx imports with a valid Avatar and SkinnedMeshRenderer.
  3. Confirm Idle.fbx copies the Soldier avatar and Loop Time is enabled.

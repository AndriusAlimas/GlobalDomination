Staging battle unit visuals (Resources)

Current default (no prefab wiring needed):
  - Soldiers are loaded at runtime from Resources (England example):
      Model: Battle/Countries/England/Units/Soldier/Model/Soldier
      Idle:  Battle/Countries/England/Units/Soldier/Animations/Idle   (AnimationClip from Idle.fbx)
  - This avoids prefab GUID wiring while iterating on art.

To update the soldier:
  - Put your model FBX under:
      Assets/Resources/Battle/Countries/England/Units/Soldier/Model/Soldier.fbx
  - Put your idle animation FBX under:
      Assets/Resources/Battle/Countries/England/Units/Soldier/Animations/Idle.fbx
  - Put extracted Unity materials and source textures under:
      Assets/Resources/Battle/Countries/England/Units/Soldier/Materials/
      Assets/Resources/Battle/Countries/England/Units/Soldier/Textures/
  - If the Idle clip doesn’t loop, enable Loop Time on the imported clip in the FBX import settings.

If you see NOTHING (no capsule, no soldier): check the Unity Console when opening staging battle.
  • "[StagingBattle] Expected N attacker(s) but none were spawned" — grid had bad/null unit entries; place units again and Confirm.
  • Battle closes in one frame if zero attackers spawn — you will not see ground/units until at least one attacker is created.
  • Tiny/huge FBX: code now rescales SoldierModel toward ~1.65m height when bounds look wrong.

Default paths (must be under Assets/Resources/):
  Battle/StagingBattleAttacker.prefab   — optional attacker prefab (not used by default while UK soldier runtime-load is enabled)
  Battle/StagingBattleDefender.prefab   — defender skirmishers (falls back to attacker prefab)

Per building / unit type (optional):
  Battle/Attackers/<BuildingType>.prefab
  Example: Battle/Attackers/Barraka.prefab  (enum name must match exactly)

Optional prefab root checklist:
  - Root: CapsuleCollider (or any Collider) + Rigidbody optional (added at runtime if missing)
  - Child: animated soldier with Animator + walk loop (in-place walk matches kinematic march)
  - Do NOT add StagingBattleUnit in the prefab — it is added at runtime on the root.

Animator: optional prefabs support common bools (Walk, IsWalking, Moving) or floats (Speed, MoveSpeed), or a single default state with no parameters (Animator.speed toggles while moving).

Import tip for the England runtime soldier: Soldier.fbx and Idle.fbx use Unity Generic rig import; Idle copies the Soldier avatar and has Loop Time enabled.

Camera: StagingBattleRtsCamera on the battle camera — WASD + middle-mouse drag pan, mouse wheel zoom, Q/E orbit. LMB/RMB stay unit select / shoot.

Soldier orientation: SoldierModel mount uses identity rotation (Y-up FBX). If your export lies on its side, fix the FBX import rotation in Unity or rotate the Model child — do not assume -90° X for every mesh.

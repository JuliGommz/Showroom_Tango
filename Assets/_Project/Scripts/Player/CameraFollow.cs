/*
====================================================================
* CameraFollow - Cinemachine Camera Target Assignment
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-12-18
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Cinemachine integration strategy
* - Owner-only camera follow logic
* 
* [AI-ASSISTED]
* - NetworkBehaviour pattern
* - Virtual Camera target assignment
* - Null-safety checks
* 
* [AI-GENERATED]
* - Complete implementation
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour)
* - Unity.Cinemachine (CinemachineCamera)
* 
* NOTES:
* - Attached to Player Prefab
* - Only owner's camera follows their player
* - Non-owners ignore camera assignment
====================================================================
*/

using FishNet.Object;
using UnityEngine;
using Unity.Cinemachine;

public class CameraFollow : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        CinemachineCamera virtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            virtualCamera.Follow = transform;
        }
        else
        {
            Debug.LogError("CameraFollow: Cinemachine Virtual Camera not found in scene!");
        }
    }
}

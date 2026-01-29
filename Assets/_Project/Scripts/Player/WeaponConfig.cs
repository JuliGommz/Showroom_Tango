/*
====================================================================
* WeaponConfig - Weapon Data Configuration (ScriptableObject)
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-08
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - 3-weapon slot design decision
* - Bullet sprite assignments
* - Fire rate values
* - Upgrade increment values (0.05 fire rate, 0.03 cooldown)
* 
* [AI-ASSISTED]
* - ScriptableObject pattern implementation
* - Upgrade stat structure
* - Runtime stat calculation properties
* 
* [AI-GENERATED]
* - None
* 
* DEPENDENCIES:
* - UnityEngine (ScriptableObject)
* 
* NOTES:
* - ScriptableObject for data-driven weapon design
* - Runtime upgrades tracked via non-serialized fields
* - Current stats calculated dynamically from base + upgrades
====================================================================
*/

using UnityEngine;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Showroom_Tango/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
    [Header("Visual")]
    public Sprite bulletSprite;
    public string weaponName;

    [Header("Stats")]
    public float baseFireRate = 0.3f;
    public float baseCooldown = 0.2f;
    public float range = 8f;
    public int damage = 10;

    [Header("Firing Position")]
    public Vector2 firePointOffset = Vector2.zero;
    public float directionAngleOffset = 0f;

    // Runtime upgrade tracking (not persisted)
    [System.NonSerialized] public int fireRateUpgrades = 0;
    [System.NonSerialized] public int cooldownUpgrades = 0;

    public float CurrentFireRate => baseFireRate - (fireRateUpgrades * 0.05f);
    public float CurrentCooldown => baseCooldown - (cooldownUpgrades * 0.03f);
}

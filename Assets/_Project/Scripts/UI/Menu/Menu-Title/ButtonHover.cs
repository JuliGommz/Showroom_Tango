/*
====================================================================
* ButtonHover - Transparent Button Hover Feedback
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Transparent button hover concept
* - Alpha value (0.2 on hover)
* 
* [AI-ASSISTED]
* - Unity EventSystem interface implementation
* - Image component caching
* 
* [AI-GENERATED]
* - Complete implementation
* 
* DEPENDENCIES:
* - UnityEngine.EventSystems (IPointerEnterHandler, IPointerExitHandler)
* - UnityEngine.UI (Image)
* 
* NOTES:
* - Simple hover feedback for transparent UI elements
* - Alpha 0 (invisible) -> 0.2 (subtle) on hover
====================================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TransparentButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverAlpha = 0.2f;
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Color c = image.color;
        c.a = hoverAlpha;
        image.color = c;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Color c = image.color;
        c.a = 0f;
        image.color = c;
    }
}

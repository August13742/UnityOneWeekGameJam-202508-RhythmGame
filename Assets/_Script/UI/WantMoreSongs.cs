using UnityEngine;
using UnityEngine.EventSystems;

public class WantMoreSongs : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject infoTextObj;

    private static WantMoreSongs currentlyExpanded;

    private void Awake()
    {
        if (infoTextObj != null)
            infoTextObj.SetActive(false);
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentlyExpanded == this)
        {
            Collapse();
            currentlyExpanded = null;
        }
        else
        {
            if (currentlyExpanded != null)
                currentlyExpanded.Collapse();

            Expand();
            currentlyExpanded = this;
        }
    }

    public void Expand()
    {
        if (infoTextObj != null)
            infoTextObj.SetActive(true);
    }

    public void Collapse()
    {
        if (infoTextObj != null)
            infoTextObj.SetActive(false);
    }

    public static void CollapseIfExpanded()
    {
        if (currentlyExpanded != null)
        {
            currentlyExpanded.Collapse();
            currentlyExpanded = null;
        }
    }
}

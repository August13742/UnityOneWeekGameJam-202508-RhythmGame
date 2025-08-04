using UnityEngine;
using TMPro;

namespace Rhythm.UI
{
    public class NotificationText : MonoBehaviour
    {
        private TMP_Text label;
        private float lifetime = 1f;
        
        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }
    }
}

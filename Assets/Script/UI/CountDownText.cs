using UnityEngine;
using TMPro;
namespace Rhythm.UI
{
    public class CountDownText : MonoBehaviour
    {
        private TMP_Text label;
        private double scheduledStartTime;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
        }

        public void SetScheduledStartTime(double startTime)
        {
            scheduledStartTime = startTime;
        }

        private void Update()
        {
            double timeLeft = scheduledStartTime - AudioSettings.dspTime;
            label.text = Mathf.Max(0, (float)timeLeft).ToString("0.00");
            if (timeLeft < 0)
            {
                Destroy(gameObject);
            }
        }
    }


}


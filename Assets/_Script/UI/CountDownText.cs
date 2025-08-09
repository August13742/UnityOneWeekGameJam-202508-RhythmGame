using UnityEngine;
using TMPro;
namespace Rhythm.UI
{
    public class CountDownText : MonoBehaviour
    {
        private TMP_Text label;
        private double scheduledStartTime;
        private bool isScheduled = false;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            label.enabled = false;
        }
        private void Start()
        {
            gameObject.SetActive(true);
        }

        public void SetScheduledStartTime(double startTime)
        {
            scheduledStartTime = startTime;
            isScheduled = true;
            label.enabled = true;
        }

        private void Update()
        {
            if (!isScheduled)
            {
                label.enabled = false;
                return;
            }

            double timeLeft = scheduledStartTime - AudioSettings.dspTime;
            if (timeLeft > 0)
            {
                label.text = Mathf.Max(0, (float)timeLeft).ToString("0.00");
                label.enabled = true;
            }
            else
            {
                label.enabled = false;
                isScheduled = false;
                Destroy(gameObject);
            }
        }
    }
}


using UnityEngine;
using UnityEngine.UI;

namespace Rhythm.GamePlay
{
    public class OSUBeatNote : MonoBehaviour
    {
        [SerializeField] private Image hitCircle;
        [SerializeField] private RawImage approachRing;

        [Header("Note Parameters")]
        [Tooltip("Scale multiplier for the approach circle at spawn.")]
        [SerializeField] private float initialScale = 1.0f;

        [Header("Timing Windows (in seconds)")]
        [Tooltip("Time window for a 'Perfect' hit, +/- from the exact hit time.")]
        [SerializeField] private float perfectWindow = 0.1f;
        [Tooltip("Time window for a 'Regular' hit, +/- from the exact hit time.")]
        [SerializeField] private float regularWindow = 0.2f;


        float timeToHit;
        float approachTime;
        float spawnTime;
        bool isHit = false;
        bool isMissed = false;

        public void Initialise(float hitTime, float approachDuration)
        {
            timeToHit = hitTime;
            approachTime = approachDuration;
            spawnTime = timeToHit - approachTime;

            isHit = false;
            isMissed = false;
            gameObject.SetActive(true);
            approachRing.rectTransform.localScale = new Vector3(initialScale, initialScale, 1f);
        }
        private void Update()
        {
            if (isHit || isMissed) return;

            float currentTime = (float) AudioSettings.dspTime;
            UpdateApproachRing(currentTime);
            CheckForMiss(currentTime);

        }
        private void UpdateApproachRing(float currentTime)
        {
            float elapsedTime = currentTime - spawnTime;
            float progress = Mathf.Clamp01(elapsedTime/approachTime);

            float currentScale = Mathf.Lerp(initialScale, .5f, progress);
            approachRing.rectTransform.localScale = new Vector3 (currentScale, currentScale, 1f);
        }
        private void CheckForMiss(float currentTime)
        {
            if (currentTime > timeToHit + regularWindow)
            {
                isMissed = true;
                Debug.Log("result: Miss");
                gameObject.SetActive (false);
            }
        }
        public string AttemptHit()
        {
            if (isHit || isMissed)
                return "Inactive";

            isHit = true;

            float hitDelta = Mathf.Abs((float)AudioSettings.dspTime - timeToHit);
            string result;
            if (hitDelta <= perfectWindow)
            {
                result = "Perfect";
                hitCircle.GetComponent<Image>().color = Color.green;
                // add more
            }
            else if (hitDelta <= regularWindow)
            {
                result = "Regular";
                // add more
                hitCircle.GetComponent<Image>().color = Color.gray;
            }
            else
            {
                result = "Fail";
                // add more
                hitCircle.GetComponent<Image>().color = Color.red;
            }
            Debug.Log($"Result: {result} (Delta: {hitDelta:F3}s)");
            


            StartCoroutine(DeactivateAfterDelay(0.15f));
            
            return result;
        }

        private System.Collections.IEnumerator DeactivateAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}


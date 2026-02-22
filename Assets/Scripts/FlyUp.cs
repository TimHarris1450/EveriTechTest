using System.Collections;
using Scripts.Core.Engine;
using UnityEngine;

namespace Scripts
{
    public class FlyUp : MonoBehaviour
    {
        [SerializeField]
        private MeterValue _jackpotMeter;
        [SerializeField]
        private GameObject FlyUpObject;
        private Transform _flyUpPos;
        private bool _playing = false;

        public void Awake()
        {
            if (_jackpotMeter == null)
            {
                _jackpotMeter = GameObject.Find("JackpotMeter")?.GetComponentInChildren<MeterValue>();
            }
        }

        public void TriggerFromSpinResult(SpinResult spinResult)
        {
            if (spinResult != null && spinResult.TriggeredFeatures.Contains("bonus_triggered"))
            {
                StartFlyUp();
            }
        }

        public void StartFlyUp()
        {
            if (_playing) { return; }
            StartCoroutine(FlyUpRoutine());
        }

        private IEnumerator FlyUpRoutine()
        {
            FlyUpObject.SetActive(true);
            _playing = true;
            if (_flyUpPos == null)
            {
                _flyUpPos = GameObject.Find("FlyUpPoint")?.transform;
            }

            if (_flyUpPos == null)
            {
                _playing = false;
                yield break;
            }

            yield return new WaitForSeconds(1);
            while (Vector3.Distance(FlyUpObject.transform.position, _flyUpPos.position) > 0.1f)
            {
                FlyUpObject.transform.position = Vector3.MoveTowards(FlyUpObject.transform.position, _flyUpPos.position, Time.deltaTime * 20);
                yield return null;
            }

            if (_jackpotMeter != null)
            {
                _jackpotMeter.AddToValue(1000);
            }

            FlyUpObject.SetActive(false);
            FlyUpObject.transform.position = transform.parent.position;
            _playing = false;
        }
    }
}

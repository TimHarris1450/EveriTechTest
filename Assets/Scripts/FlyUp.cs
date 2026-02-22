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
        [SerializeField]
        private Transform _flyUpPos;

        private bool _playing;

        public void SetMeterValue(MeterValue meterValue)
        {
            _jackpotMeter = meterValue;
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
            if (_playing)
            {
                return;
            }

            StartCoroutine(FlyUpRoutine());
        }

        private IEnumerator FlyUpRoutine()
        {
            if (_flyUpPos == null)
            {
                yield break;
            }

            FlyUpObject.SetActive(true);
            _playing = true;
            yield return new WaitForSeconds(1);
            while (Vector3.Distance(FlyUpObject.transform.position, _flyUpPos.position) > 0.1f)
            {
                FlyUpObject.transform.position = Vector3.MoveTowards(FlyUpObject.transform.position, _flyUpPos.position, Time.deltaTime * 20);
                yield return null;
            }

            _jackpotMeter?.AddToValue(1000);
            FlyUpObject.SetActive(false);
            FlyUpObject.transform.position = transform.parent.position;
            _playing = false;
        }
    }
}

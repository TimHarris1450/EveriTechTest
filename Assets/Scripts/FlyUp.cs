using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace Scripts
{
    public class FlyUp : MonoBehaviour
    {
        // jackpot meter
        [SerializeField]
        private MeterValue _jackpotMeter;
        // fly up object
        [SerializeField]
        private GameObject FlyUpObject;
        // transform to fly up position
        private Transform _flyUpPos;
        // bool for playing
        private bool _playing = false;


        // check and activate Fly Up
        public void Awake()
        {
            _jackpotMeter = GameObject.Find("JackpotMeter").GetComponentInChildren<MeterValue>();
        }
        // method to launch fly up
        public void StartFlyUp()
        {
            if (_playing) { return; }
            StartCoroutine(FlyUpRoutine());
        }
        // launch the fly up
        private IEnumerator FlyUpRoutine()
        {
            FlyUpObject.SetActive(true);
            _playing = true;
            // get fly up position
            if (_flyUpPos == null)
            { 
                _flyUpPos = GameObject.Find("FlyUpPoint").transform;
            }
            // wait a second
            yield return new WaitForSeconds(1);
            // move to fly up position
            while (Vector3.Distance(FlyUpObject.transform.position, _flyUpPos.position) > 0.1f)
            {
                FlyUpObject.transform.position = Vector3.MoveTowards(FlyUpObject.transform.position, _flyUpPos.position, Time.deltaTime * 20);
                yield return null;
            }
            // add value to jackpot
            if(_jackpotMeter != null)
            {
                _jackpotMeter.AddToValue(1000);
            }   
            // turn it off
            FlyUpObject.SetActive(false);
            // reset position
            FlyUpObject.transform.position = transform.parent.position;
            _playing = false;
        }
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Scripts
{
    public class FlyUp : MonoBehaviour
    {
        // fly up object
        [SerializeField]
        private GameObject FlyUpObject;
        // transform to fly up position
        private Transform _flyUpPos;


        // check and activate Fly Up
        public void OnEnable()
        {
            StartCoroutine(FlyUpRoutine());     
        }

        // launch the fly up
        private IEnumerator FlyUpRoutine()
        {
            // get fly up position
            _flyUpPos = GameObject.Find("FlyUpPoint").transform;
            // wait a second
            yield return new WaitForSeconds(1);
            // move to fly up position
            while (Vector3.Distance(FlyUpObject.transform.position, _flyUpPos.position) > 0.1f)
            {
                FlyUpObject.transform.position = Vector3.MoveTowards(FlyUpObject.transform.position, _flyUpPos.position, Time.deltaTime * 30);
                yield return null;
            }
            // turn it off
            FlyUpObject.SetActive(false);
            // reset position
            FlyUpObject.transform.position = new Vector3(0, 0, 0);
        }
    }
}

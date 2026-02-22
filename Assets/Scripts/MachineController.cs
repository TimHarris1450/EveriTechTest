using System.Collections;
using System.Collections.Generic;
using Scripts.Core.Math;
using Scripts.Presentation;
using UnityEngine;

namespace Scripts
{
    public class MachineController : MonoBehaviour
    {
        [SerializeField]
        public GameObject Reels;
        [SerializeField]
        private float _wait = 2f;
        private float _defaultWait;
        [SerializeField]
        private float _waitIncrement = 0.75f;
        [SerializeField]
        private bool _spinning = false;

        private SlotMathEngine _slotMathEngine;

        private void Awake()
        {
            SlotMathModel model = DefaultSlotMathModel.Create();
            _slotMathEngine = new SlotMathEngine(model);

            SymbolRegistry symbolRegistry = FindObjectOfType<SymbolRegistry>();
            if (symbolRegistry == null)
            {
                GameObject registryObject = new("SymbolRegistry");
                symbolRegistry = registryObject.AddComponent<SymbolRegistry>();
            }

            List<GameObject> symbolPrefabs = new();

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                ImageSetter imageSetter = Reels.transform.GetChild(i).GetComponent<ImageSetter>();
                if (imageSetter != null)
                {
                    imageSetter.ConfigureMath(model, i);
                    imageSetter.SetSymbolRegistry(symbolRegistry);

                    foreach (GameObject symbol in imageSetter.Symbols)
                    {
                        if (symbol != null)
                        {
                            symbolPrefabs.Add(symbol);
                        }
                    }
                }
            }

            symbolRegistry.BuildFromPrefabs(symbolPrefabs);
        }

        public void SpinCheck()
        {
            switch (_spinning)
            {
                case true:
                    StartCoroutine(StopSpin());
                    break;
                case false:
                    StartCoroutine(StartSpin());
                    break;
            }
        }

        private IEnumerator StartSpin()
        {
            _defaultWait = _wait;
            _spinning = true;

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("spin");
                _wait -= _waitIncrement;
                yield return new WaitForSeconds(_wait);
            }

            yield return new WaitForSeconds(_defaultWait);
            StartCoroutine(StopSpin());
        }

        private IEnumerator StopSpin()
        {
            BonusTracker _bonusTracker = FindObjectOfType<BonusTracker>();
            _wait = _defaultWait;

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                ImageSetter imageSetter = Reels.transform.GetChild(i).GetComponent<ImageSetter>();
                if (imageSetter != null)
                {
                    imageSetter.SetResolvedStopSymbols(_slotMathEngine.ResolveStopSymbolsForReel(i));
                }

                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("stop");
                _wait += 0.5f;
                yield return new WaitForSeconds(_wait);
                _bonusTracker.CheckSymbol();
            }

            yield return new WaitForSeconds(_wait);
            Stopped();
        }

        public void Stopped()
        {
            _spinning = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Scripts.Core.Engine;
using UnityEngine;

namespace Scripts
{
    public class BonusTracker : MonoBehaviour
    {
        [SerializeField]
        private BlackHoleController _blackHole;
        [SerializeField]
        private SymbolAnimController _symbolAnimController;

        public List<Animator> Animators;
        public int BonusCount;

        private void Awake()
        {
            _symbolAnimController ??= SymbolAnimController.Instance;
        }

        public void SetSymbolAnimController(SymbolAnimController symbolAnimController)
        {
            _symbolAnimController = symbolAnimController;
        }

        public void AddSymbol(Animator anim)
        {
            if (anim == null)
            {
                return;
            }

            if (!Animators.Contains(anim))
            {
                Animators.Add(anim);
            }
        }

        public void Increment()
        {
            BonusCount += 1;
        }

        public void CheckSymbol()
        {
            StartCoroutine(CheckSymbolCoroutine());
        }

        public void ApplySpinResult(SpinResult spinResult, List<Animator> bonusAnimators)
        {
            ResetSymbols();
            if (bonusAnimators != null)
            {
                for (int i = 0; i < bonusAnimators.Count; i++)
                {
                    AddSymbol(bonusAnimators[i]);
                }
            }

            BonusCount = Animators.Count;
            StartCoroutine(CheckSymbolCoroutine(spinResult));
        }

        private IEnumerator CheckSymbolCoroutine(SpinResult spinResult = null)
        {
            if (_symbolAnimController == null)
            {
                yield break;
            }

            switch (BonusCount)
            {
                case 3:
                case > 3:
                    _symbolAnimController.PlayWin(Animators);
                    yield return new WaitForSeconds(0.85f);
                    _blackHole?.PlayBlackHole();
                    break;
                case 2:
                    _symbolAnimController.PlayAnticipation(Animators);
                    break;
            }

            yield return new WaitForSeconds(1f);

            if (spinResult == null || !spinResult.TriggeredFeatures.Contains("bonus_triggered"))
            {
                ResetSymbols();
            }
        }

        private void ResetSymbols()
        {
            BonusCount = 0;
            Animators.Clear();
        }
    }
}

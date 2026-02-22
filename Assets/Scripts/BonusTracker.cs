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

        public List<Animator> Animators;
        public int BonusCount = 0;

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
                foreach (Animator animator in bonusAnimators)
                {
                    AddSymbol(animator);
                }
            }

            BonusCount = Animators.Count;
            StartCoroutine(CheckSymbolCoroutine(spinResult));
        }

        private IEnumerator CheckSymbolCoroutine(SpinResult spinResult = null)
        {
            SymbolAnimController symAnims = FindObjectOfType<SymbolAnimController>();
            if (symAnims == null)
            {
                yield break;
            }

            switch (BonusCount)
            {
                case 3:
                case > 3:
                    symAnims.PlayWin(Animators);
                    yield return new WaitForSeconds(0.85f);
                    _blackHole?.PlayBlackHole();
                    break;
                case 2:
                    symAnims.PlayAnticipation(Animators);
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scripts.Core.Engine;
using Scripts.Core.Math;
using Scripts.Core.MathLoading;
using Scripts.Presentation;
using UnityEngine;

namespace Scripts
{
    public class MachineController : MonoBehaviour
    {
        private enum SpinMigrationMode
        {
            LegacyOnly,
            SpinResultOnly,
            ParallelCompare
        }

        [SerializeField]
        public GameObject Reels;
        [SerializeField]
        private float _wait = 2f;
        private float _defaultWait;
        [SerializeField]
        private float _waitIncrement = 0.75f;
        [SerializeField]
        private bool _spinning = false;
        [SerializeField]
        private SpinMigrationMode _migrationMode = SpinMigrationMode.ParallelCompare;
        [SerializeField]
        private BonusTracker _bonusTracker;
        [SerializeField]
        private FlyUp _flyUp;
        [SerializeField]
        private MeterValue _meterValue;

        [SerializeField]
        private SlotMathConfig _slotMathConfig;

        private SlotMathEngine _slotMathEngine;
        private SlotEngine _slotEngine;
        private SlotMathModel _model;

        private void Awake()
        {
            _model = LoadMathModel();
            _slotMathEngine = new SlotMathEngine(_model);
            _slotEngine = new SlotEngine(_model, new SeededRNGProvider());

            _bonusTracker ??= FindObjectOfType<BonusTracker>();
            _flyUp ??= FindObjectOfType<FlyUp>();
            _meterValue ??= FindObjectOfType<MeterValue>();

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
                    imageSetter.ConfigureMath(_model, i);
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

        private SlotMathModel LoadMathModel()
        {
            if (_slotMathConfig == null)
            {
                Debug.LogWarning("SlotMathConfig is not assigned. Falling back to DefaultSlotMathModel.");
                return DefaultSlotMathModel.Create();
            }

            try
            {
                return _slotMathConfig.LoadMathModel();
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to load slot math from config asset: {exception.Message}");
                return DefaultSlotMathModel.Create();
            }
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
            _wait = _defaultWait;
            SpinResult spinResult = (_migrationMode == SpinMigrationMode.LegacyOnly)
                ? null
                : _slotEngine.Spin();

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                ImageSetter imageSetter = Reels.transform.GetChild(i).GetComponent<ImageSetter>();
                if (imageSetter != null)
                {
                    IReadOnlyList<int> stopSymbols = ResolveStopSymbols(i, spinResult);
                    imageSetter.SetResolvedStopSymbols(stopSymbols);
                }

                Reels.transform.GetChild(i).GetComponent<Animator>().SetTrigger("stop");
                _wait += 0.5f;
                yield return new WaitForSeconds(_wait);

                if (_migrationMode != SpinMigrationMode.SpinResultOnly)
                {
                    _bonusTracker?.CheckSymbol();
                }
            }

            yield return new WaitForSeconds(_wait);

            if (spinResult != null)
            {
                ApplySpinResult(spinResult);
            }

            Stopped();
        }

        private IReadOnlyList<int> ResolveStopSymbols(int reelIndex, SpinResult spinResult)
        {
            if (spinResult != null && reelIndex < spinResult.LandedSymbolMatrix.Count)
            {
                return spinResult.LandedSymbolMatrix[reelIndex];
            }

            return _slotMathEngine.ResolveStopSymbolsForReel(reelIndex);
        }

        private void ApplySpinResult(SpinResult spinResult)
        {
            List<Animator> bonusAnimators = new();
            int bonusSymbolId = _model.Symbols.FirstOrDefault(symbol => symbol.IsBonus)?.Id ?? -1;

            for (int reelIndex = 0; reelIndex < Reels.transform.childCount; reelIndex++)
            {
                ImageSetter imageSetter = Reels.transform.GetChild(reelIndex).GetComponent<ImageSetter>();
                if (imageSetter == null)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < _model.Config.VisibleRows; rowIndex++)
                {
                    int symbolId = imageSetter.GetResolvedSymbolId(rowIndex);
                    if (symbolId != bonusSymbolId)
                    {
                        continue;
                    }

                    Animator symbolAnimator = imageSetter.GetSymbolAnimator(rowIndex);
                    if (symbolAnimator != null)
                    {
                        bonusAnimators.Add(symbolAnimator);
                    }
                }
            }

            _bonusTracker?.ApplySpinResult(spinResult, bonusAnimators);
            _flyUp?.TriggerFromSpinResult(spinResult);
            _meterValue?.ApplySpinResult(spinResult);
        }

        public void Stopped()
        {
            _spinning = false;
        }
    }
}

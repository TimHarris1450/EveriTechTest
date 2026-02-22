using System.Collections;
using System.Collections.Generic;
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
        private bool _spinning;
        [SerializeField]
        private SpinMigrationMode _migrationMode = SpinMigrationMode.ParallelCompare;
        [SerializeField]
        private BonusTracker _bonusTracker;
        [SerializeField]
        private FlyUp _flyUp;
        [SerializeField]
        private MeterValue _meterValue;
        [SerializeField]
        private SymbolRegistry _symbolRegistry;

        [SerializeField]
        private SlotMathConfig _slotMathConfig;

        [Header("Debug Seed")]
        [SerializeField]
        private bool _showSeedDebugUi;
        [SerializeField]
        private string _seedInput = string.Empty;

        private SlotMathEngine _slotMathEngine;
        private SlotEngine _slotEngine;
        private SlotMathModel _model;
        private readonly List<ImageSetter> _imageSetters = new();
        private readonly List<Animator> _reelAnimators = new();

        private void Awake()
        {
            _model = LoadMathModel();
            _slotMathEngine = new SlotMathEngine(_model);
            _slotEngine = new SlotEngine(_model, new SeededRNGProvider());

            CacheSceneDependencies();
            BuildReelCaches();
            BuildSymbolRegistry();
        }

        private void CacheSceneDependencies()
        {
            _bonusTracker ??= GetComponentInChildren<BonusTracker>(true);
            _flyUp ??= GetComponentInChildren<FlyUp>(true);
            _meterValue ??= GetComponentInChildren<MeterValue>(true);
            _symbolRegistry ??= GetComponentInChildren<SymbolRegistry>(true);

            if (_symbolRegistry == null)
            {
                GameObject registryObject = new("SymbolRegistry");
                _symbolRegistry = registryObject.AddComponent<SymbolRegistry>();
            }

            if (_bonusTracker != null)
            {
                _bonusTracker.SetSymbolAnimController(SymbolAnimController.Instance);
            }

            if (_flyUp != null)
            {
                _flyUp.SetMeterValue(_meterValue);
            }
        }

        private void BuildReelCaches()
        {
            _imageSetters.Clear();
            _reelAnimators.Clear();

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                Transform reelTransform = Reels.transform.GetChild(i);
                ImageSetter imageSetter = reelTransform.GetComponent<ImageSetter>();
                Animator reelAnimator = reelTransform.GetComponent<Animator>();

                _imageSetters.Add(imageSetter);
                _reelAnimators.Add(reelAnimator);

                if (imageSetter == null)
                {
                    continue;
                }

                imageSetter.ConfigureMath(_model, i);
                imageSetter.SetSymbolRegistry(_symbolRegistry);
                imageSetter.SetBonusTracker(_bonusTracker);
                imageSetter.SetSymbolAnimController(SymbolAnimController.Instance);
                imageSetter.SetSymbolSwapPoolingEnabled(_migrationMode == SpinMigrationMode.SpinResultOnly);
            }
        }

        private void BuildSymbolRegistry()
        {
            List<GameObject> symbolPrefabs = new();
            for (int i = 0; i < _imageSetters.Count; i++)
            {
                ImageSetter imageSetter = _imageSetters[i];
                if (imageSetter == null)
                {
                    continue;
                }

                for (int symbolIndex = 0; symbolIndex < imageSetter.Symbols.Count; symbolIndex++)
                {
                    GameObject symbol = imageSetter.Symbols[symbolIndex];
                    if (symbol != null)
                    {
                        symbolPrefabs.Add(symbol);
                    }
                }
            }

            _symbolRegistry.BuildFromPrefabs(symbolPrefabs);

            for (int i = 0; i < _imageSetters.Count; i++)
            {
                _imageSetters[i]?.RefreshSymbolLookup();
            }
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
            if (_spinning)
            {
                StartCoroutine(StopSpin());
                return;
            }

            StartCoroutine(StartSpin());
        }

        private IEnumerator StartSpin()
        {
            _defaultWait = _wait;
            _spinning = true;

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                if (i < _reelAnimators.Count && _reelAnimators[i] != null)
                {
                    _reelAnimators[i].SetTrigger("spin");
                }

                _wait -= _waitIncrement;
                yield return new WaitForSeconds(_wait);
            }

            yield return new WaitForSeconds(_defaultWait);
            StartCoroutine(StopSpin());
        }

        private IEnumerator StopSpin()
        {
            _wait = _defaultWait;
            SpinResult spinResult = _migrationMode == SpinMigrationMode.LegacyOnly ? null : _slotEngine.Spin();

            for (int i = 0; i < Reels.transform.childCount; i++)
            {
                ImageSetter imageSetter = i < _imageSetters.Count ? _imageSetters[i] : null;
                if (imageSetter != null)
                {
                    IReadOnlyList<int> stopSymbols = ResolveStopSymbols(i, spinResult);
                    imageSetter.SetResolvedStopSymbols(stopSymbols);
                }

                if (i < _reelAnimators.Count && _reelAnimators[i] != null)
                {
                    _reelAnimators[i].SetTrigger("stop");
                }

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
            int bonusSymbolId = -1;
            for (int i = 0; i < _model.Symbols.Count; i++)
            {
                if (_model.Symbols[i].IsBonus)
                {
                    bonusSymbolId = _model.Symbols[i].Id;
                    break;
                }
            }

            for (int reelIndex = 0; reelIndex < _imageSetters.Count; reelIndex++)
            {
                ImageSetter imageSetter = _imageSetters[reelIndex];
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

        public void ApplyDeterministicSeed()
        {
            if (!int.TryParse(_seedInput, out int parsedSeed))
            {
                Debug.LogWarning($"Unable to parse seed '{_seedInput}'.");
                return;
            }

            _slotEngine = new SlotEngine(_model, new SeededRNGProvider(parsedSeed));
            _slotMathEngine = new SlotMathEngine(_model, parsedSeed);
            Debug.Log($"Applied deterministic seed: {parsedSeed}");
        }

        private void OnGUI()
        {
            if (!_showSeedDebugUi)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(12, 12, 300, 120), "Debug Seed", GUI.skin.window);
            GUILayout.Label("Deterministic Seed");
            _seedInput = GUILayout.TextField(_seedInput, 20);
            if (GUILayout.Button("Apply Seed"))
            {
                ApplyDeterministicSeed();
            }

            GUILayout.EndArea();
        }

        public void Stopped()
        {
            _spinning = false;
        }
    }
}

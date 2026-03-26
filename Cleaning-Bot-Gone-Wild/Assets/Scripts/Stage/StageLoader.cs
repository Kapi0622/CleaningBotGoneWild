using CleaningBot.Data;

namespace CleaningBot.Stage
{
    /// <summary>
    /// ScriptableObject から StageData を読み込む純粋クラス。
    /// Startup から new(stageData) で生成し、Load() でデータを取得する。
    /// 将来 Resources.Load / Addressables への差し替えはこのクラス内部だけで完結する。
    /// </summary>
    public class StageLoader
    {
        private readonly StageData _stageData;

        public StageLoader(StageData stageData)
        {
            _stageData = stageData;
        }

        public StageData Load() => _stageData;
    }
}

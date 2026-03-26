namespace CleaningBot.Core
{
    /// <summary>
    /// リザルト画面に表示するデータの値オブジェクト。
    /// ResultPresenter が各 Model から組み立てて ResultView に渡す。
    /// </summary>
    public record ResultData(
        int MainScore,
        int SubScore,
        int RemainingGarbage,
        float ElapsedTime,
        int StarRank);
}

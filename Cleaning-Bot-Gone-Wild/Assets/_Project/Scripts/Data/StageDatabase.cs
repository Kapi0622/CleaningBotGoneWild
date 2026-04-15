using System.Collections.Generic;
using UnityEngine;

namespace CleaningBot.Data
{
    /// <summary>
    /// ゲーム内の全ステージを管理する ScriptableObject。
    /// タイトル画面・ステージ選択画面がこれを参照してリストを構築する。
    /// </summary>
    [CreateAssetMenu(fileName = "StageDatabase", menuName = "CleaningBot/StageDatabase")]
    public class StageDatabase : ScriptableObject
    {
        public List<StageData> stages;
    }
}

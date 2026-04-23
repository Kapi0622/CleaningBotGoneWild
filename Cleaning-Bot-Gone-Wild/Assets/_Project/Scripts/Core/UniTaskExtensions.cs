using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CleaningBot.Core
{
    public static class UniTaskExtensions
    {
        public static void ForgetWithLog(this UniTask task) =>
            task.Forget(ex => { if (ex is not OperationCanceledException) Debug.LogException(ex); });
    }
}

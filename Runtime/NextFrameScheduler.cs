// Copyright (c) 2026 MauGames contributors. All rights reserved.
// Any use is prohibited without prior written permission from all four rights holders.
// License: LICENSE in the repository root.
// Reference: https://github.com/VadimCat/TapAway/blob/master/LICENSE

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Services.Saves
{
    public sealed class NextFrameScheduler
    {
        private readonly Action _action;
        private readonly PlayerLoopTiming _timing;
        private readonly CancellationToken _cancellationToken;
        private bool _scheduled;

        public NextFrameScheduler(
            Action action,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _timing = timing;
            _cancellationToken = cancellationToken;
        }

        public void Schedule()
        {
            if (_scheduled)
            {
                return;
            }

            _scheduled = true;
            Run().Forget();
        }

        private async UniTaskVoid Run()
        {
            try
            {
                if (_cancellationToken.CanBeCanceled)
                {
                    await UniTask.Yield(_timing, _cancellationToken);
                }
                else
                {
                    await UniTask.Yield(_timing);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _scheduled = false;
            }

            try
            {
                _action.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}

using System;
using ArcherLegend.Application;
using ArcherLegend.Domain;

namespace ArcherLegend.Presentation.Game
{
    public sealed class GameHudPresenter : IDisposable
    {
        private readonly IGameHudView view;
        private readonly ArcherGameSession session;
        private UpgradeOption[] shownUpgradeOptions;

        public GameHudPresenter(IGameHudView view, ArcherGameSession session)
        {
            this.view = view;
            this.session = session;
            view.UpgradeSelected += OnUpgradeSelected;
        }

        /// <summary>
        /// 毎フレーム呼ばれます。強化の選択肢は抽選し直されたときだけ作り直します。
        /// </summary>
        public void Refresh(int aliveEnemies)
        {
            var player = session.Player;
            view.SetStatus(player.Health, player.MaxHealth, player.Level, player.Experience, player.ExperienceToNext, session.Wave, session.Score, aliveEnemies);

            // 抽選のたびに新しい配列になるので、参照の比較で「選択肢が変わったか」を判定できる
            var options = session.IsUpgradePending && !session.IsGameOver ? session.CurrentUpgradeOptions : null;
            if (ReferenceEquals(options, shownUpgradeOptions))
            {
                return;
            }

            shownUpgradeOptions = options;
            if (options != null)
            {
                view.ShowUpgrades(options);
            }
            else
            {
                view.HideUpgrades();
            }
        }

        public void Dispose()
        {
            view.UpgradeSelected -= OnUpgradeSelected;
        }

        private void OnUpgradeSelected(int index)
        {
            var options = session.CurrentUpgradeOptions;
            if (!session.IsUpgradePending || index < 0 || index >= options.Length)
            {
                return;
            }

            session.ApplyUpgrade(options[index].Type);
        }
    }
}

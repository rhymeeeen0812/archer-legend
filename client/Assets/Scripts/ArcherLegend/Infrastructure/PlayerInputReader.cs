using UnityEngine;
using UnityEngine.InputSystem;

namespace ArcherLegend.Infrastructure
{
    /// <summary>
    /// Input System のプロジェクト共通アクション（InputSystem_Actions）から移動入力を読み取ります。
    /// キーボード（WASD・矢印キー）とゲームパッドの割り当てはアクションアセット側で管理します。
    /// </summary>
    public sealed class PlayerInputReader
    {
        private const string MoveActionPath = "Player/Move";

        private readonly InputAction moveAction;

        public PlayerInputReader()
        {
            moveAction = InputSystem.actions.FindAction(MoveActionPath, throwIfNotFound: true);
            moveAction.Enable();
        }

        /// <summary>移動方向です。斜め入力でも速くならないよう、長さを 1 以下に抑えます。</summary>
        public Vector2 Move => Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
    }
}

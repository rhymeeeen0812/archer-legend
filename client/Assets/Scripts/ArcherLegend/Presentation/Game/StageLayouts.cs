using System.Collections.Generic;
using UnityEngine;

namespace ArcherLegend.Presentation.Game
{
    /// <summary>
    /// ステージ（部屋）ごとの障害物の配置です。最初のステージは障害物なしで、2 番目からは用意した配置を順番に使います。
    /// どの配置も、プレイヤーが入ってくる位置（下）と出口（上）の周りは空けてあります。
    /// </summary>
    public static class StageLayouts
    {
        public const float ArenaHalfWidth = 9f;
        public const float ArenaHalfDepth = 6f;

        /// <summary>出口の幅の半分です。出口はアリーナの上の壁の真ん中にあります。</summary>
        public const float ExitHalfWidth = 1.2f;

        /// <summary>ステージに入ったときのプレイヤーの位置（下の壁の近く）です。</summary>
        public static readonly Vector3 PlayerStart = new Vector3(0f, 0.6f, -ArenaHalfDepth + 1.5f);

        /// <summary>この範囲にプレイヤーが入ると、次のステージへ進みます。</summary>
        public static readonly Rect ExitZone = new Rect(-ExitHalfWidth, ArenaHalfDepth - 1.0f, ExitHalfWidth * 2f, 1.0f);

        // 中心の X・Z と、横・奥行きの大きさで書いた配置
        private static readonly Rect[][] Patterns =
        {
            // 左右に柱が 1 本ずつ
            new[] { Box(-4.5f, 0f, 1.4f, 1.4f), Box(4.5f, 0f, 1.4f, 1.4f) },

            // 四隅寄りに柱が 4 本
            new[] { Box(-5f, -2.5f, 1.2f, 1.2f), Box(5f, -2.5f, 1.2f, 1.2f), Box(-5f, 2.5f, 1.2f, 1.2f), Box(5f, 2.5f, 1.2f, 1.2f) },

            // 真ん中に横長の壁。出口へは回り込んで進む
            new[] { Box(0f, 0.5f, 3.2f, 1.2f) },

            // 左右に縦長の壁
            new[] { Box(-3.5f, 1.5f, 0.8f, 4f), Box(3.5f, 1.5f, 0.8f, 4f) },

            // 小さな岩が散らばっている
            new[] { Box(-6f, -2f, 1f, 1f), Box(6f, 2f, 1f, 1f), Box(-2f, 2.5f, 1f, 1f), Box(2.5f, -2.5f, 1f, 1f) }
        };

        public static int PatternCount => Patterns.Length;

        /// <summary>そのステージの障害物を返します。ステージ 1 は障害物なしです。</summary>
        public static IReadOnlyList<Rect> For(int stage)
        {
            if (stage <= 1)
            {
                return System.Array.Empty<Rect>();
            }

            return Patterns[(stage - 2) % Patterns.Length];
        }

        public static bool IsInExit(Vector3 position)
        {
            return ExitZone.Contains(new Vector2(position.x, position.z));
        }

        private static Rect Box(float centerX, float centerZ, float width, float depth)
        {
            return new Rect(centerX - width * 0.5f, centerZ - depth * 0.5f, width, depth);
        }
    }
}

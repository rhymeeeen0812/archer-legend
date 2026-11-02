using System;

namespace ArcherLegend.Application
{
    public sealed class GameApiException : Exception
    {
        public GameApiException(long statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>HTTP ステータスコードです。サーバーに届かなかった場合は 0 です。</summary>
        public long StatusCode { get; }

        public bool IsNotFound => StatusCode == 404;

        /// <summary>サーバーの状態と食い違っている（回答済み・受付終了など）。送り直しても成功しません。</summary>
        public bool IsConflict => StatusCode == 409;

        /// <summary>通信エラーやサーバーの一時的な不調など、送り直せば成功する可能性があります。</summary>
        public bool IsRetryable => StatusCode == 0 || StatusCode == 408 || StatusCode == 429 || StatusCode >= 500;
    }
}

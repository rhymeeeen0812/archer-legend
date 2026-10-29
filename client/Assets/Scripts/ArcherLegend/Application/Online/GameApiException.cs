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
    }
}

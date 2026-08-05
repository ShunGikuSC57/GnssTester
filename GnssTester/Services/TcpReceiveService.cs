using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GnssTester.Services
{
    /// <summary>
    /// TCP受信サービス
    /// </summary>
    internal class TcpReceiveService
    {
        #region 定数
        /// <summary>
        /// 接続待ちタイムアウト時間（ミリ秒）
        /// </summary>
        private const int ACCEPT_TIMEOUT_MILLISECONDS = 5000;

        /// <summary>
        /// 接続後のデータ受信アイドルタイムアウト時間（ミリ秒）
        /// </summary>
        private const int IDLE_TIMEOUT_MILLISECONDS = 10000;

        /// <summary>
        /// 受信バッファサイズ
        /// </summary>
        private const int RECEIVE_BUFFER_SIZE = 4096;

        /// <summary>
        /// メッセージ区切り文字
        /// </summary>
        private const string DELIMITER = "\r\n";
        #endregion

        #region イベント
        /// <summary>
        /// 1件分のメッセージを受信した際に発生します。
        /// </summary>
        public event Action<string>? DataReceived;
        #endregion

        #region 公開メソッド
        #region RunAsync
        /// <summary>
        /// 接続待ち→受信を、キャンセルされるまで繰り返します。
        /// </summary>
        /// <param name="port">待ち受けポート番号</param>
        /// <param name="token">キャンセルトークン</param>
        public async Task RunAsync(int port, CancellationToken token)
        {
            TcpListener? listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Parse("0.0.0.0"), port);
                listener.Start();
                while (!token.IsCancellationRequested)
                {
                    var client = await AcceptWithTimeoutAsync(listener, token);
                    // 接続待ちタイムアウト → 待ち受けを継続する。
                    if (client is null) continue;

                    await ReceiveLoopAsync(client, token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Error("TCP受信処理に失敗しました。", ex);
            }
            finally
            {
                listener?.Stop();
            }
        }
        #endregion
        #endregion

        #region 内部メソッド
        #region AcceptWithTimeoutAsync
        /// <summary>
        /// タイムアウト付きで接続を待ち受けます。
        /// </summary>
        /// <param name="listener">TcpListener</param>
        /// <param name="token">キャンセルトークン</param>
        /// <returns>接続されたTcpClient。タイムアウト時はnull。</returns>
        private static async Task<TcpClient?> AcceptWithTimeoutAsync(TcpListener listener, CancellationToken token)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutSource.CancelAfter(ACCEPT_TIMEOUT_MILLISECONDS);
            try
            {
                return await listener.AcceptTcpClientAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // 接続待ちタイムアウト（停止ボタンによるキャンセルではない）
                return null;
            }
        }
        #endregion

        #region ReceiveLoopAsync
        /// <summary>
        /// 1つの接続に対して、アイドルタイムアウト付きでデータを受信し続けます。
        /// </summary>
        /// <param name="client">接続済みのTcpClient</param>
        /// <param name="token">キャンセルトークン</param>
        private async Task ReceiveLoopAsync(TcpClient client, CancellationToken token)
        {
            using var _ = client;
            using var stream = client.GetStream();
            var buffer = new byte[RECEIVE_BUFFER_SIZE];
            var pending = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutSource.CancelAfter(IDLE_TIMEOUT_MILLISECONDS);

                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, timeoutSource.Token);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    // アイドルタイムアウト → この接続を破棄し、接続待ちへ戻る。
                    return;
                }

                // 接続がクローズされた。
                if (bytesRead == 0) return;

                pending.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                ExtractMessages(pending);
            }
        }
        #endregion

        #region ExtractMessages
        /// <summary>
        /// バッファから区切り文字単位でメッセージを取り出し、通知します。
        /// </summary>
        /// <param name="pending">受信済みバッファ</param>
        private void ExtractMessages(StringBuilder pending)
        {
            var lines = pending.ToString().Split(DELIMITER);
            // 最後の要素は区切り文字未到達の可能性があるためバッファに戻す。
            pending.Clear();
            pending.Append(lines[^1]);

            for (var i = 0; i < lines.Length - 1; i++)
            {
                if (lines[i].Length > 0)
                    DataReceived?.Invoke(lines[i]);
            }
        }
        #endregion
        #endregion
    }
}
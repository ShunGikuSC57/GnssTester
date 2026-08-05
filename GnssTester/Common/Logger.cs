using GnssTester.Views;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace GnssTester
{
    /// <summary>
    /// ログ
    /// </summary>
    internal static class Logger
    {
        #region ログファイルパス
        /// <summary>
        /// ログファイルパス
        /// </summary>
        private static string LOG_FILE_PATH => Path.Combine(MainWindow.USER_DATA_DIR, "Logs", $"{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");
        #endregion

        #region ロックオブジェクト
        /// <summary>
        /// 書き込みロックオブジェクト
        /// </summary>
        private static readonly object _lockObj = new();
        #endregion

        #region 公開メソッド
        #region Write
        /// <summary>
        /// ログを出力します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        public static void Write(string message)
        {
            try
            {
                lock (_lockObj)
                {
                    var path = LOG_FILE_PATH;
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.AppendAllText(path, $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} {message}{Environment.NewLine}", new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        #endregion

        #region Error
        /// <summary>
        /// エラーログを出力します。
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <param name="ex">Exception</param>
        public static void Error(string errorMessage, Exception ex) => Write($"{errorMessage}{Environment.NewLine}{ex}");
        #endregion
        #endregion
    }
}

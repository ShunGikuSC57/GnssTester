using System.IO;
using System.Text;
using System.Text.Json;

namespace GnssTester.Models
{
    /// <summary>
    /// 接続設定
    /// </summary>
    public class ConnectionSetting
    {
        #region プロパティ
        /// <summary>
        /// 通信方法
        /// </summary>
        public CommunicationMethod CommunicationMethod { get; set; } = CommunicationMethod.Tcp;

        /// <summary>
        /// ポート番号を取得または設定します。
        /// </summary>
        /// <remarks>
        /// TCPとUDP共用
        /// </remarks>
        public int PortNumber { get; set; } = 4001;

        /// <summary>
        /// 受信フォーマットを取得または設定します。
        /// </summary>
        public ReceiveFormat ReceiveFormat { get; set; } = ReceiveFormat.GeoPoint;

        /// <summary>
        /// 受信結果を出力するか？を取得または設定します。
        /// </summary>
        public bool IsOutput { get; set; } = false;
        #endregion

        #region 公開メソッド
        #region Load
        /// <summary>
        /// 接続設定を読み込みます。
        /// </summary>
        /// <returns>接続設定</returns>
        public static ConnectionSetting Load()
        {
            var setting = new ConnectionSetting();
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                nameof(GnssTester), $"{nameof(ConnectionSetting)}.json");
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath, Encoding.UTF8);
                    setting = JsonSerializer.Deserialize<ConnectionSetting>(json) ?? new ConnectionSetting();
                }
                catch { }
            }

            return setting;
        }
        #endregion

        #region Save
        /// <summary>
        /// 接続設定を保存します。
        /// </summary>
        /// <param name="setting">接続設定</param>
        public static void Save(ConnectionSetting setting)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(GnssTester));
            var filePath = Path.Combine(dir, $"{nameof(ConnectionSetting)}.json");
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(setting, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                });

                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch { }
        }
        #endregion
        #endregion
    }
}

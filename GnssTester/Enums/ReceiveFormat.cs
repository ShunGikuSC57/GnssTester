namespace GnssTester
{
    /// <summary>
    /// 受信フォーマット
    /// </summary>
    public enum ReceiveFormat
    {
        /// <summary>緯度・経度</summary>
        GeoPoint,
        /// <summary>平面直角座標</summary>
        JapanGrid,
        /// <summary>緯度・経度JSON</summary>
        GeoPointJson,
        /// <summary>平面直角座標JSON</summary>
        JapanGridJson,
        /// <summary>NMEAフォーマット(GGA)</summary>
        NmeaFormatGga,
        /// <summary>NMEAフォーマット(RMC)</summary>
        NmeaFormatRmc
    }

    /// <summary>
    /// 受信フォーマット拡張
    /// </summary>
    internal static class ReceiveFormatExtension
    {
        #region 定数
        /// <summary>
        /// 受信フォーマット名マッピング
        /// </summary>
        private static readonly Dictionary<ReceiveFormat, string> NAME_MAP = new()
        {
            { ReceiveFormat.GeoPoint, "緯度・経度" },
            { ReceiveFormat.JapanGrid, "平面直角座標" },
            { ReceiveFormat.GeoPointJson, "緯度・経度JSON" },
            { ReceiveFormat.JapanGridJson, "平面直角座標JSON" },
            { ReceiveFormat.NmeaFormatGga, "NMEAフォーマット(GGA)" },
            { ReceiveFormat.NmeaFormatRmc, "NMEAフォーマット(RMC)" }
        };
        #endregion

        #region 拡張メソッド
        #region GetName
        /// <summary>
        /// 受信フォーマット名を取得します。
        /// </summary>
        /// <param name="sendFormat">受信フォーマット</param>
        /// <returns>受信フォーマット名</returns>
        public static string GetName(this ReceiveFormat sendFormat)
        {
            var name = string.Empty;
            if (NAME_MAP.TryGetValue(sendFormat, out string? value))
                name = value;

            return name;
        }
        #endregion
        #endregion
    }
}

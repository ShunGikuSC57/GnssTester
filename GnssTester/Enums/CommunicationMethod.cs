namespace GnssTester
{
    /// <summary>
    /// 通信方法
    /// </summary>
    public enum CommunicationMethod
    {
        /// <summary>TCP/IP</summary>
        Tcp,
        /// <summary>UDP(ブロードキャスト)</summary>
        Udp
    }

    /// <summary>
    /// 通信方法拡張
    /// </summary>
    internal static class CommunicationMethodExtension
    {
        #region 定数
        /// <summary>
        /// 通信方法名マッピング
        /// </summary>
        private static readonly Dictionary<CommunicationMethod, string> NAME_MAP = new()
        {
            { CommunicationMethod.Tcp, "TCP/IP" },
            { CommunicationMethod.Udp, "UDP(ブロードキャスト)" }
        };
        #endregion

        #region 拡張メソッド
        #region GetName
        /// <summary>
        /// 通信方法名を取得します。
        /// </summary>
        /// <param name="communicationMethod">通信方法</param>
        /// <returns>通信方法名</returns>
        public static string GetName(this CommunicationMethod communicationMethod)
        {
            var name = string.Empty;
            if (NAME_MAP.TryGetValue(communicationMethod, out string? value))
                name = value;

            return name;
        }
        #endregion
        #endregion
    }
}

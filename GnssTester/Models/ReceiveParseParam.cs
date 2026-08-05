using GisLibrary;

namespace GnssTester.Models
{
    /// <summary>
    /// 受信データ変換パラメータ
    /// </summary>
    internal class ReceiveParseParam
    {
        #region プロパティ
        /// <summary>
        /// 受信した1件分のメッセージ
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 平面直角座標系番号
        /// </summary>
        /// <remarks>
        /// 平面直角座標の場合に緯度・経度に変換するために使用します。
        /// </remarks>
        public PlaneRectangularCoordinateSystemNumber GridSystemNumber { get; set; }
        #endregion
    }
}

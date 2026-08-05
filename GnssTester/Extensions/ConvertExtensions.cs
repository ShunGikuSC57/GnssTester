using System.Globalization;

namespace GnssTester
{
    /// <summary>
    /// 変換拡張
    /// </summary>
    internal static class ConvertExtensions
    {
        #region 拡張メソッド
        #region ToDouble
        /// <summary>
        /// 対象を浮動小数点数に変換します。
        /// </summary>
        /// <param name="obj">変換対象</param>
        /// <returns>Null許容型浮動小数点数</returns>
        public static double? ToDouble(this object? obj)
        {
            double? result = null;
            if (obj != null)
            {
                var str = obj.ToString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    if (double.TryParse(str.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double d) && double.IsFinite(d))
                        result = d;
                }
            }

            return result;
        }
        #endregion
        #endregion
    }
}

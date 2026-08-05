using GisLibrary;
using GnssTester.Models;
using GnssTester.Services;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace GnssTester.Views
{
    /// <summary>
    /// メイン画面
    /// </summary>
    public partial class MainWindow : Window
    {
        #region フォルダパス
        /// <summary>
        /// アプリケーション固有ディレクリ
        /// </summary>
        public static string APP_DIR =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(GnssTester));

        /// <summary>
        /// ユーザーデータディレクトリ
        /// </summary>
        /// <remarks>
        /// WebView2のプロファイル等、マシン固有かつ頻繁に書き込まれるデータの格納先。
        /// </remarks>
        public static string USER_DATA_DIR =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(GnssTester));

        /// <summary>
        /// メイン画面地図HTMLの名前空間.フォルダ.ファイル名
        /// </summary>
        public const string MAIN_MAP_HTML_NAME = "GnssTester.Views.Map.index.html";
        #endregion

        #region ロックオブジェクト
        /// <summary>
        /// CSV書き込みロックオブジェクト
        /// </summary>
        private static readonly object _lockCsv = new();
        #endregion

        #region 平面直角座標系番号
        /// <summary>
        /// 系番号と原点緯度・経度のマッピング
        /// </summary>
        public static readonly Dictionary<PlaneRectangularCoordinateSystemNumber, string> GIRD_SYSTEM_NO_NAME_MAP = new()
        {
            { PlaneRectangularCoordinateSystemNumber.No1, "1系" },
            { PlaneRectangularCoordinateSystemNumber.No2, "2系" },
            { PlaneRectangularCoordinateSystemNumber.No3, "3系" },
            { PlaneRectangularCoordinateSystemNumber.No4, "4系" },
            { PlaneRectangularCoordinateSystemNumber.No5, "5系" },
            { PlaneRectangularCoordinateSystemNumber.No6, "6系" },
            { PlaneRectangularCoordinateSystemNumber.No7, "7系" },
            { PlaneRectangularCoordinateSystemNumber.No8, "8系" },
            { PlaneRectangularCoordinateSystemNumber.No9, "9系" },
            { PlaneRectangularCoordinateSystemNumber.No10, "10系" },
            { PlaneRectangularCoordinateSystemNumber.No11, "11系" },
            { PlaneRectangularCoordinateSystemNumber.No12, "12系" },
            { PlaneRectangularCoordinateSystemNumber.No13, "13系" },
            { PlaneRectangularCoordinateSystemNumber.No14, "14系" },
            { PlaneRectangularCoordinateSystemNumber.No15, "15系" },
            { PlaneRectangularCoordinateSystemNumber.No16, "16系" },
            { PlaneRectangularCoordinateSystemNumber.No17, "17系" },
            { PlaneRectangularCoordinateSystemNumber.No18, "18系" },
            { PlaneRectangularCoordinateSystemNumber.No19, "19系" },
        };
        #endregion

        #region Win32API
        /// <summary>
        /// システムメニュー取得
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="bRevert"></param>
        /// <returns></returns>
        [DllImport(USER_32)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        /// <summary>
        /// メニュー項目の有効化・無効化
        /// </summary>
        /// <param name="hMenu"></param>
        /// <param name="uIDEnableItem"></param>
        /// <param name="uEnable"></param>
        /// <returns></returns>
        [DllImport(USER_32)]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const string USER_32 = "user32.dll";
        private const uint SC_CLOSE = 0xF060;
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        #endregion

        #region プロパティ
        /// <summary>
        /// 接続設定を取得または設定します。
        /// </summary>
        /// <remarks>
        /// 通信設定画面のプロパティと参照状態にすることにより、通信設定画面では書き込み可能かつ、
        /// 再代入不要なリアルタイム更新を行います。
        /// </remarks>
        private ConnectionSetting ConnectionSetting { get; } = ConnectionSetting.Load();

        /// <summary>
        /// 平面直角座標系番号を取得または設定します。
        /// </summary>
        private PlaneRectangularCoordinateSystemNumber GridSystemNumber { get; set; } = PlaneRectangularCoordinateSystemNumber.No9;

        /// <summary>
        /// TCP受信サービスを取得します。
        /// </summary>
        private TcpReceiveService TcpReceiveService { get; } = new();

        /// <summary>
        /// 受信処理のキャンセルトークンソースを取得または設定します。
        /// </summary>
        private CancellationTokenSource? ReceiveCancellationSource { get; set; }

        /// <summary>
        /// 受信フォーマット別変換処理を実装します。
        /// </summary>
        private Dictionary<ReceiveFormat, Func<ReceiveParseParam, (bool Result ,double Latitude, double Longitude)>> ReceiveFunctions = new()
        {
            { ReceiveFormat.GeoPoint, GeoPointToGeoPoint },
            { ReceiveFormat.JapanGrid, JapanGridToGeoPoint },
            { ReceiveFormat.GeoPointJson, GeoPointJsonToGeoPoint },
            { ReceiveFormat.JapanGridJson, JapanGridJsonToGeoPoint },
            { ReceiveFormat.NmeaFormatGga, GgaToGeoPoint },
            { ReceiveFormat.NmeaFormatRmc, RmcToGeoPoint }
        };
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            TcpReceiveService.DataReceived += OnDataReceived;
        }
        #endregion

        #region イベントハンドラ
        #region Window_SourceInitialized
        /// <summary>
        /// ウィンドウハンドル生成完了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // 閉じる「×」ボタン無効化
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var hMenu = GetSystemMenu(hwnd, false);
            if (hMenu != IntPtr.Zero)
                EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
        }
        #endregion

        #region Window_Loaded
        /// <summary>
        /// 画面読み込み終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnStopReceive.Visibility = Visibility.Collapsed;
            cmbGridSystemNumber.ItemsSource = GIRD_SYSTEM_NO_NAME_MAP;
            cmbGridSystemNumber.SelectedValue = PlaneRectangularCoordinateSystemNumber.No9;
            // 地図初期化
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: USER_DATA_DIR);
            await wvMap.EnsureCoreWebView2Async(environment);
            wvMap.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(MAIN_MAP_HTML_NAME);
            using var reader = new StreamReader(stream!, Encoding.UTF8);
            var html = reader.ReadToEnd();
            wvMap.NavigateToString(html);
        }
        #endregion

        #region btnStartReceive_Click
        /// <summary>
        /// 受信開始ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnStartReceive_Click(object sender, RoutedEventArgs e)
        {
            btnStartReceive.Visibility = Visibility.Collapsed;
            btnStopReceive.Visibility = Visibility.Visible;
            cmbGridSystemNumber.IsEnabled = false;
            btnConnectionSetting.IsEnabled = false;
            ReceiveCancellationSource = new CancellationTokenSource();
            try
            {
                if (ConnectionSetting.CommunicationMethod == CommunicationMethod.Tcp)
                    await TcpReceiveService.RunAsync(ConnectionSetting.PortNumber, ReceiveCancellationSource.Token);
                else
                    await ReceiveUdpAsync(ConnectionSetting.PortNumber, ReceiveCancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                // 停止ボタンによる正常終了のため、何もしない。
            }
            finally
            {
                ReceiveCancellationSource?.Dispose();
                ReceiveCancellationSource = null;
                btnStopReceive.Visibility = Visibility.Collapsed;
                btnStartReceive.Visibility = Visibility.Visible;
                cmbGridSystemNumber.IsEnabled = true;
                btnConnectionSetting.IsEnabled = true;
            }
        }
        #endregion

        #region btnStopReceive_Click
        /// <summary>
        /// 受信停止ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStopReceive_Click(object sender, RoutedEventArgs e) => ReceiveCancellationSource?.Cancel();
        #endregion

        #region cmbGridSystemNumber_SelectionChanged
        /// <summary>
        /// 平面直角座標系番号変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbGridSystemNumber_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbGridSystemNumber.SelectedValue is PlaneRectangularCoordinateSystemNumber gridSystemNumber)
                GridSystemNumber = gridSystemNumber;
        }
        #endregion

        #region btnOpenCsvFolder_Click
        /// <summary>
        /// 出力フォルダを開くボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenCsvFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("EXPLORER.EXE", Path.Combine(USER_DATA_DIR, "CSV"));
            }
            catch
            {
                MsgBox.ShowError("受信結果出力フォルダが存在しません。");
            }
        }
        #endregion

        #region btnConnectionSetting_Click
        /// <summary>
        /// 通信設定ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnectionSetting_Click(object sender, RoutedEventArgs e) =>
            new ConnectionSettingWindow(ConnectionSetting) { Owner = this }.ShowDialog();
        #endregion

        #region Window_Closing
        /// <summary>
        /// 画面終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => e.Cancel = true;
        #endregion

        #region btnExit_Click
        /// <summary>
        /// 終了ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            List<string> messages = [$"{Title}を終了します。", "よろしいですか？"];
            if (MsgBox.ShowConfirm(messages, "システム終了") == MessageBoxResult.Yes)
            {
                ReceiveCancellationSource?.Cancel();
                Application.Current.Shutdown();
            }
        }
        #endregion

        #region OnDataReceived
        /// <summary>
        /// メッセージ受信イベント
        /// </summary>
        /// <param name="message"></param>
        private void OnDataReceived(string message)
        {
            ReceiveParseParam param = new()
            {
                Message = message,
                GridSystemNumber = GridSystemNumber
            };
            Dispatcher.InvokeAsync(async () =>
            {
                var ret = ReceiveFunctions[ConnectionSetting.ReceiveFormat](param);
                if (!ret.Result) return;
                await wvMap.ExecuteScriptAsync($"moveMobile({ret.Latitude}, {ret.Longitude})");
                await wvMap.ExecuteScriptAsync($"centerMap({ret.Latitude}, {ret.Longitude})");
                if (ConnectionSetting.IsOutput) OutputLatLng(ret.Latitude, ret.Longitude);
            });
        }
        #endregion
        #endregion

        #region 内部メソッド
        #region GeoPointToGeoPoint
        /// <summary>
        /// 受信した緯度・経度文字を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) GeoPointToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                var latlng = param.Message.Trim().Split(',');
                if (latlng.Length > 1)
                {
                    var lat = latlng[0].ToDouble();
                    var lng = latlng[1].ToDouble();
                    if (lat.HasValue && lng.HasValue)
                    {
                        latitude = lat.Value;
                        longitude = lng.Value;
                        result = true;
                    }
                }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region JapanGridToGeoPoint
        /// <summary>
        /// 受信した平面直角座標文字を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) JapanGridToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                var xy = param.Message.Trim().Split(',');
                if (xy.Length > 1)
                {
                    var x = xy[0].ToDouble();
                    var y = xy[1].ToDouble();
                    if (x.HasValue && y.HasValue)
                    {
                        var jg = new JapanGrid(x.Value, y.Value);
                        var geoPoint = CoordConverter.ToGeoPoint(jg, param.GridSystemNumber);
                        if (geoPoint != null)
                        {
                            latitude = geoPoint.Latitude;
                            longitude = geoPoint.Longitude;
                            result = true;
                        }
                    }
                }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region GeoPointJsonToGeoPoint
        /// <summary>
        /// 受信した緯度・経度文字を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) GeoPointJsonToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                try
                {
                    using var doc = JsonDocument.Parse(param.Message);
                    var root = doc.RootElement;
                    latitude = root.GetProperty("Latitude").GetDouble();
                    longitude = root.GetProperty("Longitude").GetDouble();

                    result = true;
                }
                catch { }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region JapanGridJsonToGeoPoint
        /// <summary>
        /// 受信した平面直角座標文字を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) JapanGridJsonToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                try
                {
                    using var doc = JsonDocument.Parse(param.Message);
                    var root = doc.RootElement;
                    var x = root.GetProperty("X").GetDouble();
                    var y = root.GetProperty("Y").GetDouble();
                    var jg = new JapanGrid(x, y);
                    var geoPoint = CoordConverter.ToGeoPoint(jg, param.GridSystemNumber);
                    if (geoPoint != null)
                    {
                        latitude = geoPoint.Latitude;
                        longitude = geoPoint.Longitude;
                        result = true;
                    }
                }
                catch { }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region GgaToGeoPoint
        /// <summary>
        /// 受信したNMEAフォーマット(GGA)を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) GgaToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                var items = param.Message.Split(',');
                if (items.Length > 4)
                {
                    latitude = items[2].ToDms(true);
                    longitude = items[4].ToDms(false);
                    result = true;
                }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region RmcToGeoPoint
        /// <summary>
        /// 受信したNMEAフォーマット(RMC)を緯度・経度に変換します。
        /// </summary>
        /// <param name="param">受信データ変換パラメータ</param>
        /// <returns>緯度・経度</returns>
        private static (bool Result, double Latitude, double Longitude) RmcToGeoPoint(ReceiveParseParam param)
        {
            var result = false;
            var latitude = 0d;
            var longitude = 0d;
            if (!string.IsNullOrWhiteSpace(param.Message))
            {
                var items = param.Message.Split(',');
                if (items.Length > 5)
                {
                    latitude = items[3].ToDms(true);
                    longitude = items[5].ToDms(false);
                    result = true;
                }
            }

            return (result, latitude, longitude);
        }
        #endregion

        #region ReceiveUdpAsync
        /// <summary>
        /// UDP受信を行います。
        /// </summary>
        /// <remarks>
        /// 送信元がブロードキャストで送ってくる前提のため、特定IPにはバインドせず0.0.0.0固定で受信します。
        /// </remarks>
        /// <param name="port">受信ポート番号</param>
        /// <param name="token">キャンセルトークン</param>
        private async Task ReceiveUdpAsync(int port, CancellationToken token)
        {
            UdpClient? client = null;
            try
            {
                client = new UdpClient(port);
                while (!token.IsCancellationRequested)
                {
                    var result = await client.ReceiveAsync(token);
                    var message = Encoding.ASCII.GetString(result.Buffer).TrimEnd('\r', '\n');
                    if (!string.IsNullOrEmpty(message)) OnDataReceived(message);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Error("UDP受信処理に失敗しました。", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
        #endregion

        #region OutputLatLng
        /// <summary>
        /// 緯度・経度CSVを出力します。
        /// </summary>
        /// <param name="latitude">緯度</param>
        /// <param name="longitude">経度</param>
        private void OutputLatLng(double latitude, double longitude)
        {
            var path = Path.Combine(USER_DATA_DIR, "CSV", $"latlng_{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv");
            try
            {
                lock (_lockCsv)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.AppendAllText(path, $"{latitude},{longitude}{Environment.NewLine}", new UTF8Encoding(false));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        #endregion
        #endregion
    }
}
using GnssTester.Models;
using System.Windows;

namespace GnssTester.Views
{
    /// <summary>
    /// 通信設定画面
    /// </summary>
    public partial class ConnectionSettingWindow : Window
    {
        #region プロパティ
        /// <summary>
        /// 接続設定を取得または設定します。
        /// </summary>
        private ConnectionSetting ConnectionSetting { get; set; }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConnectionSettingWindow(ConnectionSetting connectionSetting)
        {
            InitializeComponent();
            ConnectionSetting = connectionSetting;
        }
        #endregion

        #region イベントハンドラ
        #region Window_Loaded
        /// <summary>
        /// 画面読み込み終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // コンボボックスバインド
            Dictionary<CommunicationMethod, string> communicationMethodItemsSource = [];
            foreach (var communicationMethod in Enum.GetValues<CommunicationMethod>())
                communicationMethodItemsSource[communicationMethod] = communicationMethod.GetName();
            cmbCommunicationMethod.ItemsSource = communicationMethodItemsSource;
            Dictionary<ReceiveFormat, string> receiveFormatItemsSource = [];
            foreach (var sendFormat in Enum.GetValues<ReceiveFormat>())
                receiveFormatItemsSource[sendFormat] = sendFormat.GetName();
            cmbReceiveFormat.ItemsSource = receiveFormatItemsSource;
            // 値の設定
            cmbCommunicationMethod.SelectedValue = ConnectionSetting.CommunicationMethod;
            numPortNumber.Value = ConnectionSetting.PortNumber;
            cmbReceiveFormat.SelectedValue = ConnectionSetting.ReceiveFormat;
            chkOutput.IsChecked = ConnectionSetting.IsOutput;
        }
        #endregion

        #region btnUpdate_Click
        /// <summary>
        /// 更新ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            List<string> messages = [$"{Title}を更新します。", "よろしいですか？"];
            if (MsgBox.ShowConfirm(messages) != MessageBoxResult.Yes) return;
            if (cmbCommunicationMethod.SelectedValue is CommunicationMethod communicationMethod)
                ConnectionSetting.CommunicationMethod = communicationMethod;
            ConnectionSetting.PortNumber = numPortNumber.Value ?? 1024;
            if (cmbReceiveFormat.SelectedValue is ReceiveFormat receiveFormat)
                ConnectionSetting.ReceiveFormat = receiveFormat;
            ConnectionSetting.IsOutput = chkOutput.IsChecked ?? false;

            ConnectionSetting.Save(ConnectionSetting);
            MsgBox.ShowInfo($"{Title}を更新しました。");

            DialogResult = true;
        }
        #endregion

        #region btnClose_Click
        /// <summary>
        /// 閉じるボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, RoutedEventArgs e) => Close();
        #endregion

        #region Window_Closing
        /// <summary>
        /// 画面終了イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!(DialogResult ?? false))
            {
                List<string> messages = [$"更新せずに{Title}を閉じます。", "よろしいですか？"];
                if (MsgBox.ShowConfirm(messages) != MessageBoxResult.Yes) e.Cancel = true;
            }
        }
        #endregion
        #endregion
    }
}

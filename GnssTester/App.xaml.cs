using System.Windows;

namespace GnssTester
{
    /// <summary>
    /// GNSS通信検証ツール
    /// </summary>
    public partial class App : Application
    {
        #region メンバ変数
        /// <summary>
        /// 多重起動抑止用
        /// </summary>
        private Mutex? _mutex;

        /// <summary>
        /// 新たにMutexを作成できたか？
        /// </summary>
        private bool _isCreatedNew;
        #endregion

        #region オーバーライド
        /// <summary>
        /// OnStartup
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _mutex = new Mutex(true, nameof(GnssTester), out _isCreatedNew);
            if (!_isCreatedNew)
            {
                Shutdown();
                return;
            }
        }

        /// <summary>
        /// OnExit
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_isCreatedNew)
                _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
        #endregion
    }
}

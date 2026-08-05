using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace GnssTester.UserControls
{
    /// <summary>
    /// 符号なし整数専用テキストボックス
    /// </summary>
    internal class UIntTextBox : TextBox
    {
        #region 依存関係プロパティ
        #region UIntText
        /// <summary>
        /// 数値文字列を取得または設定します。
        /// </summary>
        public string UIntText
        {
            get => (string)GetValue(UIntTextProperty);
            set => SetValue(UIntTextProperty, value);
        }

        /// <summary>
        /// 数値文字列の依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty UIntTextProperty = DependencyProperty.Register(
            nameof(UIntText),
            typeof(string),
            typeof(UIntTextBox),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Journal,
                OnUIntTextChanged,
                CoerceUIntText,
                true,
                UpdateSourceTrigger.LostFocus));

        /// <summary>
        /// 数値として解釈できない値が設定された場合に0へ補正します。
        /// </summary>
        /// <param name="d">UIntTextBoxコントロール</param>
        /// <param name="baseValue">設定しようとしている値</param>
        /// <returns>補正後の値</returns>
        private static object CoerceUIntText(DependencyObject d, object baseValue)
        {
            var text = baseValue as string ?? string.Empty;
            return uint.TryParse(text, out _) ? text : "0";
        }
        #endregion

        #region MinValue
        /// <summary>
        /// 最小値を取得または設定します。
        /// </summary>
        public uint MinValue
        {
            get => (uint)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        /// <summary>
        /// 最小値の依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            nameof(MinValue),
            typeof(uint),
            typeof(UIntTextBox),
            new PropertyMetadata(uint.MinValue));
        #endregion

        #region MaxValue
        /// <summary>
        /// 最大値を取得または設定します。
        /// </summary>
        public uint MaxValue
        {
            get => (uint)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        /// <summary>
        /// MaxValueの依存関係プロパティ
        /// </summary>
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            nameof(MaxValue),
            typeof(uint),
            typeof(UIntTextBox),
            new PropertyMetadata(uint.MaxValue));
        #endregion
        #endregion

        #region コンストラクタ
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <remarks>
        /// XAMLを持たないコントロールのため、入力制限・貼り付け検証のイベントをここで明示的に登録します。
        /// </remarks>
        public UIntTextBox()
        {
            PreviewTextInput += TextBox_PreviewTextInput;
            AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(TextBox_PreviewExecuted));
        }
        #endregion

        #region オーバーライド
        #region OnTextChanged
        /// <summary>
        /// テキスト変更時の処理を行います。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            UIntText = Text;
        }
        #endregion

        #region OnGotFocus
        /// <summary>
        /// フォーカスを得た時に、テキストを全選択状態にします。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            SelectAll();
        }
        #endregion

        #region OnLostFocus
        /// <summary>
        /// フォーカスが外れた際の範囲チェックを行います。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            if (uint.TryParse(Text, out var value))
            {
                var clamped = Math.Clamp(value, MinValue, MaxValue);
                if (clamped != value) Text = clamped.ToString();
            }
            else
            {
                Text = Math.Clamp(0u, MinValue, MaxValue).ToString();
            }
        }
        #endregion
        #endregion

        #region イベントハンドラ
        #region TextBox_PreviewTextInput
        /// <summary>
        /// テキストボックスキー入力確定前イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// 入力の制限、数字だけ通す
        /// </remarks>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!new System.Text.RegularExpressions.Regex(@"^[0-9]$").IsMatch(e.Text))
                e.Handled = true;
        }
        #endregion

        #region TextBox_PreviewExecuted
        /// <summary>
        /// テキスト実行前イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// 数値として解釈できない、または範囲外の値の貼り付けを無効にする
        /// </remarks>
        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (uint.TryParse(Clipboard.GetText(), out uint value))
                {
                    if (value < MinValue || MaxValue < value)
                        e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                }
            }
        }
        #endregion
        #endregion

        #region 内部メソッド
        #region OnUIntTextChanged
        /// <summary>
        /// UIntText変更時にテキストへ値を反映するかどうかを判定します。
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnUIntTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIntTextBox textBox) return;

            var currentText = textBox.Text;
            var newText = (string)e.NewValue;
            if (currentText == newText) return;

            if (uint.TryParse(currentText, out var currentUInt) &&
                uint.TryParse(newText, out var newUInt) &&
                currentUInt == newUInt) return;

            textBox.Text = newText;
        }
        #endregion
        #endregion
    }
}

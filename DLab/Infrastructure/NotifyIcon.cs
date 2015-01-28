using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace DLab.Infrastructure
{
    public enum BalloonTipIcon
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

	[ContentProperty("Text")]
	[DefaultEvent("MouseDoubleClick")]
	public partial class NotifyIcon : FrameworkElement, IAddChild
	{
		public static readonly RoutedEvent MouseClickEvent = EventManager.RegisterRoutedEvent(
			"MouseClick",
			RoutingStrategy.Bubble,
			typeof(MouseButtonEventHandler),
			typeof(NotifyIcon));

		public static readonly RoutedEvent MouseDoubleClickEvent = EventManager.RegisterRoutedEvent(
			"MouseDoubleClick",
			RoutingStrategy.Bubble,
			typeof(MouseButtonEventHandler),
			typeof(NotifyIcon));

		public static readonly DependencyProperty BalloonTipIconProperty =
			DependencyProperty.Register("BalloonTipIcon", typeof(BalloonTipIcon), typeof(NotifyIcon));

		public static readonly DependencyProperty BalloonTipTextProperty =
			DependencyProperty.Register("BalloonTipText", typeof(string), typeof(NotifyIcon));

		public static readonly DependencyProperty BalloonTipTitleProperty =
			DependencyProperty.Register("BalloonTipTitle", typeof(string), typeof(NotifyIcon));

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
			"Icon",
			typeof(ImageSource),
			typeof(NotifyIcon),
			new FrameworkPropertyMetadata(OnIconChanged));

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
			"Text",
			typeof(string),
			typeof(NotifyIcon),
			new PropertyMetadata(OnTextChanged));

		private System.Windows.Forms.NotifyIcon notifyIcon;

		static NotifyIcon()
		{
			VisibilityProperty.OverrideMetadata(typeof(NotifyIcon), new PropertyMetadata(OnVisibilityChanged));
		}

		public event MouseButtonEventHandler MouseClick
		{
			add { this.AddHandler(MouseClickEvent, value); }
			remove { this.RemoveHandler(MouseClickEvent, value); }
		}

		public event MouseButtonEventHandler MouseDoubleClick
		{
			add { this.AddHandler(MouseDoubleClickEvent, value); }
			remove { this.RemoveHandler(MouseDoubleClickEvent, value); }
		}

		public BalloonTipIcon BalloonTipIcon
		{
			get { return (BalloonTipIcon)this.GetValue(BalloonTipIconProperty); }
			set { this.SetValue(BalloonTipIconProperty, value); }
		}

		public string BalloonTipText
		{
			get { return (string)this.GetValue(BalloonTipTextProperty); }
			set { this.SetValue(BalloonTipTextProperty, value); }
		}

		public string BalloonTipTitle
		{
			get { return (string)this.GetValue(BalloonTipTitleProperty); }
			set { this.SetValue(BalloonTipTitleProperty, value); }
		}

		public ImageSource Icon
		{
			get { return (ImageSource)this.GetValue(IconProperty); }
			set { this.SetValue(IconProperty, value); }
		}

		public string Text
		{
			get { return (string)this.GetValue(TextProperty); }
			set { this.SetValue(TextProperty, value); }
		}

		public override void BeginInit()
		{
			base.BeginInit();
			this.InitializeNotifyIcon();
		}

		public void ShowBalloonTip(int timeout)
		{
			this.notifyIcon.BalloonTipTitle = this.BalloonTipTitle;
			this.notifyIcon.BalloonTipText = this.BalloonTipText;
			this.notifyIcon.BalloonTipIcon = (System.Windows.Forms.ToolTipIcon)this.BalloonTipIcon;
			this.notifyIcon.ShowBalloonTip(timeout);
		}

		public void ShowBalloonTip(int timeout, string tipTitle, string tipText, BalloonTipIcon tipIcon)
		{
			this.notifyIcon.ShowBalloonTip(timeout, tipTitle, tipText, (System.Windows.Forms.ToolTipIcon)tipIcon);
		}

		#region IAddChild Members

		void IAddChild.AddChild(object value)
		{
			throw new InvalidOperationException();
		}

		void IAddChild.AddText(string text)
		{
			if (text == null)
			{
				throw new ArgumentNullException("text");
			}

			this.Text = text;
		}

		#endregion

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			this.AttachToWindowClose();
		}

		private static MouseButtonEventArgs CreateMouseButtonEventArgs(
			RoutedEvent handler,
			System.Windows.Forms.MouseButtons button)
		{
			return new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, ToMouseButton(button))
			{
				RoutedEvent = handler
			};
		}

		private static System.Drawing.Icon FromImageSource(ImageSource icon)
		{
			if (icon == null)
			{
				return null;
			}

			Uri iconUri = new Uri(icon.ToString());

		    if (iconUri.LocalPath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
		    {
		        var bm = new Bitmap(Application.GetResourceStream(iconUri).Stream);
		        var hnd = bm.GetHicon();
		        return System.Drawing.Icon.FromHandle(hnd);
		    }

			return new Icon(Application.GetResourceStream(iconUri).Stream);
		}

		private static void OnIconChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			if (!DesignerProperties.GetIsInDesignMode(target))
			{
				NotifyIcon control = (NotifyIcon)target;
				control.notifyIcon.Icon = FromImageSource(control.Icon);
			}
		}

		private static void OnTextChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			NotifyIcon control = (NotifyIcon)target;
			control.notifyIcon.Text = control.Text;
		}

		private static void OnVisibilityChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			NotifyIcon control = (NotifyIcon)target;
			control.notifyIcon.Visible = control.Visibility == Visibility.Visible;
		}

		private static MouseButton ToMouseButton(System.Windows.Forms.MouseButtons button)
		{
			switch (button)
			{
				case System.Windows.Forms.MouseButtons.Left:
					return MouseButton.Left;
				case System.Windows.Forms.MouseButtons.Right:
					return MouseButton.Right;
				case System.Windows.Forms.MouseButtons.Middle:
					return MouseButton.Middle;
				case System.Windows.Forms.MouseButtons.XButton1:
					return MouseButton.XButton1;
				case System.Windows.Forms.MouseButtons.XButton2:
					return MouseButton.XButton2;
			}

			throw new InvalidOperationException();
		}

		private void AttachToWindowClose()
		{
			var window = Window.GetWindow(this);
			if (window != null)
			{
				window.Closed += (s, a) => this.notifyIcon.Dispose();
			}
		}

		private void InitializeNotifyIcon()
		{
			this.notifyIcon = new System.Windows.Forms.NotifyIcon();
			this.notifyIcon.Text = this.Text;
			this.notifyIcon.Icon = FromImageSource(this.Icon);
			this.notifyIcon.Visible = this.Visibility == Visibility.Visible;

			this.notifyIcon.MouseDown += this.OnMouseDown;
			this.notifyIcon.MouseUp += this.OnMouseUp;
			this.notifyIcon.MouseClick += this.OnMouseClick;
			this.notifyIcon.MouseDoubleClick += this.OnMouseDoubleClick;

			this.InitializeNativeHooks();
		}

		private void OnMouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			this.RaiseEvent(CreateMouseButtonEventArgs(MouseDownEvent, e.Button));
		}

		private void OnMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			this.RaiseEvent(CreateMouseButtonEventArgs(MouseDoubleClickEvent, e.Button));
		}

		private void OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			this.RaiseEvent(CreateMouseButtonEventArgs(MouseClickEvent, e.Button));
		}

		private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Right)
			{
				this.ShowContextMenu();
			}

			this.RaiseEvent(CreateMouseButtonEventArgs(MouseUpEvent, e.Button));
		}

		private void ShowContextMenu()
		{
			if (this.ContextMenu != null)
			{
				this.AttachContextMenu();
				this.ContextMenu.IsOpen = true;
			}
		}

		partial void AttachContextMenu();

		partial void InitializeNativeHooks();
	}
}
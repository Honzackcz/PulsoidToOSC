using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PulsoidToOSC
{
	public partial class ColorPickerWindow : Window
	{
		private const int BitMapSVSize = 32;
		private const int BitMapHSize = 256;
		private const int CanvasSize = 256;
		private WriteableBitmap? _bitmapH;
		private WriteableBitmap? _bitmapSV;
		private Rectangle? _slectionRectangle;
		private Ellipse? _selectionEllipse;

		private double _hue = 0;
		private double _saturation = 1;
		private double _value = 1;

		public Color Color { get; private set; }

		public ColorPickerWindow()
		{
			InitializeComponent();

			InitializeBitmaps();
			CreatePickers();
			UpdateHueImage();
			UpdateValueSaturationImage();
		}

		public void SetColor(Color color)
		{
			Color = color;

			ColorToHSV(Color, out _hue, out _saturation, out _value);
			ColorIndicator.Background = new SolidColorBrush(Color);
			UpdateValueSaturationImage();
			UpdatePickersPosision();
		}

		private void InitializeBitmaps()
		{
			_bitmapH = new WriteableBitmap(BitMapHSize, 1, 32, 1, PixelFormats.Rgb24, null);
			HueImage.Source = _bitmapH;

			_bitmapSV = new WriteableBitmap(BitMapSVSize, BitMapSVSize, 32, 32, PixelFormats.Rgb24, null);
			ValueSaturationImage.Source = _bitmapSV;
		}

		private void CreatePickers()
		{
			_slectionRectangle = new Rectangle
			{
				Width = 6,
				Height = 32,
				Stroke = Brushes.Black,
				StrokeThickness = 2
			};
			Canvas.SetLeft(_slectionRectangle, 0);
			Canvas.SetTop(_slectionRectangle, 0);
			HueCanvas.Children.Add(_slectionRectangle);

			_selectionEllipse = new Ellipse
			{
				Width = 10,
				Height = 10,
				Stroke = Brushes.Black,
				StrokeThickness = 2
			};
			Canvas.SetLeft(_selectionEllipse, 0);
			Canvas.SetTop(_selectionEllipse, 0);
			ValueSaturationCanvas.Children.Add(_selectionEllipse);
		}

		private void UpdatePickersPosision()
		{
			Canvas.SetLeft(_slectionRectangle, (_hue / 360) * CanvasSize - (_slectionRectangle?.Width ?? 0) / 2);

			Canvas.SetLeft(_selectionEllipse, _saturation * CanvasSize - (_selectionEllipse?.Width ?? 0) / 2);
			Canvas.SetTop(_selectionEllipse, (1 - _value) * CanvasSize - (_selectionEllipse?.Height ?? 0) / 2);
			if (_selectionEllipse != null) _selectionEllipse.Stroke = _value > 0.5 ? Brushes.Black : Brushes.White;
		}

		private void UpdateHueImage()
		{
			int stride = BitMapHSize * 3;
			byte[] pixels = new byte[BitMapHSize * 3];

			for (int x = 0; x < BitMapHSize; x++)
			{
				double hue = (x / (double)BitMapHSize) * 360;

				Color color = ColorFromHSV(hue, 1, 1);

				int index = x * 3;
				pixels[index + 0] = color.R;
				pixels[index + 1] = color.G;
				pixels[index + 2] = color.B;
			}
			
			_bitmapH?.WritePixels(new Int32Rect(0, 0, BitMapHSize, 1), pixels, stride, 0);
		}

		private void UpdateValueSaturationImage()
		{
			int stride = BitMapSVSize * 3;
			byte[] pixels = new byte[BitMapSVSize * BitMapSVSize * 3];

			for (int y = 0; y < BitMapSVSize; y++)
			{
				for (int x = 0; x < BitMapSVSize; x++)
				{
					double saturation = x / (double)BitMapSVSize;
					double value = 1 - y / (double)BitMapSVSize;

					Color color = ColorFromHSV(_hue, saturation, value);

					int index = (y * BitMapSVSize + x) * 3;
					pixels[index + 0] = color.R;
					pixels[index + 1] = color.G;
					pixels[index + 2] = color.B;
				}
			}

			_bitmapSV?.WritePixels(new Int32Rect(0, 0, BitMapSVSize, BitMapSVSize), pixels, stride, 0);
		}

		private void HueCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point position = e.GetPosition(HueImage);
				if (position.X >= 0 && position.X < CanvasSize && position.Y >= 0 && position.Y < 32)
				{
					Canvas.SetLeft(_slectionRectangle, position.X - (_slectionRectangle?.Width ?? 0) / 2);

					_hue = (position.X / CanvasSize) * 360;

					UpdateValueSaturationImage();

					Color selectedColor = ColorFromHSV(_hue, _saturation, _value);
					Color = selectedColor;
					ColorIndicator.Background = new SolidColorBrush(selectedColor);
				}
			}
		}

		private void ValueSaturationCanvas_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				Point position = e.GetPosition(ValueSaturationImage);
				if (position.X >= 0 && position.X < CanvasSize && position.Y >= 0 && position.Y < CanvasSize)
				{
					Canvas.SetLeft(_selectionEllipse, position.X - (_selectionEllipse?.Width ?? 0) / 2);
					Canvas.SetTop(_selectionEllipse, position.Y - (_selectionEllipse?.Height ?? 0) / 2);

					_saturation = position.X / CanvasSize;
					_value = 1 - position.Y / CanvasSize;

					if (_selectionEllipse != null) _selectionEllipse.Stroke = _value > 0.5 ? Brushes.Black : Brushes.White;
					Color selectedColor = ColorFromHSV(_hue, _saturation, _value);
					Color = selectedColor;
					ColorIndicator.Background = new SolidColorBrush(selectedColor);
				}
			}
		}

		private static Color ColorFromHSV(double hue, double saturation, double value)
		{
			int hi = (int)(hue / 60) % 6;
			double f = hue / 60 - hi;
			value *= 255;
			byte v = (byte)value;
			byte p = (byte)(value * (1 - saturation));
			byte q = (byte)(value * (1 - f * saturation));
			byte t = (byte)(value * (1 - (1 - f) * saturation));

			return hi switch
			{
				0 => Color.FromRgb(v, t, p),
				1 => Color.FromRgb(q, v, p),
				2 => Color.FromRgb(p, v, t),
				3 => Color.FromRgb(p, q, v),
				4 => Color.FromRgb(t, p, v),
				_ => Color.FromRgb(v, p, q),
			};
		}

		public static void ColorToHSV(Color color, out double hue, out double saturation, out double value)
		{
			// Normalize RGB values to the [0, 1] range
			double r = color.R / 255.0;
			double g = color.G / 255.0;
			double b = color.B / 255.0;

			// Find min and max RGB values
			double max = Math.Max(r, Math.Max(g, b));
			double min = Math.Min(r, Math.Min(g, b));
			double delta = max - min;

			// Calculate Hue
			if (delta == 0)
			{
				hue = 0; // Undefined, so we'll just set it to 0
			}
			else if (max == r)
			{
				hue = 60 * (((g - b) / delta) % 6);
			}
			else if (max == g)
			{
				hue = 60 * (((b - r) / delta) + 2);
			}
			else
			{
				hue = 60 * (((r - g) / delta) + 4);
			}

			if (hue < 0)
			{
				hue += 360;
			}

			// Calculate Saturation
			saturation = (max == 0) ? 0 : delta / max;

			// Calculate Value
			value = max;
		}
	}
}

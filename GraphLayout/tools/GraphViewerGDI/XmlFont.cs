using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Microsoft.Msagl.GraphViewerGdi
{
	/// <summary>
	/// XmlFont is a serializable wrapper object around System.Drawing.Font
	/// </summary>
	[Serializable]
	public class XmlFont : IDisposable
	{
		private Font _font = null;
		private string _fontName = "Verdana";
		private float _fontSize = 8;
		private FontStyle _fontStyle = FontStyle.Regular;
		private bool _strikeOut = false;
		private bool _underline = false;
		private bool _bold = false;
		private bool _italic = false;

		public XmlFont() { }

		public XmlFont(Font f)
		{
			_font = f;
			_fontName = f.Name;
			_fontSize = f.Size;
			_fontStyle = f.Style;
			_bold = f.Bold;
			_underline = f.Underline;
			_strikeOut = f.Strikeout;
			_italic = f.Italic;
		}


		public Font ToFont()
		{
			if (_font == null) _font = new Font(_fontName, _fontSize, Style);
			return _font;
		}

		public void FromFont(Font f)
		{
			_font = f;
		}

		public void Dispose()
		{
			_font.Dispose();
		}

		public static implicit operator Font(XmlFont x)
		{
			return x.ToFont();
		}

		public static implicit operator XmlFont(Font f)
		{
			return new XmlFont(f);
		}

		[XmlAttribute]
		public string Name
		{
			get { return _fontName; }
			set
			{
				try
				{
					if (_font == null || _font.Name != value)
					{
						_fontName = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, _fontStyle);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public float Size
		{
			get { return _fontSize; }
			set
			{
				try
				{
					if (_font == null || _font.Size != value)
					{
						_fontSize = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, _fontStyle);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public FontStyle Style
		{
			get
			{
				FontStyle fs = _fontStyle;
				if (_strikeOut) fs = fs | FontStyle.Strikeout;
				if (_underline) fs = fs | FontStyle.Underline;
				if (_bold) fs = fs | FontStyle.Bold;
				if (_italic) fs = fs | FontStyle.Italic;
				return fs;
			}
			set
			{
				try
				{
					if (_font == null || _font.Style != value)
					{
						_fontStyle = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, Style);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public bool StrikeOut
		{
			get { return _strikeOut; }
			set
			{
				try
				{
					if (_font == null || _font.Strikeout != value)
					{
						_strikeOut = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, Style);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public bool Underline
		{
			get { return _underline; }
			set
			{
				try
				{
					if (_font == null || _font.Underline != value)
					{
						_strikeOut = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, Style);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public bool Bold
		{
			get { return _bold; }
			set
			{
				try
				{
					if (_font == null || _font.Bold != value)
					{
						_bold = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, Style);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}

		[XmlAttribute]
		public bool Italic
		{
			get { return _italic; }
			set
			{
				try
				{
					if (_font == null || _font.Italic != value)
					{
						_italic = value;
						_font?.Dispose();
						_font = new Font(_fontName, _fontSize, Style);
					}
				}
				catch (Exception)
				{
					_font?.Dispose();
					_font = new Font("Verdana", 8);
				}
			}
		}
	}

}

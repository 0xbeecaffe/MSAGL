using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace Microsoft.Msagl.GraphViewerGdi
{
	/// <summary>
	/// Xml serialization helper class for System.Image type
	/// </summary>
	public class XmlImage
	{
		private Image image_ = null;

		public XmlImage() { }

		public XmlImage(Image img) { image_ = img; }

		public Image ToImage()
		{
			return image_;
		}

		public void FromImage(Image img)
		{
			image_ = img;
		}

		public static implicit operator Image(XmlImage x)
		{
			return x.ToImage();
		}

		public static implicit operator XmlImage(Image img)
		{
			return new XmlImage(img);
		}

		[XmlAttribute]
		public byte[] ImageBuffer
		{
			get
			{
				byte[] imageBuffer = null;
				if (image_ != null)
				{
					using (var stream = new MemoryStream())
					{
						image_.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
						imageBuffer = stream.ToArray();
					}
				}
				return imageBuffer;
			}
			set
			{
				try
				{
					image_?.Dispose();
					image_ = null;
					if (value != null)
					{
						using (MemoryStream ms = new MemoryStream())
						{
							ms.Write(value, 0, value.Length);
							image_ = Image.FromStream(ms);
						}
					}
				}
				catch (Exception)
				{
					image_ = null;
				}
			}
		}
	}
}


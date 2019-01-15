using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Msagl.GraphViewerGdi
{
	/// <summary>
	/// An abstract, base object of all Annotation objects
	/// </summary>
	public abstract class AnnotationBaseObject
	{
		#region Fields
		/// <summary>
		/// A unique object ID
		/// </summary>
		public Guid ObjectID { get; private set; } = Guid.NewGuid();
		/// <summary>
		/// Object name
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// The base rectangle of the object
		/// </summary>
		public Rectangle BaseRectangle = new Rectangle(0, 0, 0, 0);
		/// <summary>
		/// Any labels associated to this object
		/// </summary>
		public List<AnnotationLabel> Labels = new List<AnnotationLabel>();
		/// <summary>
		/// Controls if the annotation object is in the backgroud or foreground
		/// </summary>
		public AnnotationObjectLayer Layer = AnnotationObjectLayer.Background;
		#endregion

		#region Public members
		public abstract void Draw(Graphics g);

		public AnnotationLabel AddLabel(string displayText, ContentAlignment alignment = ContentAlignment.MiddleCenter)
		{
			AnnotationLabel lbl = new AnnotationLabel(this, displayText, alignment);
			Labels.Add(lbl);
			return lbl;
		}
		#endregion
	};

	/// <summary>
	/// An ellipsys object
	/// </summary>
	public class AnnotationEllipse : AnnotationBaseObject
	{
		/// <summary>
		/// The contour color of the object
		/// </summary>
		public Color ContourColor { get; set; } = Color.CornflowerBlue;

		/// <summary>
		/// The fill color of the object
		/// </summary>
		public Color? FillColor { get; set; } = null;


		private int _TransparencyLevel  = 255;
		/// <summary>
		/// The level of fill color trnsparency between 0..255
		/// </summary>
		public int TransparencyLevel
		{
			get { return _TransparencyLevel; }
			set
			{
				if (value < 0) _TransparencyLevel = 0;
				else if (value > 255) _TransparencyLevel = 255;
				else _TransparencyLevel = value;
			}
		}

		public override void Draw(Graphics g)
		{
			if (g == null) return;
			if (BaseRectangle.Width == 0 || BaseRectangle.Height == 0) return;
			// --
			if (FillColor != null)
			{
				using (SolidBrush penBrush = new SolidBrush(ContourColor))
				{
					Color fColor = Color.FromArgb(TransparencyLevel, (Color)FillColor);
					using (SolidBrush fillBrush = new SolidBrush(fColor))
					{
						g.FillEllipse(fillBrush, BaseRectangle);
					}
				}
			}
			using (SolidBrush penBrush = new SolidBrush(ContourColor))
			{
				using (Pen p = new Pen(penBrush))
				{
					g.DrawEllipse(p, BaseRectangle);
				}
			}
		}
	}

	/// <summary>
	/// A rectangle object
	/// </summary>
	public class AnnotationRectangle : AnnotationBaseObject
	{
		/// <summary>
		/// The contour color of the object
		/// </summary>
		public Color ContourColor { get; set; } = Color.CornflowerBlue;

		/// <summary>
		/// The fill color of the object
		/// </summary>
		public Color? FillColor { get; set; } = null;

		private int _TransparencyLevel = 255;
		/// <summary>
		/// The level of fill color trnsparency between 0..255
		/// </summary>
		public int TransparencyLevel
		{
			get { return _TransparencyLevel; }
			set
			{
				if (value < 0) _TransparencyLevel = 0;
				else if (value > 255) _TransparencyLevel = 255;
				else _TransparencyLevel = value;
			}
		}

		public override void Draw(Graphics g)
		{
			if (g == null) return;
			if (BaseRectangle.Width == 0 || BaseRectangle.Height == 0) return;
			// --
			if (FillColor != null)
			{
				using (SolidBrush penBrush = new SolidBrush(ContourColor))
				{
					Color fColor = Color.FromArgb(TransparencyLevel, (Color)FillColor);
					using (SolidBrush fillBrush = new SolidBrush(fColor))
					{
						g.FillRectangle(fillBrush, BaseRectangle);
					}
				}
			}
			using (SolidBrush penBrush = new SolidBrush(ContourColor))
			{
				using (Pen p = new Pen(penBrush))
				{
					g.DrawRectangle(p, BaseRectangle);
				}
			}
		}
	}

	public class AnnotationLabel : AnnotationBaseObject
	{
		#region Fields
		/// <summary>
		/// The label text
		/// </summary>
		public string DisplayText { get; set; }
		/// <summary>
		/// Label text positioning inside parent's BaseRectangle
		/// </summary>
		public ContentAlignment Alignment { get; set; } = ContentAlignment.MiddleCenter;
		/// <summary>
		/// The parent object of the label, may only be set via the constructor
		/// </summary>
		private AnnotationBaseObject Parent;
		#endregion

		#region Constructors
		public AnnotationLabel(AnnotationBaseObject parent, string displayText, ContentAlignment alignement = ContentAlignment.MiddleCenter)
		{
			this.Parent = parent;
			this.DisplayText = displayText;
			this.Alignment = alignement;
		}
		#endregion

		#region Public members
		public override void Draw(Graphics g)
		{
			throw new NotImplementedException();
		}
		#endregion
	}

	/// <summary>
	/// Position of the annotation objext in Z order
	/// </summary>
	public enum AnnotationObjectLayer { Background, Foreground }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Xml.Serialization;
using Point = System.Drawing.Point;

namespace Microsoft.Msagl.GraphViewerGdi.Annotation
{
	/// <summary>
	/// An abstract, base object of all Annotation objects
	/// </summary>
	[Serializable]
	public abstract class AnnotationBaseObject
	{
		#region Fields
		/// <summary>
		/// A unique object ID
		/// </summary>
		public Guid ObjectID { get; set; } = Guid.NewGuid();
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

		#region Constructors
		public AnnotationBaseObject() { }
		#endregion

		#region Properties
		public virtual Point Center
		{
			get
			{
				return new Point(BaseRectangle.Left + (BaseRectangle.Right - BaseRectangle.Left) / 2, BaseRectangle.Top + (BaseRectangle.Bottom - BaseRectangle.Top) / 2);
			}
		}
		#endregion

		#region Public members
		public virtual void Draw(Graphics g)
		{
			Labels.ForEach(l => l.Draw(g));
		}

		public AnnotationLabel AddLabel(string displayText, ContentAlignment alignment = ContentAlignment.MiddleCenter)
		{
			AnnotationLabel lbl = new AnnotationLabel(this, displayText, alignment);
			Labels.Add(lbl);
			return lbl;
		}

		/// <summary>
		/// Returns the object region of the tested point 
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public abstract AnnotationObjectRegion HitRegion(Point testPoint);

		/// <summary>
		/// Returns the object region of the tested point 
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public AnnotationObjectRegion HitRegion(Microsoft.Msagl.Core.Geometry.Point testPoint)
		{
			return HitRegion(new Point((int)testPoint.X, (int)testPoint.Y));
		}

		/// <summary>
		/// Determines if testPoint is inside the object coontour
		/// </summary>
		/// <param name="testPoint"></param>
		/// <returns></returns>
		public abstract bool ContainsPoint(Point testPoint);
		#endregion
	};

	/// <summary>
	/// Assumes that this type of Annotation object has a well defined contour
	/// </summary>
	[Serializable]
	public abstract class FramedAnnotationObject : AnnotationBaseObject
	{
		/// <summary>
		/// The contour color of the object
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color FrameColor { get; set; } = Color.CornflowerBlue;

		public int FrameWidth { get; set; } = 1;

		/// <summary>
		/// The fill color of the object
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color FillColor { get; set; } = Color.AliceBlue;

		/// <summary>
		/// The fill color of the object
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color FillColor2 { get; set; } = Color.White;

		/// <summary>
		/// The gradient fill style
		/// </summary>
		public LinearGradientMode GradientMode { get; set; } = LinearGradientMode.Vertical;

		/// <summary>
		/// The fill mode for background
		/// </summary>
		public BackFillMode FillMode { get; set; } = BackFillMode.Solid;

		private int _TransparencyLevel = 100;
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

		/// <summary>
		/// Must return the GraphicPath object that is the contour of this Annotation object
		/// </summary>
		public abstract GraphicsPath Frame { get; }

		public virtual Pen DrawingPen
		{
			get
			{
				return new Pen(DrawingBrush, FrameWidth);
			}
		}

		public virtual Brush DrawingBrush
		{
			get
			{
				return new SolidBrush(FrameColor);
			}
		}

		/// <summary>
		/// Draws the object using the Contour path
		/// </summary>
		/// <param name="g"></param>
		public override void Draw(Graphics g)
		{
			if (g == null) return;
			if (BaseRectangle.Width == 0 || BaseRectangle.Height == 0) return;
			// --
			switch (FillMode)
			{
				case BackFillMode.None: break;
				case BackFillMode.Solid:
					{
						Color fColor = Color.FromArgb(TransparencyLevel, FillColor);
						using (SolidBrush fillBrush = new SolidBrush(fColor))
						{
							g.FillPath(fillBrush, Frame);
						}
						break;
					}
				case BackFillMode.Gradient:
					{
						Color startColor = Color.FromArgb(TransparencyLevel, FillColor);
						Color endColor = Color.FromArgb(TransparencyLevel, FillColor2);
						using (LinearGradientBrush fillBrush = new LinearGradientBrush(BaseRectangle, startColor, endColor, GradientMode))
						{
							g.FillPath(fillBrush, Frame);
						}
						break;
					}
			}
			if (FrameWidth > 0)
			{
				using (var p = DrawingPen)
				{
					g.DrawPath(p, Frame);
				}
			}

			base.Draw(g);
		}

		public override AnnotationObjectRegion HitRegion(Point testPoint)
		{
			if (ContainsPoint(testPoint))
			{
				using (Pen hitTestPen = new Pen(Brushes.Black, 30))
				{
					Point c = Center;
					// The vector pointing from Center to TestPoint vCT = vT - vC
					var vCT = Vector.Subtract(new Vector(testPoint.X, testPoint.Y), new Vector(c.X, c.Y));
					// The angle of vCT will deterine the region. Runs from -180 to +180, 0 bein the right side
					double vCTangle = Vector.AngleBetween(new Vector(10, 0), vCT);
					// get the angles for the 4 cournes of baserectangle : TL, TR, BL, BR
					double[] cornerAngles = new double[4] { 0, 0, 0, 0 };
					// consider that the fucking coordinate system is upside down => Top = Bottom
					// top-left vector
					var vTL = Vector.Subtract(new Vector(BaseRectangle.Left, BaseRectangle.Bottom), new Vector(c.X, c.Y));
					cornerAngles[0] = Vector.AngleBetween(new Vector(10, 0), vTL);
					// top-right vector
					var vTR = Vector.Subtract(new Vector(BaseRectangle.Right, BaseRectangle.Bottom), new Vector(c.X, c.Y));
					cornerAngles[1] = Vector.AngleBetween(new Vector(10, 0), vTR);
					// bottom-left vector
					var vBL = Vector.Subtract(new Vector(BaseRectangle.Left, BaseRectangle.Top), new Vector(c.X, c.Y));
					cornerAngles[2] = Vector.AngleBetween(new Vector(10, 0), vBL);
					// bottom-right vector
					var vBR = Vector.Subtract(new Vector(BaseRectangle.Right, BaseRectangle.Top), new Vector(c.X, c.Y));
					cornerAngles[3] = Vector.AngleBetween(new Vector(10, 0), vBR);

					bool onEdge = Frame.IsOutlineVisible(testPoint, hitTestPen);
					if (onEdge)
					{
						if ((vCTangle > 0 && vCTangle < cornerAngles[1]) || (vCTangle <= 0 && vCTangle > cornerAngles[3])) return AnnotationObjectRegion.EdgeRight;
						else if (vCTangle >= cornerAngles[0] || vCTangle <= cornerAngles[2]) return AnnotationObjectRegion.EdgeLeft;
						else if (vCTangle >= cornerAngles[1] && vCTangle < cornerAngles[0]) return AnnotationObjectRegion.EdgeBottom;
						else return AnnotationObjectRegion.EdgeTop;
					}
					else
					{
						if (Math.Abs(vCTangle) < 45) return AnnotationObjectRegion.BodyRight;
						else if (Math.Abs(vCTangle) > 135) return AnnotationObjectRegion.BodyLeft;
						else if (vCTangle >= 45 && vCTangle <= 135) return AnnotationObjectRegion.BodyBottom;
						else return AnnotationObjectRegion.BodyTop;
					}
				}
			}
			else return AnnotationObjectRegion.None;
		}

		public override bool ContainsPoint(Point testPoint)
		{
			using (var r = new Region(Frame))
			{
				return r.IsVisible(testPoint);
			}
		}
	}

	/// <summary>
	/// An ellipsys object
	/// </summary>
	[Serializable]
	public class AnnotationEllipse : FramedAnnotationObject
	{
		/// <summary>
		/// Returns the ellipse shaped GraphicsPath that fits BaseRectangle
		/// </summary>
		/// <returns></returns>
		public override GraphicsPath Frame
		{
			get
			{
				var path = new GraphicsPath();
				path.AddEllipse(BaseRectangle);
				return path;
			}
		}
	}

	/// <summary>
	/// A rectangle object
	/// </summary>
	[Serializable]
	public class AnnotationRectangle : FramedAnnotationObject
	{
		/// <summary>
		/// Returns the rectangle shaped GraphicsPath that fits BaseRectangle
		/// </summary>
		/// <returns></returns>
		public override GraphicsPath Frame
		{
			get
			{
				var path = new GraphicsPath();
				path.AddRectangle(BaseRectangle);
				return path;
			}
		}
	}

	[Serializable]
	public class AnnotationLabel : AnnotationRectangle
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
		/// The parent object of the label
		/// </summary>
		public AnnotationBaseObject Parent;
		#endregion

		#region Constructors
		public AnnotationLabel() : base() { }
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

		public override AnnotationObjectRegion HitRegion(Point testPoint)
		{
			throw new NotImplementedException();
		}

		public override bool ContainsPoint(Point testPoint)
		{
			return BaseRectangle.Contains(testPoint);
		}
		#endregion
	}

	/// <summary>
	/// Position of the annotation objext in Z order
	/// </summary>
	public enum AnnotationObjectLayer { Background, Foreground }

	public enum BackFillMode { None, Solid, Gradient }

	[Flags]
	public enum AnnotationObjectRegion
	{
		/// <summary>
		/// Not any region
		/// </summary>
		None = 0,
		/// <summary>
		/// On the edge of the object, left side
		/// </summary>
		EdgeLeft = 1,
		/// <summary>
		/// On the edge of the object, right side
		/// </summary>
		EdgeRight = 2,
		/// <summary>
		/// On the edge of the object, top side
		/// </summary>
		EdgeTop = 4,
		/// <summary>
		/// On the edge of the object, bottom side
		/// </summary>
		EdgeBottom = 8,
		/// <summary>
		///Edge mask, covering any side
		/// </summary>
		Edge = 15,
		/// <summary>
		/// Inside the bodyof the object, left side
		/// </summary>
		BodyLeft = 16,
		/// <summary>
		/// Inside the bodyof the object, right side
		/// </summary>
		BodyRight = 32,
		/// <summary>
		/// Inside the bodyof the object, top side
		/// </summary>
		BodyTop = 64,
		/// <summary>
		/// Inside the bodyof the object, bottom side
		/// </summary>
		BodyBottom = 128,
		Body = 240
	}

	internal struct AnnotationObjectHit
	{
		public AnnotationBaseObject aObject;
		public AnnotationObjectRegion hitRegion;

	}
}

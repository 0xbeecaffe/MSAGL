using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;
using Point = System.Drawing.Point;

namespace Microsoft.Msagl.GraphViewerGdi.Annotation
{
	/// <summary>
	/// An abstract, base object of all Annotation objects
	/// </summary>
	[Serializable]
	[XmlInclude(typeof(AnnotationEllipse))]
	[XmlInclude(typeof(AnnotationRectangle))]
	[XmlInclude(typeof(AnnotationLabel))]
	public abstract class AnnotationBaseObject : ICloneable
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
		/// The GViewer where this object belongs to
		/// </summary>
		[NonSerialized, XmlIgnore]
		public GViewer Viewer = null;
		/// <summary>
		/// The base rectangle of the object
		/// </summary>
		public virtual Rectangle BaseRectangle { get; set; } = new Rectangle(0, 0, 0, 0);
		/// <summary>
		/// Any child objects associated to this object
		/// </summary>
		public List<AnnotationBaseObject> Children = new List<AnnotationBaseObject>();
		/// <summary>
		/// Controls if the annotation object is in the backgroud or foreground
		/// </summary>
		public AnnotationObjectLayer Layer = AnnotationObjectLayer.Background;
		/// <summary>
		/// Determines if the object is auto-sized, hence user cannot resize it
		/// </summary>
		public bool FixedSize = false;
		/// <summary>
		/// Objects cannot be moved or resized when blocked
		/// </summary>
		public bool Locked = false;
		/// <summary>
		/// Determines if the object is visually selected in Viewer
		/// </summary>
		public bool Selected = false;
		/// <summary>
		/// The parent object of the label
		/// </summary>
		[NonSerialized, XmlIgnore]
		public AnnotationBaseObject Parent;
		#endregion

		#region Constructors
		public AnnotationBaseObject() { }
		#endregion

		#region Properties
		public virtual Point Center
		{
			get
			{
				return new Point((BaseRectangle.Left + BaseRectangle.Right) / 2, (BaseRectangle.Top + BaseRectangle.Bottom) / 2);
			}
		}
		#endregion

		#region Public members

		public AnnotationLabel AddLabel(string displayText, ContentAlignment alignment = ContentAlignment.MiddleCenter)
		{
			AnnotationLabel lbl = new AnnotationLabel(this, displayText, alignment);
			lbl.BaseRectangle = this.BaseRectangle;
			AddChild(lbl);
			return lbl;
		}

		public void AddChild(AnnotationBaseObject child)
		{
			child.Parent = this;
			Rectangle childBaseRectangle = BaseRectangle;

			if (child.BaseRectangle.Left < this.BaseRectangle.Left) childBaseRectangle.X = this.BaseRectangle.X;
			if (child.BaseRectangle.Top < this.BaseRectangle.Top) childBaseRectangle.Y = this.BaseRectangle.Y;
			if (child.BaseRectangle.Right > this.BaseRectangle.Right) childBaseRectangle.Width -= (child.BaseRectangle.Right - this.BaseRectangle.Right);
			if (child.BaseRectangle.Bottom > this.BaseRectangle.Bottom) childBaseRectangle.Height -= (child.BaseRectangle.Bottom - this.BaseRectangle.Top);
			child.BaseRectangle = childBaseRectangle;
			Children.Add(child);
			Viewer?.Invalidate();
		}

		/// <summary>
		/// Determines if the objects's BaseREctangle top-left corner can be moved to the given point. By default, an object may not be dragged outside its parent frame
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public virtual bool AllowedLocation(Point p)
		{
			if (Parent is FramedAnnotationObject)
			{
				FramedAnnotationObject fParent = Parent as FramedAnnotationObject;
				Rectangle testRect = BaseRectangle;
				testRect.Offset(p.X - BaseRectangle.X, p.Y - BaseRectangle.Y);
				// test if all corners of testRect are inside parent's frame
				GraphicsPath parentFrame = fParent.Frame;
				bool b = parentFrame.IsVisible(p) && parentFrame.IsVisible(testRect.Right, testRect.Top) && parentFrame.IsVisible(testRect.Left, testRect.Bottom) && parentFrame.IsVisible(testRect.Right, testRect.Bottom);
				return b;
			}
			else return true;
		}

		public virtual void Draw(Graphics g)
		{
			Children.ForEach(l => l.Draw(g));
		}

		/// <summary>
		/// Replace parent value for all children recursively. This is necessary after deserialization of object.
		/// </summary>
		public void FixChildren()
		{
			Children.ForEach(c =>
			{
				c.Parent = this;
				c.FixChildren();
			});
		}

		/// <summary>
		/// Returns a list with 
		/// </summary>
		public List<AnnotationBaseObject> MeAndMyChildren()
		{
			List<AnnotationBaseObject> aItems = Children.SelectMany(c => c.MeAndMyChildren()).ToList();
			aItems.Insert(0, this);
			return aItems;
		}

		/// <summary>
		/// Removes this object from its parent
		/// </summary>
		public void RemoveFromParent()
		{
			if (Parent != null && Parent.Children.Contains(this))
			{
				Parent.Children.Remove(this);
				Parent = null;
				Viewer?.Invalidate();
			}
		}

		/// <summary>
		/// Brings the object one layer forward
		/// </summary>
		public void BringForward()
		{
			if (Viewer != null)
			{
				var allObjects = Viewer.AnnotationObjects;
				int currentIndex = allObjects.IndexOf(this);
				if (currentIndex < allObjects.Count - 1)
				{
					GViewer v = Viewer;
					v.RemoveAnnotationObject(this);
					v.AddAnnotationObject(this, currentIndex + 1);
					v.Invalidate();
				}
			}
		}

		/// <summary>
		/// Brings the object to front
		/// </summary>
		public void BringToFront()
		{
			if (Viewer != null)
			{
				var allObjects = Viewer.AnnotationObjects;
				int currentIndex = allObjects.IndexOf(this);
				if (currentIndex < allObjects.Count - 1)
				{
					GViewer v = Viewer;
					v.RemoveAnnotationObject(this);
					v.AddAnnotationObject(this, allObjects.Count - 1);
					v.Invalidate();
				}
			}
		}

		/// <summary>
		/// Sets the location of this object and all of its children
		/// </summary>
		/// <param name="newLocation"></param>
		public virtual void SetLocation(Point newLocation)
		{
			Point offset = new Point(newLocation.X - BaseRectangle.X, newLocation.Y - BaseRectangle.Y);
			OffsetChildren(this, offset);

			void OffsetChildren(AnnotationBaseObject parent, Point offsetBy)
			{
				parent.Children.ForEach(c => OffsetChildren(c, offsetBy));
				Rectangle br = parent.BaseRectangle;
				br.Offset(offsetBy);
				parent.BaseRectangle = br;
			}
		}

		/// <summary>
		/// Gets or sets the X coordinate of location
		/// </summary>
		public virtual int X
		{
			get { return BaseRectangle.X; }
			set
			{
				Rectangle br = BaseRectangle;
				br.X = value;
				BaseRectangle = br;
			}
		}

		/// <summary>
		/// Gets or sets the Y coordinate of location
		/// </summary>
		public virtual int Y
		{
			get { return BaseRectangle.Y; }
			set
			{
				Rectangle br = BaseRectangle;
				br.Y = value;
				BaseRectangle = br;
			}
		}

		/// <summary>
		/// Gets or sets the Width of BaseRectangle
		/// </summary>
		/// <param name="width"></param>
		public virtual int Width
		{
			get
			{
				return BaseRectangle.Width;
			}
			set
			{
				Rectangle br = BaseRectangle;
				br.Width = value;
				BaseRectangle = br;
			}
		}

		/// <summary>
		/// Gets or sets the Width of BaseRectangle
		/// </summary>
		/// <param name="width"></param>
		public virtual int Height
		{
			get
			{
				return BaseRectangle.Height;
			}
			set
			{
				Rectangle br = BaseRectangle;
				br.Height = value;
				BaseRectangle = br;
			}
		}

		/// <summary>
		/// Sends the object one layer backward
		/// </summary>
		public void SendBackward()
		{
			if (Viewer != null)
			{
				int currentIndex = Viewer.AnnotationObjects.IndexOf(this);
				if (currentIndex > 0)
				{
					GViewer v = Viewer;
					v.RemoveAnnotationObject(this);
					v.AddAnnotationObject(this, currentIndex - 1);
					v.Invalidate();
				}
			}
		}

		/// <summary>
		/// Sends the object to the back
		/// </summary>
		public void SendToBack()
		{
			if (Viewer != null)
			{
				int currentIndex = Viewer.AnnotationObjects.IndexOf(this);
				if (currentIndex > 0)
				{
					GViewer v = Viewer;
					v.RemoveAnnotationObject(this);
					v.AddAnnotationObject(this, 0);
					v.Invalidate();
				}
			}
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

		public object Clone()
		{
			XmlSerializer ser = new XmlSerializer(this.GetType());
			using (MemoryStream ms = new MemoryStream())
			{
				ser.Serialize(ms, this);
				ms.Position = 0;
				return ser.Deserialize(ms);
			}
		}
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

		/// <summary>
		/// The contour color of the object when selected
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color SelectedFrameColor { get; set; } = Color.Red;

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

		private int _OpacityLevel = 100;
		/// <summary>
		/// The level of fill color trnsparency between 0..255
		/// </summary>
		public int OpacityLevel
		{
			get { return _OpacityLevel; }
			set
			{
				if (value < 0) _OpacityLevel = 0;
				else if (value > 255) _OpacityLevel = 255;
				else _OpacityLevel = value;
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
				if (Selected) return new SolidBrush(SelectedFrameColor);
				else return new SolidBrush(FrameColor);
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
						Color fColor = Color.FromArgb(OpacityLevel, FillColor);
						using (SolidBrush fillBrush = new SolidBrush(fColor))
						{
							g.FillPath(fillBrush, Frame);
						}
						break;
					}
				case BackFillMode.Gradient:
					{
						Color startColor = Color.FromArgb(OpacityLevel, FillColor2);
						Color endColor = Color.FromArgb(OpacityLevel, FillColor);
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
			else if (Selected)
			{
				using (Pen p = new Pen(DrawingBrush, 1))
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
				int hitTestWidth = FixedSize ? 1 : (FrameWidth > 30 ? FrameWidth : 30);
				using (Pen hitTestPen = new Pen(Brushes.Black, hitTestWidth))
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
	/// Implements an abstract base Curve annotation object. A descendant must implement a way to provide CurvePoints.
	/// </summary>
	[Serializable]
	public abstract class AnnotationCurveBase : AnnotationBaseObject
	{
		#region Constructors
		public AnnotationCurveBase() : base()
		{
			Locked = true;
		}
		#endregion

		#region Properties
		/// <summary>
		/// The line color of the curve
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color LineColor { get; set; } = Color.AliceBlue;

		/// <summary>
		/// The line color of the curve if object is selected
		/// </summary>
		[XmlElement(Type = typeof(XmlColor))]
		public Color SelectedLineColor { get; set; } = Color.Red;

		/// <summary>
		/// The width of the curve line
		/// </summary>
		public int LineWidth { get; set; } = 1;

		/// <summary>
		/// The opacity level of the curve line
		/// </summary>
		public int LineOpacity { get; set; } = 255;

		/// <summary>
		/// Provides the curve points
		/// </summary>
		public abstract PointF[] CurvePoints { get; }

		/// <summary>
		/// Defines the LineCap at starting point
		/// </summary>
		public LineCap StartLineCap { get; set; } = LineCap.Round;

		/// <summary>
		/// Defines the LineCap and ending point
		/// </summary>
		public LineCap EndLineCap { get; set; } = LineCap.ArrowAnchor;

		/// <summary>
		/// Gets the BaseRectangle which is calculated from CurvePoints. Value may not be set.
		/// </summary>
		public override Rectangle BaseRectangle
		{
			get
			{
				var cp = CurvePoints;
				if (cp.Length > 0)
				{
					float minX = cp.Min(p => p.X);
					float maxX = cp.Max(p => p.X);
					float minY = cp.Min(p => p.Y);
					float maxY = cp.Max(p => p.Y);
					return new Rectangle(new Point((int)minX, (int)minY), new System.Drawing.Size((int)(maxX - minX), (int)(maxY - minY)));
				}
				else return new Rectangle(0, 0, 0, 0);
			}
			set { }
		}

		/// <summary>
		/// Gets the width of BaseRectangle. Value may not be set.
		/// </summary>
		public override int Width { get => base.Width; set { } }

		/// <summary>
		/// Gets the Height of BaseRectangle. Value may not be set.
		/// </summary>
		public override int Height { get => base.Height; set { } }

		/// <summary>
		/// Gets the X coordinate of location Value may not be set.
		/// </summary>
		public override int X { get => base.X; set { } }

		/// <summary>
		/// Gets the Y coordinate of location Value may not be set.
		/// </summary>
		public override int Y { get => base.Y; set { } }

		#endregion

		#region Public Methods

		public virtual GraphicsPath Curve
		{
			get
			{
				var path = new GraphicsPath();
				var curvePoints = CurvePoints;
				if (curvePoints.Length > 1) path.AddCurve(curvePoints);
				return path;
			}
		}

		public virtual Pen DrawingPen
		{
			get
			{
				Pen p = new Pen(DrawingBrush, LineWidth);
				p.SetLineCap(StartLineCap, EndLineCap, DashCap.Round);
				return p;
			}
		}

		public virtual Brush DrawingBrush
		{
			get
			{
				if (Selected) return new SolidBrush(SelectedLineColor);
				else
				{
					Color brushColor = Color.FromArgb(LineOpacity, LineColor);
					return new SolidBrush(brushColor);
				}
			}
		}

		public override bool ContainsPoint(Point testPoint)
		{
			using (var r = new Region(Curve))
			{
				return r.IsVisible(testPoint);
			}
		}

		/// <summary>
		/// Draws the object using the Curve path
		/// </summary>
		/// <param name="g"></param>
		public override void Draw(Graphics g)
		{
			if (g == null) return;
			if (BaseRectangle.Width == 0 || BaseRectangle.Height == 0) return;
			// --
			if (LineWidth > 0)
			{
				using (var p = DrawingPen)
				{
					g.DrawPath(p, Curve);
				}
			}
			base.Draw(g);
		}

		public override AnnotationObjectRegion HitRegion(Point testPoint)
		{
			if (ContainsPoint(testPoint))
			{
				// Returning Body, because this object may not be resized
				return AnnotationObjectRegion.Body;
			}
			else return AnnotationObjectRegion.None;
		}
		#endregion
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
	public class AnnotationLabel : AnnotationRectangle, IDisposable
	{
		#region Fields
		public string _DisplayText;

		[XmlIgnore]
		/// <summary>
		/// The label text
		/// </summary>
		public string DisplayText
		{
			get
			{
				return _DisplayText;
			}
			set
			{
				_DisplayText = value;
				System.Drawing.Size textSize = System.Windows.Forms.TextRenderer.MeasureText(_DisplayText, DisplayFont);
				Rectangle newBaseRectangle = this.BaseRectangle;
				newBaseRectangle.Width = textSize.Width;
				newBaseRectangle.Height = textSize.Height;
				BaseRectangle = newBaseRectangle;
			}
		}

		/// <summary>
		/// A serializable Font to use for rendering the text
		/// </summary>
		public XmlFont DisplayFont;

		[XmlElement(Type = typeof(XmlColor))]
		public Color FontColor = Color.Black;

		/// <summary>
		/// Label text positioning inside parent's BaseRectangle
		/// </summary>
		public ContentAlignment Alignment { get; set; } = ContentAlignment.MiddleCenter;

		#endregion

		#region Constructors
		public AnnotationLabel() : base()
		{
			// We will calculate label size, do not allow the user to resize it
			FixedSize = true;
			// Mahe the label fully transparent
			this.OpacityLevel = 0;
			// Don't want a frame by default
			this.FrameWidth = 0;
			this.DisplayFont = new XmlFont();
		}

		public AnnotationLabel(AnnotationBaseObject parent, string displayText, ContentAlignment alignement = ContentAlignment.MiddleCenter) : this()
		{
			this.Parent = parent;
			if (Parent != null) this.Viewer = parent.Viewer;
			this.DisplayText = displayText;
			this.Alignment = alignement;
		}
		#endregion

		#region Public members
		public override void Draw(Graphics g)
		{
			base.Draw(g);
			using (Matrix m = g.Transform)
			{
				using (Matrix saveM = m.Clone())
				{
					//rotate the label around its center
					using (var m2 = new Matrix(1, 0, 0, -1, 0, 2 * Center.Y))
					{
						m.Multiply(m2);
					}
					g.Transform = m;
					SizeF StringSize = g.MeasureString(DisplayText, DisplayFont);
					Rectangle newBaseRectangle = this.BaseRectangle;
					newBaseRectangle.Width = (int)StringSize.Width;
					newBaseRectangle.Height = (int)StringSize.Height;
					BaseRectangle = newBaseRectangle;
					PointF textLocation = new PointF { X = BaseRectangle.X + BaseRectangle.Width / 2 - StringSize.Width / 2, Y = BaseRectangle.Y + BaseRectangle.Height / 2 - StringSize.Height / 2 };

					using (Brush textBrush = new SolidBrush(FontColor))
					{
						//
						// See https://www.igloocoder.com/2010/06/09/Rotating-text-using-Graphics-DrawString/
						//
						//var stringFormat = new StringFormat
						//{
						//	Alignment = StringAlignment.Near,
						//	LineAlignment = StringAlignment.Near
						//};
						//PointF transformCoordinate = new PointF(textLocation.X,textLocation.Y);
						//g.TranslateTransform(transformCoordinate.X, transformCoordinate.Y);
						//g.RotateTransform(0);
						//g.DrawString(DisplayText, DisplayFont, textBrush, textLocation, stringFormat);

						g.DrawString(DisplayText, DisplayFont, textBrush, textLocation);
					}

					g.Transform = saveM;
				}
			}
		}

		public void Dispose()
		{
			DisplayFont.Dispose();
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

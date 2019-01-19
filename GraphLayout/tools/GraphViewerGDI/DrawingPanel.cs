/*
Microsoft Automatic Graph Layout,MSAGL 

Copyright (c) Microsoft Corporation

All rights reserved. 

MIT License 

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
""Software""), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi.Annotation;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using BBox = Microsoft.Msagl.Core.Geometry.Rectangle;
using Color = System.Drawing.Color;
using MouseButtons = System.Windows.Forms.MouseButtons;
using P2 = Microsoft.Msagl.Core.Geometry.Point;

namespace Microsoft.Msagl.GraphViewerGdi
{
	/// <summary>
	/// this class serves as a drawing panel for GViewer
	/// </summary>
	internal class DrawingPanel : Control
	{
		readonly Color rubberRectColor = Color.Green;
		const FrameStyle RubberRectStyle = FrameStyle.Dashed;
		MouseButtons currentPressedButton;
		GViewer gViewer;

		System.Drawing.Point mouseDownPoint;

		System.Drawing.Point mouseUpPoint;
		P2 rubberLineEnd;
		P2 rubberLineStart;
		System.Drawing.Rectangle rubberRect;
		bool zoomWindow;
		PlaneTransformation mouseDownTransform;
		bool NeedToEraseRubber { get; set; }
		internal GViewer GViewer
		{
			private get { return gViewer; }
			set { gViewer = value; }
		}
		/// <summary>
		/// The Anotation object last hit by mouseDown event. Cleared on mouseUp
		/// </summary>
		AnnotationObjectHit _draggedAnnotationObject;
		/// <summary>
		/// The Annotation object last selected
		/// </summary>
		internal AnnotationBaseObject SelectedAnnotationObject { get; set; }
		/// <summary>
		/// The cursor position from top-left corner of _hitAnnotationObject when hit
		/// </summary>
		Size _annotationHitOffset;

		DraggingMode MouseDraggingMode
		{
			get
			{
				if (gViewer.panButton.Pushed)
					return DraggingMode.Pan;
				if (gViewer.windowZoomButton.Pushed)
					return DraggingMode.WindowZoom;
				return DraggingMode.Default;
			}
		}

		bool DrawingRubberEdge { get; set; }

		P2 RubberLineEnd
		{
			get { return rubberLineEnd; }
			set
			{
				if (DrawingRubberEdge)
					Invalidate(CreateRectForRubberEdge());
				rubberLineEnd = value;
			}
		}

		EdgeGeometry CurrentRubberEdge { get; set; }

		internal void SetDoubleBuffering()
		{
			// the magic calls for invoking doublebuffering
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}


		protected override void OnPaint(PaintEventArgs e)
		{

			if (gViewer != null && gViewer.Graph != null && gViewer.Graph.GeometryGraph != null)
			{
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
				gViewer.ProcessOnPaint(e.Graphics, null);
			}
			if (CurrentRubberEdge != null)
				using (GraphicsPath gp = Draw.CreateGraphicsPath(CurrentRubberEdge.Curve))
				using (var pen = new Pen(Brushes.Black, (float)GViewer.LineThicknessForEditing))
					e.Graphics.DrawPath(pen, gp);

			if (DrawingRubberEdge)
				e.Graphics.DrawLine(new Pen(Brushes.Black, (float)GViewer.LineThicknessForEditing),
														(float)rubberLineStart.X, (float)rubberLineStart.Y, (float)RubberLineEnd.X,
														(float)RubberLineEnd.Y);
			base.OnPaint(e); // Filippo Polo 13/11/07; if I don't do this, onpaint events won't be invoked
			gViewer.RaisePaintEvent(e);
		}

		void DrawXorFrame()
		{
			ControlPaint.DrawReversibleFrame(rubberRect, rubberRectColor, RubberRectStyle);
			NeedToEraseRubber = !NeedToEraseRubber;
		}

		void DrawZoomWindow(MouseEventArgs args)
		{
			mouseUpPoint.X = args.X;
			mouseUpPoint.Y = args.Y;

			if (NeedToEraseRubber)
				DrawXorFrame();

			if (ClientRectangle.Contains(PointToClient(MousePosition)))
			{
				rubberRect = GViewer.RectFromPoints(PointToScreen(mouseDownPoint), PointToScreen(mouseUpPoint));
				DrawXorFrame();
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			MsaglMouseEventArgs iArgs = CreateMouseEventArgs(e);
			P2 p1 = gViewer.ScreenToSource(e.Location);
			gViewer.RaiseMouseDownEvent(iArgs);
			if (!iArgs.Handled)
			{
				#region Annotation object hit testing
				// Here, we get a chance to process AnnotationObject selection and move
				if (_draggedAnnotationObject.aObject == null)
				{
					_draggedAnnotationObject.aObject = SelectedAnnotationObject;
					if (_draggedAnnotationObject.aObject != null)
					{
						_draggedAnnotationObject.hitRegion = _draggedAnnotationObject.aObject.HitRegion(p1);
						_annotationHitOffset = new Size((int)p1.X - _draggedAnnotationObject.aObject.BaseRectangle.X, (int)p1.Y - _draggedAnnotationObject.aObject.BaseRectangle.Y);
						return;
					}
					else
					{
						_draggedAnnotationObject.hitRegion = AnnotationObjectRegion.None;
					}
				}
				#endregion

				currentPressedButton = e.Button;
				if (currentPressedButton == MouseButtons.Left)
					if (ClientRectangle.Contains(PointToClient(MousePosition)))
					{
						mouseDownPoint = new Point(e.X, e.Y);
						if (MouseDraggingMode != DraggingMode.Pan)
							zoomWindow = true;
						else
						{
							mouseDownTransform = gViewer.Transform.Clone();
						}
					}
			}
		}

		protected override void OnMouseUp(MouseEventArgs args)
		{
			base.OnMouseUp(args);
			MsaglMouseEventArgs iArgs = CreateMouseEventArgs(args);
			_draggedAnnotationObject.aObject = null;
			_draggedAnnotationObject.hitRegion = AnnotationObjectRegion.None;
			gViewer.RaiseMouseUpEvent(iArgs);
			if (NeedToEraseRubber) DrawXorFrame();
			if (!iArgs.Handled)
			{

				if (gViewer.OriginalGraph != null && MouseDraggingMode == DraggingMode.WindowZoom)
				{
					var p = mouseDownPoint;
					double f = Math.Max(Math.Abs(p.X - args.X), Math.Abs(p.Y - args.Y)) / GViewer.Dpi;
					if (f > gViewer.ZoomWindowThreshold && zoomWindow)
					{
						mouseUpPoint = new Point(args.X, args.Y);
						if (ClientRectangle.Contains(mouseUpPoint))
						{
							//var r = GViewer.RectFromPoints(mouseDownPoint, mouseUpPoint);
							//r.Intersect(gViewer.DestRect);
							if (GViewer.ModifierKeyWasPressed() == false)
							{
								P2 p1 = gViewer.ScreenToSource(mouseDownPoint);
								P2 p2 = gViewer.ScreenToSource(mouseUpPoint);
								double sc = Math.Min(Width / Math.Abs(p1.X - p2.X),
										Height / Math.Abs(p1.Y - p2.Y));
								P2 center = 0.5f * (p1 + p2);
								gViewer.SetTransformOnScaleAndCenter(sc, center);
								Invalidate();
							}
						}
					}
				}
			}
			zoomWindow = false;
		}

		protected override void OnMouseMove(MouseEventArgs args)
		{
			MsaglMouseEventArgs iArgs = CreateMouseEventArgs(args);
			P2 p1 = gViewer.ScreenToSource(args.Location);
			// Here, we get a chance to process AnnotationObject selection and move
			#region Handle Annotation objects
			Cursor annotationCursor = Cursors.Default;
			if (args.Button == MouseButtons.None)
			{
				var overAnnotationObject = GViewer._annotationObjects.FirstOrDefault(a => a.HitRegion(p1) != AnnotationObjectRegion.None);
				if (overAnnotationObject != null)
				{
					AnnotationObjectRegion hr = overAnnotationObject.HitRegion(p1);
					// if hit on object body
					if ((hr & AnnotationObjectRegion.Body) == hr) annotationCursor = Cursors.SizeAll;
					// hit on edge
					switch (hr)
					{
						case AnnotationObjectRegion.EdgeLeft:
						case AnnotationObjectRegion.EdgeRight:
							{
								annotationCursor = Cursors.SizeWE;
								break;
							}
						case AnnotationObjectRegion.EdgeTop:
						case AnnotationObjectRegion.EdgeBottom:
							{
								annotationCursor = Cursors.SizeNS;
								break;
							}
					}
				}
			}
			if (_draggedAnnotationObject.aObject != null && args.Button == MouseButtons.Left)
			{
				// an AnnotationObject is hit by MouseDown, now moding the mouse while Left button is being pressed => move or size the object depending on HitRegion
				AnnotationObjectRegion hr = _draggedAnnotationObject.hitRegion;
				AnnotationBaseObject ao = _draggedAnnotationObject.aObject;
				if ((hr & AnnotationObjectRegion.Edge) == hr)
				{
					// resize
					switch (hr)
					{
						case AnnotationObjectRegion.EdgeLeft:
							{
								int w = ao.BaseRectangle.Width - ((int)p1.X - ao.BaseRectangle.X);
								if (w > 5)
								{
									ao.BaseRectangle.X = (int)p1.X;
									ao.BaseRectangle.Width = w;
								}
								break;
							}
						case AnnotationObjectRegion.EdgeRight:
							{
								int w = (int)p1.X - ao.BaseRectangle.X;
								if (w > 5) ao.BaseRectangle.Width = w;
								break;
							}
						case AnnotationObjectRegion.EdgeTop:
							{
								int h = ao.BaseRectangle.Height - ((int)p1.Y - ao.BaseRectangle.Y);
								if (h > 5)
								{
									ao.BaseRectangle.Y = (int)p1.Y;
									ao.BaseRectangle.Height = h;
								}
								break;
							}
						case AnnotationObjectRegion.EdgeBottom:
							{
								int h = (int)p1.Y - ao.BaseRectangle.Y;
								if (h > 5) ao.BaseRectangle.Height = h;
								break;
							}
					}
				}
				else
				{
					// move
					ao.BaseRectangle.Location = new Point((int)p1.X - _annotationHitOffset.Width, (int)p1.Y - _annotationHitOffset.Height);
				}
				Invalidate();
			}
			#endregion
			else
			{
				gViewer.RaiseMouseMoveEvent(iArgs);
				gViewer.RaiseRegularMouseMove(args);
				if (!iArgs.Handled)
				{
					if (gViewer.Graph != null)
					{
						SetCursor(args);
						if (MouseDraggingMode == DraggingMode.Pan)
						{
							ProcessPan(args);
						}
						else if (zoomWindow)
						{
							//the user is holding the left button, do nothing
							DrawZoomWindow(args);
						}
						else
						{
							HitIfBbNodeIsNotNull(args);
							if ((gViewer.SelectedObject == null || gViewer.SelectedObject is AnnotationBaseObject) && annotationCursor != Cursors.Default) this.Cursor = annotationCursor;
						}
					}
				}
			}
		}

		/// <summary>
		/// Set context menu strip
		/// </summary>
		/// <param name="contexMenuStrip"></param>
		public void SetCms(ContextMenuStrip contexMenuStrip)
		{
			MouseClick +=
					delegate (object sender, MouseEventArgs e)
					{
						if (e.Button == MouseButtons.Right)
						{
							var newE = new MouseEventArgs(
												MouseButtons.None,
												e.Clicks,
												e.X,
												e.Y,
												e.Delta);

							OnMouseMove(newE);

							contexMenuStrip.Show(this, e.X, e.Y);
						}
					};
		}

		void HitIfBbNodeIsNotNull(MouseEventArgs args)
		{
			if (gViewer.DGraph != null && gViewer.BbNode != null)
				gViewer.Hit(args);
		}


		static MsaglMouseEventArgs CreateMouseEventArgs(MouseEventArgs args)
		{
			return new ViewerMouseEventArgs(args);
		}


		void SetCursor(MouseEventArgs args)
		{
			Cursor cur;
			if (MouseDraggingMode == DraggingMode.Pan)
			{
				cur = args.Button == MouseButtons.Left
									? gViewer.panGrabCursor
									: gViewer.panOpenCursor;
			}
			else
				cur = gViewer.originalCursor;

			if (cur != null)
				Cursor = cur;
		}


		void ProcessPan(MouseEventArgs args)
		{
			if (ClientRectangle.Contains(args.X, args.Y))
			{
				if (args.Button == MouseButtons.Left)
				{
					if (mouseDownTransform != null)
					{
						gViewer.Transform[0, 2] = mouseDownTransform[0, 2] + args.X - mouseDownPoint.X;
						gViewer.Transform[1, 2] = mouseDownTransform[1, 2] + args.Y - mouseDownPoint.Y;
					}
					gViewer.Invalidate();
				}
				else
					GViewer.Hit(args);
			}
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			gViewer.OnKey(e);
			base.OnKeyUp(e);
		}


		internal void DrawRubberLine(MsaglMouseEventArgs args)
		{
			RubberLineEnd = gViewer.ScreenToSource(new Point(args.X, args.Y));
			DrawRubberLineWithKnownEnd();
		}

		internal void DrawRubberLine(P2 point)
		{
			RubberLineEnd = point;
			DrawRubberLineWithKnownEnd();
		}

		void DrawRubberLineWithKnownEnd()
		{
			DrawingRubberEdge = true;
			Invalidate(CreateRectForRubberEdge());
		}

		Rectangle CreateRectForRubberEdge()
		{
			var rect = new BBox(rubberLineStart, RubberLineEnd);
			double w = gViewer.LineThicknessForEditing;
			var del = new P2(-w, w);
			rect.Add(rect.LeftTop + del);
			rect.Add(rect.RightBottom - del);
			return GViewer.CreateScreenRectFromTwoCornersInTheSource(rect.LeftTop, rect.RightBottom);
		}

		internal void StopDrawRubberLine()
		{
			DrawingRubberEdge = false;
			Invalidate(CreateRectForRubberEdge());
		}

		internal void MarkTheStartOfRubberLine(P2 point)
		{
			rubberLineStart = point;
		}

		internal void DrawRubberEdge(EdgeGeometry edgeGeometry)
		{
			BBox rectToInvalidate = edgeGeometry.BoundingBox;
			if (CurrentRubberEdge != null)
			{
				BBox b = CurrentRubberEdge.BoundingBox;
				rectToInvalidate.Add(b);
			}
			CurrentRubberEdge = edgeGeometry;
			GViewer.Invalidate(GViewer.CreateScreenRectFromTwoCornersInTheSource(rectToInvalidate.LeftTop,
																																					 rectToInvalidate.RightBottom));
		}

		internal void StopDrawingRubberEdge()
		{
			if (CurrentRubberEdge != null)
				GViewer.Invalidate(
						GViewer.CreateScreenRectFromTwoCornersInTheSource(
								CurrentRubberEdge.BoundingBox.LeftTop,
								CurrentRubberEdge.BoundingBox.RightBottom));

			CurrentRubberEdge = null;
		}
	}
}
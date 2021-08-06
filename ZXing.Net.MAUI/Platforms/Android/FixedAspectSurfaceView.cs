using Android.Content.Res;
using Android.Content;
using Android.Views;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Android.Views.View;
using Android.Util;

namespace ZXing.Net.Maui
{
	public class FixedAspectSurfaceView : SurfaceView
	{
		float aspectRatio = 1.333f;
		GestureDetector gestureDetector;

		public FixedAspectSurfaceView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
			AspectRatio = 1.333f;
		}

		/**
		 * Desired width/height ratio
		 */
		public float AspectRatio
		{
			get => aspectRatio;
			set
			{
				aspectRatio = value; RequestLayout();
			}
		}

		/**
		 * Set a gesture listener to listen for touch events
		 */
		public void SetGestureListener(Context context, GestureDetector.IOnGestureListener listener)
		{
			if (listener == null)
				gestureDetector = null;
			else
				gestureDetector = new GestureDetector(context, listener);
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var widthMode = MeasureSpec.GetMode(widthMeasureSpec);
			var heightMode = MeasureSpec.GetMode(heightMeasureSpec);
			var width = MeasureSpec.GetSize(widthMeasureSpec);
			var height = MeasureSpec.GetSize(heightMeasureSpec);

			// General goal: Adjust dimensions to maintain the requested aspect ratio as much
			// as possible. Depending on the measure specs handed down, this may not be possible

			// Only set one of these to true
			var scaleWidth = false;
			var scaleHeight = false;

			// Sort out which dimension to scale, if either can be. There are 9 combinations of
			// possible measure specs; a few cases below handle multiple combinations
			//noinspection StatementWithEmptyBody
			if (widthMode == MeasureSpecMode.Exactly && heightMode == MeasureSpecMode.Exactly)
			{
				// Can't adjust sizes at all, do nothing
			}
			else if (widthMode == MeasureSpecMode.Exactly)
			{
				// Width is fixed, heightMode either AT_MOST or UNSPECIFIED, so adjust height
				scaleHeight = true;
			}
			else if (heightMode == MeasureSpecMode.Exactly)
			{
				// Height is fixed, widthMode either AT_MOST or UNSPECIFIED, so adjust width
				scaleWidth = true;
			}
			else if (widthMode == MeasureSpecMode.AtMost && heightMode == MeasureSpecMode.AtMost)
			{
				// Need to fit into box <= [width, height] in size.
				// Maximize the View's area while maintaining aspect ratio
				// This means keeping one dimension as large as possible and shrinking the other
				float boxAspectRatio = width / (float)height;
				if (boxAspectRatio > AspectRatio)
				{
					// Box is wider than requested aspect; pillarbox
					scaleWidth = true;
				}
				else
				{
					// Box is narrower than requested aspect; letterbox
					scaleHeight = true;
				}
			}
			else if (widthMode == MeasureSpecMode.AtMost)
			{
				// Maximize width, heightSpec is UNSPECIFIED
				scaleHeight = true;
			}
			else if (heightMode == MeasureSpecMode.AtMost)
			{
				// Maximize height, widthSpec is UNSPECIFIED
				scaleWidth = true;
			}
			else
			{
				// Both MeasureSpecs are UNSPECIFIED. This is probably a pathological layout,
				// with width == height == 0
				// but arbitrarily scale height anyway
				scaleHeight = true;
			}

			// Do the scaling
			if (scaleWidth)
			{
				width = (int)(height * AspectRatio);
			}
			else if (scaleHeight)
			{
				height = (int)(width / AspectRatio);
			}

			// Override width/height if needed for EXACTLY and AT_MOST specs
			width = View.ResolveSizeAndState(width, widthMeasureSpec, 0);
			height = View.ResolveSizeAndState(height, heightMeasureSpec, 0);

			// Finally set the calculated dimensions
			SetMeasuredDimension(width, height);
		}

		public override bool OnTouchEvent(MotionEvent @event)
			=> gestureDetector?.OnTouchEvent(@event) ?? false;
	}
}

using System;
using System.Drawing;
using OpenTK;
namespace osum
{
	public class TrackingPointIphone : TrackingPoint
	{
		public TrackingPointIphone (PointF location, object tag) : base(location,tag)
		{			
		}
		
		public override OpenTK.Vector2 WindowDelta {
			get
			{
				return new Vector2((GameBase.ScaleFactor * Delta.Y / GameBase.NativeSize.Width) * GameBase.BaseSizeFixedWidth.Width,
					                   -((GameBase.ScaleFactor * Delta.X / GameBase.NativeSize.Height) * GameBase.BaseSizeFixedWidth.Height));
			}
		}
		
		public override OpenTK.Vector2 BasePosition {
			get
			{
				return new Vector2((GameBase.ScaleFactor * Location.Y / GameBase.NativeSize.Width) * GameBase.BaseSizeFixedWidth.Width,
				                   GameBase.BaseSizeFixedWidth.Height - ((GameBase.ScaleFactor * Location.X / GameBase.NativeSize.Height) * GameBase.BaseSizeFixedWidth.Height));
			}
		}
	}
}


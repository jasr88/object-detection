using UnityEngine;

public class BoundingBoxDimensions :DimensionsBase { }
public class BoundingBox
{
	public BoundingBoxDimensions Dimensions { get; set; }
	public string Label { get; set; }
	public float Confidence { get; set; }
	// We use the Unity Rect to draw the graphic representation of the object detection
	public Rect Rect {
		get { return new Rect (Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height); }
	}
	public Color BoxColor { get; set; }
}
